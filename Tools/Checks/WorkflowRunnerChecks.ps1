$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Import-Module (Join-Path $root 'Tools/Workflow.Common.psm1') -Force
$passed=0
function Require-Throws($action) {
    $thrown=$false
    try { & $action | Out-Null } catch { $thrown=$true }
    if(!$thrown) { throw 'Invalid pipeline response was incorrectly accepted.' }
    $script:passed++
}
Require-Throws { Assert-PipelineResponse ([pscustomobject]@{success=$false;result='error'}) }
Require-Throws { Assert-PipelineResponse ([pscustomobject]@{success=$true;result=[pscustomobject]@{success=$false;error='Compilation Failed'}}) }
Require-Throws { Assert-PipelineResponse ([pscustomobject]@{success=$true;result=[pscustomobject]@{success=$false;error='Runtime Error'}}) }
Require-Throws { Assert-PipelineResponse ([pscustomobject]@{result='missing transport status'}) }
$result=Assert-PipelineResponse ([pscustomobject]@{success=$true;result=[pscustomobject]@{success=$true;result=[pscustomobject]@{passed=20;failed=0}}})
if($result.result.passed -ne 20) { throw 'Successful nested report lost.' };$passed++
Get-ChildItem (Join-Path $root 'Tools') -Recurse -Include *.ps1,*.psm1 | ForEach-Object {
    $tokens=$null;$parseErrors=$null
    [System.Management.Automation.Language.Parser]::ParseFile($_.FullName,[ref]$tokens,[ref]$parseErrors) | Out-Null
    if($parseErrors.Count) { throw ($parseErrors | Out-String) }
};$passed++
$registry=Get-Content (Join-Path $root 'Tools/Verification/suites.json') -Raw | ConvertFrom-Json
foreach($profile in $registry.profiles.PSObject.Properties) {
    foreach($id in $profile.Value) {
        $suite=$registry.suites.$id
        if(!$suite -or !(Test-Path (Join-Path $root $suite.file)) -or !$suite.entry -or !$suite.purpose) { throw "Invalid suite: $id" }
    }
};$passed++
Write-Host "Workflow runner: $passed checks passed."
