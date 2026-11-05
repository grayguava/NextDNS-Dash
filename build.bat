@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist bin\app_x64 mkdir bin\app_x64
if not exist bin\DLLs mkdir bin\DLLs

"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /out:bin\app_x64\NextDNS-Dash.exe ^
  /win32icon:assets\icon.ico ^
  /reference:lib\Microsoft.Web.WebView2.Core.dll ^
  /reference:lib\Microsoft.Web.WebView2.WinForms.dll ^
  src\*.cs || exit /b 1

copy /y lib\*.dll bin\DLLs\ >nul
echo Built bin\app_x64\NextDNS-Dash.exe
