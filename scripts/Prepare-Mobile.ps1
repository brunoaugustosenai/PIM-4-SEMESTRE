param(
    [switch]$ForceRegenerate
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$mobile = Join-Path $root 'Naxapa.Mobile'

if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) {
    throw 'Flutter não encontrado no PATH. Execute flutter doctor -v e corrija a instalação.'
}

Write-Host 'Flutter encontrado:' -ForegroundColor Cyan
flutter --version

$android = Join-Path $mobile 'android'
$metadata = Join-Path $mobile '.metadata'

if ($ForceRegenerate -and (Test-Path $android)) {
    Remove-Item $android -Recurse -Force
}

if (-not (Test-Path $android)) {
    $temp = Join-Path $env:TEMP ("naxapa_flutter_scaffold_" + [Guid]::NewGuid().ToString('N'))
    try {
        Write-Host 'Gerando scaffold Android compatível com o Flutter instalado...' -ForegroundColor Cyan
        flutter create --platforms=android --org br.com.naxapa --project-name naxapa_mobile $temp
        if ($LASTEXITCODE -ne 0) { throw 'flutter create falhou.' }

        Copy-Item (Join-Path $temp 'android') $android -Recurse -Force
        if (-not (Test-Path $metadata) -and (Test-Path (Join-Path $temp '.metadata'))) {
            Copy-Item (Join-Path $temp '.metadata') $metadata -Force
        }
    }
    finally {
        if (Test-Path $temp) {
            Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

$mainManifest = Join-Path $android 'app\src\main\AndroidManifest.xml'
if (-not (Test-Path $mainManifest)) {
    throw "AndroidManifest.xml não encontrado: $mainManifest"
}

$manifestText = Get-Content $mainManifest -Raw
if ($manifestText -notmatch 'android.permission.INTERNET') {
    $manifestText = $manifestText -replace '<manifest([^>]*)>', "<manifest`$1>`r`n    <uses-permission android:name=`"android.permission.INTERNET`" />"
    Set-Content -Path $mainManifest -Value $manifestText -Encoding UTF8
}

# HTTP local é permitido SOMENTE em debug, para 10.0.2.2/IP da rede local.
$debugManifestDir = Join-Path $android 'app\src\debug'
$debugResDir = Join-Path $debugManifestDir 'res\xml'
New-Item -ItemType Directory -Path $debugResDir -Force | Out-Null

$debugManifest = Join-Path $debugManifestDir 'AndroidManifest.xml'
@'
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <uses-permission android:name="android.permission.INTERNET" />
    <application
        android:usesCleartextTraffic="true"
        android:networkSecurityConfig="@xml/network_security_config" />
</manifest>
'@ | Set-Content -Path $debugManifest -Encoding UTF8

$networkConfig = Join-Path $debugResDir 'network_security_config.xml'
@'
<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <base-config cleartextTrafficPermitted="true" />
</network-security-config>
'@ | Set-Content -Path $networkConfig -Encoding UTF8

Push-Location $mobile
try {
    Write-Host 'Executando flutter pub get...' -ForegroundColor Cyan
    flutter pub get
    if ($LASTEXITCODE -ne 0) { throw 'flutter pub get falhou.' }
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'NAXAPA Mobile preparado com sucesso.' -ForegroundColor Green
Write-Host 'Próximo passo: .\VALIDAR_MOBILE.bat' -ForegroundColor Green
