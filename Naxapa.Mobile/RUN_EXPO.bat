@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
echo ==========================================
echo NAXAPA MOBILE - EXPO GO
echo ==========================================
if not exist node_modules (
  echo Dependencias ainda nao instaladas.
  echo Execute PREPARAR_EXPO.bat primeiro.
  pause
  exit /b 1
)
call npx expo start --lan --clear
