[CmdletBinding()]
param([Parameter(Mandatory)][ValidateNotNullOrEmpty()][string[]]$Path)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $PSScriptRoot 'Workflow.Common.psm1') -Force
$prefix=$root+[IO.Path]::DirectorySeparatorChar
$failures=@()
foreach ($item in $Path) {
    $document=[IO.Path]::GetFullPath([IO.Path]::Combine($root,$item))
    if (!$document.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $document -PathType Leaf)) { throw "Invalid repository document: $item" }
    $failures+=@(Get-DocumentationLinkFailures -Root $root -DocumentPath $document -Content (Get-Content -LiteralPath $document -Raw))
}
if ($failures.Count) { throw ($failures -join "`n") }
Write-Output "Documentation links: $($Path.Count) files passed. No Unity command executed."
