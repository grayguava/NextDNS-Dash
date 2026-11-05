@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist bin\DLLs mkdir bin\DLLs
if not exist bin\config mkdir bin\config

"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /out:bin\NextDNS-Dash.exe ^
  /win32icon:assets\icon.ico ^
  /reference:lib\Microsoft.Web.WebView2.Core.dll ^
  /reference:lib\Microsoft.Web.WebView2.WinForms.dll ^
  src\*.cs || exit /b 1

copy /y lib\*.dll bin\DLLs\ >nul
echo Built bin\NextDNS-Dash.exe
