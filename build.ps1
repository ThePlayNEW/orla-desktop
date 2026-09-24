$ErrorActionPreference = 'Stop'
dotnet build (Join-Path $PSScriptRoot 'Orla.slnx') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
