$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /optimize+ /out:dist\JianClip.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll src\JianClip.cs
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
