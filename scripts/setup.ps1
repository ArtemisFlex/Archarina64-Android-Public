param([string]$AndroidSdkDirectory = "$env:LOCALAPPDATA\Android\Sdk", [string]$JavaSdkDirectory = 'C:\Program Files\Android\Android Studio\jbr')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$sdkRoot = "$env:LOCALAPPDATA\Archarina64Android\dotnet"
New-Item -ItemType Directory -Path $sdkRoot -Force | Out-Null
if (!(Test-Path "$sdkRoot\dotnet.exe")) {
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile "$sdkRoot\dotnet-install.ps1"
    & "$sdkRoot\dotnet-install.ps1" -Version 10.0.401 -InstallDir $sdkRoot -NoPath
    if ($LASTEXITCODE -ne 0) { throw '.NET SDK installation failed.' }
}
if (!(Test-Path "$JavaSdkDirectory\bin\java.exe")) { throw 'Pass -JavaSdkDirectory with a JDK 21 installation.' }
Push-Location $repoRoot
try {
    & "$sdkRoot\dotnet.exe" workload install android --skip-manifest-update
    if ($LASTEXITCODE -ne 0) { throw 'Android workload installation failed.' }
    & "$sdkRoot\dotnet.exe" build src/Archarina64.Android -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=$AndroidSdkDirectory" "-p:JavaSdkDirectory=$JavaSdkDirectory" -p:AcceptAndroidSdkLicenses=True
    if ($LASTEXITCODE -ne 0) { throw 'Android dependency installation failed.' }
} finally { Pop-Location }
