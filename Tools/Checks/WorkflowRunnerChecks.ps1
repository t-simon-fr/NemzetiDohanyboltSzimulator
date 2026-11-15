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
if (!(Test-TransportFailure 'An error occurred while sending the request.')) { throw 'Dropped pipeline request was not classified as a transport failure.' };$passed++
if (!(Test-TransportFailure 'No connection could be made because the target machine actively refused it. (127.0.0.1:7800)')) { throw 'Refused pipeline port was not classified as a transport failure.' };$passed++
if ((Test-TransportFailure 'script run execution failed: {"diagnostics":[],"success":false}')) { throw 'Execution failure was misclassified as a transport failure.' };$passed++
if ((Test-TransportFailure 'Unity compilation failed. Read the Unity console; no tests were run.')) { throw 'Compilation failure was misclassified as a transport failure.' };$passed++

Reset-UnityTransportRetryCount
$script:retryCalls=0
$recovered=Invoke-WithTransportRetry { $script:retryCalls++; if($script:retryCalls -lt 3) { throw 'An error occurred while sending the request.' }; 'ok' } -BeforeRetry { param($attempt,$attempts) }
if ($recovered -ne 'ok' -or $script:retryCalls -ne 3) { throw 'Transient transport failure was not repeated until success.' };$passed++
if ((Get-UnityTransportRetryCount) -ne 2) { throw 'Transport retry counter does not match the actual repeats.' };$passed++

Reset-UnityTransportRetryCount
$script:retryCalls=0
try { Invoke-WithTransportRetry { $script:retryCalls++; throw 'Use of unassigned local variable' } -BeforeRetry { param($attempt,$attempts) } | Out-Null } catch {}
if ($script:retryCalls -ne 1) { throw 'Assertion-style failure was retried as a transport failure.' };$passed++
if ((Get-UnityTransportRetryCount) -ne 0) { throw 'Non-transport failure incremented the retry counter.' };$passed++

Reset-UnityTransportRetryCount
$script:retryCalls=0
$exhausted=$false
try { Invoke-WithTransportRetry { $script:retryCalls++; throw 'Unable to connect to the remote server' } -Attempts 2 -BeforeRetry { param($attempt,$attempts) } | Out-Null } catch { $exhausted=$true }
if (!$exhausted -or $script:retryCalls -ne 2) { throw 'Exhausted transport retries did not stop at the configured limit.' };$passed++
Reset-UnityTransportRetryCount
Write-Host "Workflow runner: $passed checks passed."
