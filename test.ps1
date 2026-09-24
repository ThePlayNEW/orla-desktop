$ErrorActionPreference = 'Stop'
$executable = Join-Path $PSScriptRoot 'Orla.exe'
$reportDirectory = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
$reportPath = Join-Path $reportDirectory 'tests.txt'
$process = Start-Process -FilePath $executable -ArgumentList @('--self-test', ('"' + $reportPath + '"')) -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(30000)) { Stop-Process -Id $process.Id; throw 'Tests timed out.' }
Get-Content -LiteralPath $reportPath
if ($process.ExitCode -ne 0) { throw 'Tests failed.' }
