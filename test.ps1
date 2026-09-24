$ErrorActionPreference = 'Stop'
dotnet test (Join-Path $PSScriptRoot 'Orla.slnx') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
