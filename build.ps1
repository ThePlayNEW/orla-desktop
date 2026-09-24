$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
$wpf=Join-Path $framework 'WPF'
$refs=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','Microsoft.CSharp.dll','System.Xaml.dll')|ForEach-Object{"/reference:$(Join-Path $framework $_)"}
$refs+=@('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll')|ForEach-Object{"/reference:$(Join-Path $wpf $_)"}
$files=Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs'|ForEach-Object FullName
$iconArgs=@()
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Orla.ico')){$iconArgs=@("/win32icon:$(Join-Path $PSScriptRoot 'Orla.ico')")}
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$(Join-Path $PSScriptRoot 'Orla.exe')" "/win32manifest:$(Join-Path $PSScriptRoot 'src\Orla.manifest')" @iconArgs @refs @files
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar o Orla'}
Write-Output 'Orla compilado.'
