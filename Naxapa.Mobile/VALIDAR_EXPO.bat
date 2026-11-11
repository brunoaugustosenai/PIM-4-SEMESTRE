@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
echo ==========================================
echo NAXAPA MOBILE EXPO - VALIDACAO
echo ==========================================
call npx expo-doctor || goto :erro
call npx expo install --check || goto :erro
call npx tsc --noEmit || goto :erro
echo.
echo VALIDACAO EXPO APROVADA
pause
exit /b 0
:erro
echo.
echo VALIDACAO EXPO FALHOU
pause
exit /b 1
