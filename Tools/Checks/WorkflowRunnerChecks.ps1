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
$selected=@(Resolve-VerificationSuites -Registry $registry -Suite @('coffee-display','ledger','coffee-display'))
if (($selected -join ',') -ne 'coffee-display,ledger') { throw 'Targeted suite order or deduplication changed.' };$passed++
$default=@(Resolve-VerificationSuites -Registry $registry -Profile smoke)
if (($default -join ',') -ne ($registry.profiles.smoke -join ',')) { throw 'Default profile compatibility lost.' };$passed++
Require-Throws { Resolve-VerificationSuites -Registry $registry -Suite @('ledger','does-not-exist') }
Require-Throws { Resolve-VerificationSuites -Registry $registry -Suite @(' ') }
Require-Throws { Resolve-VerificationSuites -Registry $registry -Profile 'does-not-exist' }
$empty=[pscustomobject]@{profiles=[pscustomobject]@{empty=@()};suites=$registry.suites}
Require-Throws { Resolve-VerificationSuites -Registry $empty -Profile empty }
$document=Join-Path $root 'Docs/Workflow/mock.md'
$valid=@(Get-DocumentationLinkFailures -Root $root -DocumentPath $document -Content '[repo](../../AGENTS.md#rules) [web](https://example.invalid/) [anchor](#rules)')
if ($valid.Count -ne 0) { throw 'Valid relative documentation links were rejected.' };$passed++
$broken=@(Get-DocumentationLinkFailures -Root $root -DocumentPath $document -Content '[missing](missing.md) [escape](../../../outside.md)')
if ($broken.Count -ne 2) { throw 'Missing or escaping documentation links were accepted.' };$passed++
Write-Host "Workflow runner: $passed checks passed."
