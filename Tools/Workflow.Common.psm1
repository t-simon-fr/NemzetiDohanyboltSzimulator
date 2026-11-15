Set-StrictMode -Version Latest

$script:UnityTransportRetries=0

function Get-UnityTransportRetryCount { return $script:UnityTransportRetries }
function Reset-UnityTransportRetryCount { $script:UnityTransportRetries=0 }

function Assert-PipelineResponse {
    param([Parameter(Mandatory)]$Response, [string]$Context='Pipeline')
    if ($Response.PSObject.Properties.Name -notcontains 'success' -or !$Response.success) { throw "$Context transport failed." }
    $value=$Response.result
    # script run/eval wrap their execution status inside the transport envelope.
    if ($null -ne $value -and $value.PSObject.Properties.Name -contains 'success' -and !$value.success) {
        $detail=$value | ConvertTo-Json -Depth 12 -Compress
        throw "$Context execution failed: $detail"
    }
    return $value
}

function Get-SourceIdentity {
    param([Parameter(Mandatory)][string]$Root)
    $head=(& git -C $Root rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve source commit.' }
    $lines=[System.Collections.Generic.List[string]]::new()
    foreach($folder in @('Assets','Packages','ProjectSettings','Tools')) {
        Get-ChildItem -LiteralPath (Join-Path $Root $folder) -Recurse -File |
            Where-Object { $_.FullName -notmatch '[\\/]_Recovery[\\/]|[\\/]_Recovery\.meta$' } |
            ForEach-Object {
                $relative=[IO.Path]::GetRelativePath($Root,$_.FullName).Replace('\','/')
                $lines.Add($relative+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)
            }
    }
    $bytes=[Text.Encoding]::UTF8.GetBytes((($lines | Sort-Object) -join "`n"))
    $fingerprint=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    return [ordered]@{commit=$head;sourceFingerprint=$fingerprint;fingerprintScope='Assets excluding user _Recovery, Packages, ProjectSettings, Tools';dirty=[bool](& git -C $Root status --porcelain --untracked-files=normal)}
}

function Test-TransportFailure {
    param([Parameter(Mandatory)]$Failure)
    # The pipeline endpoint restarts after a domain reload or a WebGL build and can drop an in-flight request.
    $message=if ($Failure -is [System.Management.Automation.ErrorRecord]) { "$($Failure.Exception.Message)" } else { "$Failure" }
    return $message -match 'error occurred while sending the request|actively refused|Unable to connect|forcibly closed|connection was closed|connection was reset|operation has timed out|The request was aborted'
}

function Wait-PipelineDescriptorRefresh {
    param([Parameter(Mandatory)][string]$Root,[Parameter(Mandatory)][datetime]$ObservedUtc,[ValidateRange(1,120)][int]$TimeoutSeconds=5)
    $descriptor=Join-Path $Root 'Library/Pipeline/.unity-pipeline-port'
    $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while([DateTime]::UtcNow -lt $deadline) {
        try {
            if((Test-Path -LiteralPath $descriptor) -and (Get-Item -LiteralPath $descriptor).LastWriteTimeUtc -gt $ObservedUtc) { return $true }
        } catch { }
        Start-Sleep -Milliseconds 250
    }
    return $false
}

function Invoke-WithTransportRetry {
    param([Parameter(Mandatory)][scriptblock]$Action,[ValidateRange(1,10)][int]$Attempts=4,[scriptblock]$BeforeRetry)
    for($attempt=1;;$attempt++) {
        try { return (& $Action) }
        catch {
            $failure=$_
            # Execution and compilation failures are real results; only a lost transport is worth repeating.
            if($attempt -ge $Attempts -or !(Test-TransportFailure -Failure $failure)) { throw }
            $script:UnityTransportRetries++
            if($BeforeRetry) { & $BeforeRetry $attempt $Attempts } else { Start-Sleep -Milliseconds 750 }
        }
    }
}

function Invoke-UnityCommand {
    param([Parameter(Mandatory)][string]$Root,[Parameter(Mandatory)][string]$Command,$Parameters=@{},[ValidateRange(1,10)][int]$Attempts=4)
    $json=$Parameters | ConvertTo-Json -Depth 15 -Compress
    # The retry hook waits for the editor to republish its endpoint, so the repeat uses the new port and token.
    # Repeating a dropped request is safe: it never produced a result, and every suite resets the demo state first.
    $state=[pscustomobject]@{attemptStartedUtc=[DateTime]::UtcNow}
    $action={
        $state.attemptStartedUtc=[DateTime]::UtcNow
        $raw=& (Join-Path $Root 'Tools/Invoke-Pipeline.ps1') -Command $Command -Parameters $json
        $response=($raw -join "`n") | ConvertFrom-Json
        Assert-PipelineResponse -Response $response -Context $Command
    }.GetNewClosure()
    $beforeRetry={
        param($attempt,$attempts)
        Write-Host "Pipeline transport failed for '$Command' (attempt $attempt/$attempts); waiting for the editor endpoint."
        Wait-PipelineDescriptorRefresh -Root $Root -ObservedUtc $state.attemptStartedUtc | Out-Null
    }.GetNewClosure()
    return Invoke-WithTransportRetry -Action $action -Attempts $Attempts -BeforeRetry $beforeRetry
}

function Resolve-VerificationSuites {
    param([Parameter(Mandatory)]$Registry, [string]$Profile='smoke', [string[]]$Suite=@())
    $selection = if ($Suite.Count -gt 0) { $Suite } else {
        if ($Registry.profiles.PSObject.Properties.Name -notcontains $Profile) { throw "Unknown verification profile: $Profile" }
        $Registry.profiles.$Profile
    }
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($id in $selection) {
        if ([string]::IsNullOrWhiteSpace($id) -or $Registry.suites.PSObject.Properties.Name -notcontains $id) { throw "Unknown verification suite: $id" }
        if ($seen.Add($id)) { Write-Output $id }
    }
    if ($seen.Count -eq 0) { throw 'No verification suites selected.' }
}

function Get-DocumentationLinkFailures {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$DocumentPath, [AllowEmptyString()][string]$Content)
    $prefix=[IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    $directory=[IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($DocumentPath))
    foreach ($match in [regex]::Matches($Content,'\]\((?<target>[^)]+)\)')) {
        $target=$match.Groups['target'].Value.Trim().Trim('<','>')
        if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $target.StartsWith('#')) { continue }
        $target=[Uri]::UnescapeDataString(($target -split '#',2)[0])
        if ([string]::IsNullOrWhiteSpace($target)) { continue }
        $resolved=[IO.Path]::GetFullPath([IO.Path]::Combine($directory,$target))
        if (!$resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $resolved)) {
            Write-Output "$DocumentPath -> $target"
        }
    }
}

Export-ModuleMember -Function Assert-PipelineResponse,Get-SourceIdentity,Invoke-UnityCommand,Resolve-VerificationSuites,Get-DocumentationLinkFailures,Test-TransportFailure,Invoke-WithTransportRetry,Wait-PipelineDescriptorRefresh,Get-UnityTransportRetryCount,Reset-UnityTransportRetryCount
