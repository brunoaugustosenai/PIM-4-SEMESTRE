$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Write-Host '== NAXAPA build .NET 10 ==' -ForegroundColor Cyan
$ver=& dotnet --version
if(-not $ver.StartsWith('10.')){throw "É necessário .NET 10 SDK. Encontrado: $ver"}
Push-Location $root
try {
  dotnet restore .\Naxapa.Web\Naxapa.Web.csproj
  dotnet build .\Naxapa.Web\Naxapa.Web.csproj -c Release --no-restore
  dotnet test .\Naxapa.Web.Tests\Naxapa.Web.Tests.csproj -c Release
  dotnet publish .\Naxapa.Web\Naxapa.Web.csproj -c Release -o .\artifacts\web
  dotnet publish .\Naxapa.Pdv\Naxapa.Pdv.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\artifacts\pdv
  dotnet publish .\Naxapa.PrintAgent\Naxapa.PrintAgent.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\artifacts\print-agent
  if(Get-Command flutter -ErrorAction SilentlyContinue){& .\scripts\Prepare-Mobile.ps1;Push-Location .\Naxapa.Mobile;try{flutter pub get;flutter analyze;flutter test;flutter build apk --release;New-Item -ItemType Directory -Force ..\artifacts\mobile|Out-Null;Copy-Item .\build\app\outputs\flutter-apk\app-release.apk ..\artifacts\mobile\NAXAPA-Mobile.apk -Force}finally{Pop-Location}}else{Write-Warning 'Flutter não encontrado; Mobile não foi compilado.'}
} finally { Pop-Location }
Write-Host 'Build concluído. Consulte a pasta artifacts.' -ForegroundColor Green
