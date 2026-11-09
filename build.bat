@echo off
setlocal
pushd "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "build\fetch-deps.ps1"
if errorlevel 1 goto fail

powershell -NoProfile -ExecutionPolicy Bypass -File "build\build.ps1"
if errorlevel 1 goto fail

popd
exit /b 0

:fail
popd
exit /b 1
