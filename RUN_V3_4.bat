@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
where dotnet >nul 2>&1 || (echo [ERRO] .NET SDK nao encontrado.& pause & exit /b 1)
echo Iniciando NAXAPA em http://localhost:5180 ...
dotnet run --project Naxapa.Web\Naxapa.Web.csproj --urls http://localhost:5180
pause
