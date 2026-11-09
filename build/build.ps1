$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw "Missing $csc" }

$required = @(
    'assets\icon.ico',
    'assets\icon.svg',
    'assets\MonaSans\MonaSansVF.woff2',
    'assets\MonaSans\OFL.txt',
    'ui\index.html',
    'ui\config.html',
    'ui\app.css',
    'ui\app.js'
)
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Missing $file" }
}

$cache = Join-Path $root 'build\cache\webview2'
$dlls = @(
    'Microsoft.Web.WebView2.Core.dll',
    'Microsoft.Web.WebView2.WinForms.dll',
    'WebView2Loader.dll'
)
foreach ($dll in $dlls) {
    if (-not (Test-Path -LiteralPath (Join-Path $cache $dll))) {
        throw "Missing $dll. Run build.bat to fetch the WebView2 SDK."
    }
}

if (Test-Path -LiteralPath 'bin\DLLs') { Remove-Item -LiteralPath 'bin\DLLs' -Recurse -Force }
if (Test-Path -LiteralPath 'bin\config') { Remove-Item -LiteralPath 'bin\config' -Recurse -Force }
New-Item -ItemType Directory -Path 'bin\DLLs', 'obj' -Force | Out-Null

$appVer = 'dev'
if (Test-Path -LiteralPath 'version') {
    $raw = (Get-Content -LiteralPath 'version' -Raw).Trim()
    if ($raw) { $appVer = $raw -replace '^v', '' }
}
$buildInfo = 'static class BuildInfo { public const string Version = "' + $appVer + '"; }'
Set-Content -LiteralPath 'obj\BuildInfo.cs' -Value $buildInfo -Encoding ascii

$core = Join-Path $cache 'Microsoft.Web.WebView2.Core.dll'
$winforms = Join-Path $cache 'Microsoft.Web.WebView2.WinForms.dll'

& $csc /nologo /target:winexe /platform:x64 /optimize+ /out:bin\NextDNS-Dash.exe `
    /win32icon:assets\icon.ico `
    /recurse:src\*.cs obj\BuildInfo.cs `
    "/reference:$core" `
    "/reference:$winforms"
if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }

if (Test-Path -LiteralPath 'bin\UI') { Remove-Item -LiteralPath 'bin\UI' -Recurse -Force }
New-Item -ItemType Directory -Path 'bin\UI' | Out-Null
Copy-Item -Path 'ui\*' -Destination 'bin\UI' -Recurse -Force
Copy-Item -LiteralPath 'assets\MonaSans\MonaSansVF.woff2' -Destination 'bin\UI\MonaSansVF.woff2' -Force
Copy-Item -LiteralPath 'assets\MonaSans\OFL.txt' -Destination 'bin\UI\OFL-MonaSans.txt' -Force
Copy-Item -LiteralPath 'assets\icon.svg' -Destination 'bin\UI\logo.svg' -Force

$uiRequired = @(
    'bin\UI\index.html',
    'bin\UI\config.html',
    'bin\UI\app.css',
    'bin\UI\app.js',
    'bin\UI\logo.svg',
    'bin\UI\MonaSansVF.woff2',
    'bin\UI\OFL-MonaSans.txt'
)
foreach ($file in $uiRequired) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Missing $file" }
}

foreach ($dll in $dlls) {
    Copy-Item -LiteralPath (Join-Path $cache $dll) -Destination (Join-Path $root 'bin\DLLs') -Force
}

Write-Host "Built bin\NextDNS-Dash.exe ($appVer)"
