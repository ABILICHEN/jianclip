param([string]$AppPath='dist\JianClip.exe',[switch]$Integration)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testExe=Join-Path (Split-Path $AppPath) 'SmokeTests.exe'
& $csc /nologo /target:exe "/out:$testExe" "/reference:$AppPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll tests\SmokeTests.cs tests\KnowledgeTests.cs
if ($LASTEXITCODE -ne 0) {throw 'Test compilation failed'}
& $testExe
if ($LASTEXITCODE -ne 0) {throw 'Tests failed'}
if ($Integration) {
 $integrationExe=Join-Path (Split-Path $AppPath) 'IntegrationTests.exe'
 & $csc /nologo /target:exe "/out:$integrationExe" "/reference:$AppPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll tests\IntegrationTests.cs
 if ($LASTEXITCODE -ne 0) {throw 'Integration compilation failed'}
 & $integrationExe
 if ($LASTEXITCODE -ne 0) {throw 'Integration tests failed'}
}
