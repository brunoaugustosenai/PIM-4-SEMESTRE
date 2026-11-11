@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Prepare-Mobile.ps1"
if errorlevel 1 exit /b 1
pushd Naxapa.Mobile
flutter clean
flutter pub get
flutter build apk --debug
set ERR=%ERRORLEVEL%
popd
if errorlevel 1 exit /b %ERR%
echo.
echo APK: Naxapa.Mobile\build\app\outputs\flutter-apk\app-debug.apk
pause
