@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo NAXAPA MOBILE - EXECUTAR
ECHO ============================================================

powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Prepare-Mobile.ps1"
if errorlevel 1 goto :erro

pushd Naxapa.Mobile
flutter devices
echo.
echo Se houver mais de um dispositivo, o Flutter pedira para escolher.
echo.
flutter run
set ERR=%ERRORLEVEL%
popd
exit /b %ERR%

:erro
echo [ERRO] Nao foi possivel preparar o mobile.
pause
exit /b 1
