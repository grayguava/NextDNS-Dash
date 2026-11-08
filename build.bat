@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo Missing %CSC%
  exit /b 1
)

for %%f in (assets\icon.ico assets\icon.svg assets\MonaSans\MonaSansVF.woff2 assets\MonaSans\OFL.txt ui\index.html ui\config.html ui\app.css ui\app.js) do (
  if not exist "%%f" (
    echo Missing %%f
    exit /b 1
  )
)

if not exist bin\DLLs mkdir bin\DLLs
if not exist bin\config mkdir bin\config
if not exist obj mkdir obj

set "APPVER="
for /f "usebackq delims=" %%v in ("version") do if not defined APPVER set "APPVER=%%v"
set "APPVER=%APPVER:v=%"
if not defined APPVER set "APPVER=dev"
> obj\BuildInfo.cs echo static class BuildInfo { public const string Version = "%APPVER%"; }

"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /out:bin\NextDNS-Dash.exe ^
  /win32icon:assets\icon.ico ^
  /recurse:src\*.cs obj\BuildInfo.cs ^
  /reference:lib\Microsoft.Web.WebView2.Core.dll ^
  /reference:lib\Microsoft.Web.WebView2.WinForms.dll || exit /b 1

if exist bin\ui rmdir /S /Q bin\ui
mkdir bin\ui
xcopy /E /I /Y /Q ui bin\ui >nul || exit /b 1
copy /y assets\MonaSans\MonaSansVF.woff2 bin\ui\ >nul || exit /b 1
copy /y assets\MonaSans\OFL.txt bin\ui\OFL-MonaSans.txt >nul || exit /b 1
copy /y assets\icon.svg bin\ui\logo.svg >nul || exit /b 1

for %%f in (bin\ui\index.html bin\ui\config.html bin\ui\app.css bin\ui\app.js bin\ui\logo.svg bin\ui\MonaSansVF.woff2 bin\ui\OFL-MonaSans.txt) do (
  if not exist "%%f" (
    echo Missing %%f
    exit /b 1
  )
)

copy /y lib\*.dll bin\DLLs\ >nul || exit /b 1
echo Built bin\NextDNS-Dash.exe (%APPVER%)
