$ErrorActionPreference = 'Stop'
$artifacts = Join-Path $PSScriptRoot 'artifacts'
$publish = Join-Path $artifacts 'publish'
Remove-Item -LiteralPath $publish -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $PSScriptRoot 'src\Orla\Orla.csproj') -c Release -o $publish --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$files = @('Orla.exe', 'Orla.exe.config') | ForEach-Object { Join-Path $publish $_ }
$files += @('README.md', 'LICENSE', 'docs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
$zip = Join-Path $artifacts 'orla-windows-x64.zip'
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
Get-FileHash -LiteralPath $zip -Algorithm SHA256
