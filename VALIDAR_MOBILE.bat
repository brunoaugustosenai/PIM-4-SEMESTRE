@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo NAXAPA MOBILE - VALIDACAO COMPLETA
echo ============================================================

powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Prepare-Mobile.ps1"
if errorlevel 1 goto :erro

pushd Naxapa.Mobile

echo.
echo [1/4] flutter doctor -v
flutter doctor -v
if errorlevel 1 echo [AVISO] flutter doctor encontrou pendencias. Veja acima.

echo.
echo [2/4] flutter analyze
flutter analyze
if errorlevel 1 goto :erro_mobile

echo.
echo [3/4] flutter test
flutter test
if errorlevel 1 goto :erro_mobile

echo.
echo [4/4] flutter build apk --debug
flutter build apk --debug
if errorlevel 1 goto :erro_mobile

popd
echo.
echo ============================================================
echo VALIDACAO MOBILE APROVADA
echo APK DEBUG:
echo Naxapa.Mobile\build\app\outputs\flutter-apk\app-debug.apk
echo ============================================================
pause
exit /b 0

:erro_mobile
popd
:erro
echo.
echo ============================================================
echo VALIDACAO MOBILE FALHOU
ECHO Veja a primeira mensagem de erro acima.
echo ============================================================
pause
exit /b 1
