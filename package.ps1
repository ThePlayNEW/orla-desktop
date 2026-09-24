param(
    [string]$Version,       # defaults to AssemblyVersion in src/Orla/AssemblyInfo.cs
    [switch]$SkipPublish,   # reuse files already in artifacts/publish (for example, after signing)
    [switch]$KeepReleases   # keep artifacts/releases, so vpk can build a delta from a downloaded release
)
$ErrorActionPreference = 'Stop'
$artifacts = Join-Path $PSScriptRoot 'artifacts'
$publish = Join-Path $artifacts 'publish'
$releases = Join-Path $artifacts 'releases'

if (-not $Version) {
    $info = Get-Content (Join-Path $PSScriptRoot 'src\Orla\AssemblyInfo.cs') -Raw
    if ($info -notmatch 'AssemblyVersion\("(\d+\.\d+\.\d+)') { throw 'AssemblyVersion not found.' }
    $Version = $Matches[1]
}

if (-not $SkipPublish) {
    Remove-Item -LiteralPath $publish -Recurse -Force -ErrorAction SilentlyContinue
    dotnet publish (Join-Path $PSScriptRoot 'src\Orla\Orla.csproj') -c Release -o $publish --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
}

# Plain zip: the published files plus the documentation. Built on every run, with or without vpk.
$zip = Join-Path $artifacts "orla-$Version-windows-x64.zip"
$files = @(Get-ChildItem -LiteralPath $publish | ForEach-Object FullName)
$files += @('README.md', 'LICENSE', 'docs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
$outputs = @($zip)

# Velopack installer, portable zip and update packages.
if (Get-Command vpk -ErrorAction SilentlyContinue) {
    if (-not $KeepReleases) { Remove-Item -LiteralPath $releases -Recurse -Force -ErrorAction SilentlyContinue }
    vpk pack --packId OrlaDesktop --packVersion $Version --packDir $publish --mainExe Orla.exe `
        --packTitle 'Orla Desktop' --packAuthors 'Eduardo Torres' `
        --icon (Join-Path $PSScriptRoot 'src\Orla\Orla.ico') `
        --runtime win-x64 --framework net48 --shortcuts StartMenuRoot --outputDir $releases
    if ($LASTEXITCODE -ne 0) { throw 'vpk pack failed.' }
    $outputs += @('OrlaDesktop-win-Setup.exe', 'OrlaDesktop-win-Portable.zip', "OrlaDesktop-$Version-*.nupkg") |
        ForEach-Object { Get-ChildItem (Join-Path $releases $_) | ForEach-Object FullName }
}
else {
    Write-Warning 'vpk not found; skipping installer. Install it with: dotnet tool install -g vpk'
}

Get-FileHash -LiteralPath $outputs -Algorithm SHA256 | ForEach-Object {
    '{0}  {1}  ({2:N0} bytes)' -f $_.Hash, (Split-Path $_.Path -Leaf), (Get-Item -LiteralPath $_.Path).Length
}
