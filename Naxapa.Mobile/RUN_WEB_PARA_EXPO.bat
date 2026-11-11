@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0.."
echo Iniciando NAXAPA Web/API para acesso pelo celular...
dotnet run --project Naxapa.Web\Naxapa.Web.csproj --urls http://0.0.0.0:5180
