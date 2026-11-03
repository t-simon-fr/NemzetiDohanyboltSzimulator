[CmdletBinding()]
param([ValidateSet('main-menu','settings','current')][string]$Scenario='current',[ValidateRange(5,60)][int]$TimeoutSeconds=20)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $PSScriptRoot 'Workflow.Common.psm1') -Force
$run=Join-Path $root ('Builds/Verification/visual-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$image=Join-Path $run ($Scenario+'.png')
$identity=Get-SourceIdentity $root
if($Scenario -ne 'current') {
    Invoke-UnityCommand $root 'editor.playmode stop' | Out-Null
    Invoke-UnityCommand $root 'editor.playmode start' | Out-Null
}
$setup='if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorUtility.scriptCompilationFailed)throw new System.Exception("Enter Play with a successful compile first.");'
if($Scenario -eq 'settings') { $setup+='var menu=MvpShopController.Instance.GetComponent<ShopMenu>();typeof(ShopMenu).GetField("settings",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(menu,true);' }
# CaptureScreenshot is asynchronous and includes IMGUI. Camera.Render does not include menu overlays.
$literal='"'+$image.Replace('\','/').Replace('"','\"')+'"'
$code=$setup+'UnityEngine.ScreenCapture.CaptureScreenshot('+$literal+'); return true;'
Invoke-UnityCommand $root 'script eval' @{code=$code} | Out-Null
$deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while(!(Test-Path -LiteralPath $image) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 250 }
if(!(Test-Path -LiteralPath $image) -or (Get-Item -LiteralPath $image).Length -lt 100) { throw 'No Game view image produced. Show the Game view and retry; do not mark visual verification complete.' }
@{scenario=$Scenario;source=$identity;capturedUtc=[DateTime]::UtcNow.ToString('o');image=$image;review='pending';interactionReview='pending';reviewer=$null} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'review.json') -Encoding utf8
Write-Host "Captured: $image"
Write-Host 'Inspect the image and run the interaction checklist before marking visual review passed.'
