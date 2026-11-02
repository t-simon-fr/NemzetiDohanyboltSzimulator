param([Parameter(Mandatory=$true)][string]$Command,[string]$Parameters='{}')
$ErrorActionPreference='Stop'
$descriptor=Join-Path $PSScriptRoot '../Library/Pipeline/.unity-pipeline-port'
for($attempt=0;$attempt -lt 12 -and !(Test-Path -LiteralPath $descriptor);$attempt++){Start-Sleep -Milliseconds 500}
$connection=Get-Content -LiteralPath $descriptor -Raw | ConvertFrom-Json
$headers=@{Authorization=('Bearer '+$connection.evalToken)}
$body=@{command=$Command;parameters=($Parameters|ConvertFrom-Json)}|ConvertTo-Json -Depth 12 -Compress
Invoke-RestMethod ('http://127.0.0.1:'+$connection.port+'/api/exec') -Method Post -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 55 | ConvertTo-Json -Depth 15
