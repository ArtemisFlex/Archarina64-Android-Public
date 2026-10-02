param([switch]$TestsOnly, [string]$AndroidSdkDirectory = "$env:LOCALAPPDATA\Android\Sdk", [string]$JavaSdkDirectory = 'C:\Program Files\Android\Android Studio\jbr')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$dotnetExe = "$env:LOCALAPPDATA\Archarina64Android\dotnet\dotnet.exe"
if (!(Test-Path $dotnetExe)) { $dotnetExe = (Get-Command dotnet -ErrorAction Stop).Source }
Push-Location $repoRoot
try {
    & $dotnetExe run --project tests/Archarina64.Core.Tests
    if ($LASTEXITCODE -ne 0) { throw 'Core regression checks failed.' }
    if (!$TestsOnly) {
        & $dotnetExe build src/Archarina64.Android -c Debug "-p:AndroidSdkDirectory=$AndroidSdkDirectory" "-p:JavaSdkDirectory=$JavaSdkDirectory"
        if ($LASTEXITCODE -ne 0) { throw 'Android build failed.' }
        New-Item -ItemType Directory -Path artifacts -Force | Out-Null
        Get-ChildItem src/Archarina64.Android/bin/Debug -Recurse -Filter '*-Signed.apk' | Copy-Item -Destination artifacts
    }
} finally { Pop-Location }
