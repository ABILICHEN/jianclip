param([string]$OutputPath = 'dist\JianClip.exe')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outDir=Split-Path $OutputPath -Parent
if ($outDir) { New-Item -ItemType Directory -Force $outDir | Out-Null }
$sources=Get-ChildItem src\*.cs | ForEach-Object { $_.FullName }
& $csc /nologo /target:winexe /optimize+ "/out:$OutputPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll "/reference:$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\WPF\System.Speech.dll" /reference:System.IO.Compression.dll /resource:data\offline-catalog.zip,JianClip.Catalog.zip /resource:data\knowledge.json,JianClip.Knowledge.json $sources
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
