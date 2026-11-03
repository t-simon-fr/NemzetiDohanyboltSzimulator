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

Export-ModuleMember -Function Assert-PipelineResponse,Get-SourceIdentity,Invoke-UnityCommand
