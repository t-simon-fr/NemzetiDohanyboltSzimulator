Set-StrictMode -Version Latest

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

function Invoke-UnityCommand {
    param([Parameter(Mandatory)][string]$Root,[Parameter(Mandatory)][string]$Command,$Parameters=@{})
    $json=$Parameters | ConvertTo-Json -Depth 15 -Compress
    $raw=& (Join-Path $Root 'Tools/Invoke-Pipeline.ps1') -Command $Command -Parameters $json
    $response=($raw -join "`n") | ConvertFrom-Json
    return Assert-PipelineResponse -Response $response -Context $Command
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

Export-ModuleMember -Function Assert-PipelineResponse,Get-SourceIdentity,Invoke-UnityCommand,Resolve-VerificationSuites,Get-DocumentationLinkFailures
