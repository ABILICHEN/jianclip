param([string]$OutputPath = 'dist\JianClip.exe')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outDir=Split-Path $OutputPath -Parent
if ($outDir) { New-Item -ItemType Directory -Force $outDir | Out-Null }
$sources=Get-ChildItem src\*.cs | ForEach-Object { $_.FullName }
& $csc /nologo /target:winexe /optimize+ "/out:$OutputPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll $sources
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
