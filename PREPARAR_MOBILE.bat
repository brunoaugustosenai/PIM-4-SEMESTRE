@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo NAXAPA MOBILE - PREPARAR FLUTTER / ANDROID
echo ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Prepare-Mobile.ps1"
if errorlevel 1 (
  echo.
  echo [ERRO] Falha na preparacao do mobile.
  pause
  exit /b 1
)
echo.
echo [OK] Mobile preparado.
pause
