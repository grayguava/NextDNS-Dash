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
    'ui\lock.html',
    'ui\app.css',
    'ui\app.js',
    'ui\lock.js'
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
New-Item -ItemType Directory -Path 'bin\DLLs', 'bin\config', 'obj' -Force | Out-Null

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

if (Test-Path -LiteralPath 'bin\ui') { Remove-Item -LiteralPath 'bin\ui' -Recurse -Force }
New-Item -ItemType Directory -Path 'bin\ui' | Out-Null
Copy-Item -Path 'ui\*' -Destination 'bin\ui' -Recurse -Force
Copy-Item -LiteralPath 'assets\MonaSans\MonaSansVF.woff2' -Destination 'bin\ui\MonaSansVF.woff2' -Force
Copy-Item -LiteralPath 'assets\MonaSans\OFL.txt' -Destination 'bin\ui\OFL-MonaSans.txt' -Force
Copy-Item -LiteralPath 'assets\icon.svg' -Destination 'bin\ui\logo.svg' -Force

$uiRequired = @(
    'bin\ui\index.html',
    'bin\ui\config.html',
    'bin\ui\lock.html',
    'bin\ui\app.css',
    'bin\ui\app.js',
    'bin\ui\lock.js',
    'bin\ui\logo.svg',
    'bin\ui\MonaSansVF.woff2',
    'bin\ui\OFL-MonaSans.txt'
)
foreach ($file in $uiRequired) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Missing $file" }
}

foreach ($dll in $dlls) {
    Copy-Item -LiteralPath (Join-Path $cache $dll) -Destination (Join-Path $root 'bin\DLLs') -Force
}

Write-Host "Built bin\NextDNS-Dash.exe ($appVer)"
