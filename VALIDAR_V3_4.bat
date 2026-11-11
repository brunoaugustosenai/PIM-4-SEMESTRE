@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
echo ============================================================
echo NAXAPA v3.4 - VALIDACAO DE BUILD E TESTES
ECHO ============================================================
where dotnet >nul 2>&1 || (echo [ERRO] .NET SDK nao encontrado.& pause & exit /b 1)
for /f "delims=" %%V in ('dotnet --version') do set DOTNET_VERSION=%%V
echo .NET: %DOTNET_VERSION%
echo.

echo [1/4] Limpando caches...
if exist "Naxapa.Web\bin" rmdir /s /q "Naxapa.Web\bin"
if exist "Naxapa.Web\obj" rmdir /s /q "Naxapa.Web\obj"
if exist "Naxapa.Web.Tests\bin" rmdir /s /q "Naxapa.Web.Tests\bin"
if exist "Naxapa.Web.Tests\obj" rmdir /s /q "Naxapa.Web.Tests\obj"

echo [2/4] Restaurando pacotes...
dotnet restore NAXAPA.sln
if errorlevel 1 goto :fail

echo [3/4] Compilando Gestao/API...
dotnet build Naxapa.Web\Naxapa.Web.csproj -c Debug --no-restore
if errorlevel 1 goto :fail

echo [4/4] Executando testes .NET...
dotnet test Naxapa.Web.Tests\Naxapa.Web.Tests.csproj -c Debug
if errorlevel 1 goto :fail

echo.
echo ============================================================
echo VALIDACAO APROVADA
ECHO O teste de integracao usa NaxapaDB_Test_* temporario e nao altera
ECHO o NaxapaDB principal.
echo ============================================================
pause
exit /b 0

:fail
echo.
echo ============================================================
echo VALIDACAO FALHOU - veja o primeiro erro acima.
echo ============================================================
pause
exit /b 1
