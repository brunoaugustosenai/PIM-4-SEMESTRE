@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
echo ==========================================
echo NAXAPA MOBILE EXPO - PREPARACAO
echo ==========================================
where node >nul 2>&1 || (echo [ERRO] Node.js nao encontrado.& pause & exit /b 1)
where npm >nul 2>&1 || (echo [ERRO] npm nao encontrado.& pause & exit /b 1)
echo [1/4] Instalando dependencias...
call npm install || goto :erro
echo [2/4] Ajustando pacotes Expo para o SDK do projeto...
call npx expo install expo-sqlite expo-secure-store || goto :erro
echo [3/4] Verificando dependencias...
call npx expo install --check || goto :erro
echo [4/4] TypeScript...
call npx tsc --noEmit || goto :erro
echo.
echo [OK] NAXAPA Mobile Expo preparado.
echo Rode RUN_EXPO.bat para abrir o QR Code.
pause
exit /b 0
:erro
echo.
echo [ERRO] A preparacao falhou. Veja a primeira mensagem de erro acima.
pause
exit /b 1
