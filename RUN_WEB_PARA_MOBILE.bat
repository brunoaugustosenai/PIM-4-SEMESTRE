@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo NAXAPA WEB/API - ACESSO PARA MOBILE NA REDE LOCAL
echo ============================================================
dotnet run --project Naxapa.Web\Naxapa.Web.csproj --urls http://0.0.0.0:5180
