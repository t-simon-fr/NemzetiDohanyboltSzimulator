[CmdletBinding()]
param([ValidateSet('smoke','checkout')][string]$Profile='smoke',[ValidateRange(10,600)][int]$CompileTimeoutSeconds=120)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $PSScriptRoot 'Workflow.Common.psm1') -Force
$run=Join-Path $root ('Builds/Verification/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$report=[ordered]@{schemaVersion=1;profile=$Profile;startedUtc=[DateTime]::UtcNow.ToString('o');source=$null;compilation='pending';tests=@();result='failed';error=$null}
$startedPlay=$false
try {
    $report.source=Get-SourceIdentity $root
    $registry=Get-Content (Join-Path $PSScriptRoot 'Verification/suites.json') -Raw | ConvertFrom-Json
    Invoke-UnityCommand $root 'editor.playmode stop' | Out-Null
    # This request can be dropped by the successful domain reload; status polling is authoritative.
    $compileStarted=[DateTime]::UtcNow
    $statusFile=Join-Path $root 'Temp/pipeline_recompile_status.json'
    try { Invoke-UnityCommand $root 'script recompile' | Out-Null } catch { Write-Host 'Compile request interrupted; checking compile status.' }
    $deadline=[DateTime]::UtcNow.AddSeconds($CompileTimeoutSeconds)
    $compiled=$false
    while([DateTime]::UtcNow -lt $deadline) {
        try {
            $state=Invoke-UnityCommand $root 'script recompile_status'
            if($state.failed -or $state.compilationFailed) { throw 'UNITY_COMPILE_FAILED' }
            if($state.status -in @('completed','up_to_date') -and (Test-Path -LiteralPath $statusFile) -and (Get-Item -LiteralPath $statusFile).LastWriteTimeUtc -ge $compileStarted) { $compiled=$true;break }
        } catch {
            if($_.Exception.Message -eq 'UNITY_COMPILE_FAILED') { throw 'Unity compilation failed. Read the Unity console; no tests were run.' }
        }
        Start-Sleep -Milliseconds 500
    }
    if(!$compiled) { throw 'Unity compilation did not complete before timeout.' }
    $report.compilation='passed'
    # Native state prevents an old managed assembly from masquerading as a successful fresh compile.
    $truth=Invoke-UnityCommand $root 'script eval' @{code='return new { compiling=UnityEditor.EditorApplication.isCompiling, failed=UnityEditor.EditorUtility.scriptCompilationFailed, scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };'}
    if($truth.result.compiling -or $truth.result.failed) { throw 'Unity is still compiling or has compile errors.' }
    if($truth.result.scene -ne 'Assets/Game/Scenes/ShopPrototype.unity') { throw 'Open ShopPrototype before running gameplay checks.' }
    Invoke-UnityCommand $root 'editor.playmode start' | Out-Null
    $startedPlay=$true
    Invoke-UnityCommand $root 'script eval' @{code='var g=MvpShopController.Instance; if(g==null)throw new System.Exception("Shop controller not ready");g.GetComponent<ShopMenu>().Resume();return true;'} | Out-Null
    foreach($id in $registry.profiles.$Profile) {
        $suite=$registry.suites.$id
        $value=Invoke-UnityCommand $root 'script run' @{file=$suite.file;entry=$suite.entry;args=@()}
        if($null -eq $value.result -or $value.result.PSObject.Properties.Name -notcontains 'failed' -or $value.result.failed -ne 0 -or $value.result.passed -le 0) { throw "Suite $id returned no successful assertion report." }
        $report.tests+=@{id=$id;entry=$suite.entry;passed=$value.result.passed;failed=$value.result.failed}
        Write-Host "$id : $($value.result.passed) passed"
    }
    $after=Get-SourceIdentity $root
    if($after.sourceFingerprint -ne $report.source.sourceFingerprint) { throw 'Source changed during verification; rerun for the new source.' }
    $report.result='passed'
} catch { $report.error=$_.Exception.Message;Write-Warning $report.error }
finally {
    if($startedPlay) { try { Invoke-UnityCommand $root 'editor.playmode stop' | Out-Null } catch { $report.result='failed';$report.error='Could not exit Play mode: '+$_.Exception.Message } }
    $report.finishedUtc=[DateTime]::UtcNow.ToString('o')
    $report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $run 'report.json') -Encoding utf8
    Write-Host "Report: $run/report.json"
}
if($report.result -ne 'passed') { exit 1 }
exit 0
