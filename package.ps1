$ErrorActionPreference = 'Stop'
$artifactDirectory = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
$packageFiles = @('Orla.exe', 'Orla.exe.config', 'README.md', 'LICENSE') | ForEach-Object { Join-Path $PSScriptRoot $_ }
Compress-Archive -LiteralPath $packageFiles -DestinationPath (Join-Path $artifactDirectory 'orla-windows-x64.zip') -Force
Get-FileHash -LiteralPath (Join-Path $artifactDirectory 'orla-windows-x64.zip') -Algorithm SHA256
