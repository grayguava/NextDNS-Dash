$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw "Missing $csc" }

function Read-Properties([string]$Path) {
    $map = @{}
    foreach ($raw in Get-Content -LiteralPath $Path) {
        $line = $raw.Trim()
        if ($line.Length -eq 0 -or $line[0] -eq '#' -or $line[0] -eq ';') { continue }
        $eq = $line.IndexOf('=')
        if ($eq -lt 1) { continue }
        $key = $line.Substring(0, $eq).Trim().ToLowerInvariant()
        $value = $line.Substring($eq + 1).Trim()
        $map[$key] = $value
    }
    return $map
}

function ConvertTo-CsString([string]$Value) {
    $escaped = $Value.Replace('\', '\\').Replace('"', '\"').Replace("`r", '\r').Replace("`n", '\n')
    return '"' + $escaped + '"'
}

function ConvertTo-Rgb([string]$Value) {
    if ($Value -notmatch '^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$') { return $null }
    $hex = $Value.Substring(1)
    if ($hex.Length -eq 3) {
        $hex = '{0}{0}{1}{1}{2}{2}' -f $hex[0], $hex[1], $hex[2]
    }
    return @(
        [Convert]::ToInt32($hex.Substring(0, 2), 16),
        [Convert]::ToInt32($hex.Substring(2, 2), 16),
        [Convert]::ToInt32($hex.Substring(4, 2), 16)
    )
}

function Get-Luminance($Rgb) {
    $weights = @(0.2126, 0.7152, 0.0722)
    $sum = 0.0
    for ($i = 0; $i -lt 3; $i++) {
        $c = $Rgb[$i] / 255.0
        if ($c -le 0.03928) { $c = $c / 12.92 }
        else { $c = [Math]::Pow(($c + 0.055) / 1.055, 2.4) }
        $sum += $weights[$i] * $c
    }
    return $sum
}

function Get-Contrast($A, $B) {
    $la = Get-Luminance $A
    $lb = Get-Luminance $B
    if ($la -lt $lb) { $t = $la; $la = $lb; $lb = $t }
    return ($la + 0.05) / ($lb + 0.05)
}

function ConvertTo-HexCode($Rgb) {
    return '#{0:x2}{1:x2}{2:x2}' -f $Rgb[0], $Rgb[1], $Rgb[2]
}

$profilePath = Join-Path $root 'build\.properties'
if (-not (Test-Path -LiteralPath $profilePath)) { throw "Missing build\.properties" }
$props = Read-Properties $profilePath

foreach ($key in @('appname', 'servicename', 'homeurl', 'allowedhosts')) {
    if (-not $props.ContainsKey($key) -or -not $props[$key]) {
        throw "build\.properties: missing $key"
    }
}

$appName = $props['appname']
if ($appName -notmatch '^[A-Za-z0-9][A-Za-z0-9-]*$') {
    throw "build\.properties: appName must use ASCII letters, digits and hyphens"
}

$serviceName = $props['servicename']
if ($serviceName -match '[\x00-\x1F\x7F]') {
    throw "build\.properties: serviceName contains control characters"
}

$homeUrl = $props['homeurl']
$homeUri = $null
if (-not [Uri]::TryCreate($homeUrl, [UriKind]::Absolute, [ref]$homeUri) -or $homeUri.Scheme -ne 'https') {
    throw "build\.properties: homeUrl must be an absolute https URL"
}

$hosts = @($props['allowedhosts'].Split(',') |
    ForEach-Object { $_.Trim().ToLowerInvariant() } |
    Where-Object { $_ })
if ($hosts.Count -eq 0) { throw "build\.properties: allowedHosts is empty" }
foreach ($hostName in $hosts) {
    if ($hostName -notmatch '^[a-z0-9]([a-z0-9.-]*[a-z0-9])?$' -or $hostName.Contains('..')) {
        throw "build\.properties: invalid allowedHosts entry '$hostName'"
    }
}

$serviceAfnName = $props['serviceafnname']
if ($null -eq $serviceAfnName) { $serviceAfnName = '' }

$serviceAfnUrl = $props['serviceafnurl']
if ($null -eq $serviceAfnUrl) { $serviceAfnUrl = '' }
if ($serviceAfnUrl -and $serviceAfnUrl -notmatch '^https?://') {
    throw "build\.properties: serviceAfnUrl must start with http:// or https://"
}

$projectUrl = $props['projecturl']
if ($null -eq $projectUrl) { $projectUrl = '' }
if ($projectUrl -and $projectUrl -notmatch '^https?://') {
    throw "build\.properties: projectUrl must start with http:// or https://"
}

$logo = $props['logo']
if (-not $logo) { $logo = 'assets\icon.svg' }
$icon = $props['icon']
if (-not $icon) { $icon = 'assets\icon.ico' }

$colorsPath = Join-Path $root 'build\.colors'
if (-not (Test-Path -LiteralPath $colorsPath)) { throw "Missing build\.colors" }
$colorProps = Read-Properties $colorsPath

$colorNames = [ordered]@{
    bg = 'Bg'
    surface = 'Surface'
    surfaceHover = 'SurfaceHover'
    line = 'Line'
    text = 'Text'
    muted = 'Muted'
    mutedDim = 'MutedDim'
    accent = 'Accent'
    accentHover = 'AccentHover'
    danger = 'Danger'
    dangerHover = 'DangerHover'
}

$palette = @{}
foreach ($key in $colorNames.Keys) {
    $value = $colorProps[$key.ToLowerInvariant()]
    if (-not $value) { throw "build\.colors: missing $key" }
    $rgb = ConvertTo-Rgb $value
    if ($null -eq $rgb) { throw "build\.colors: $key must be #rgb or #rrggbb" }
    $palette[$key] = $rgb
}

$contrastChecks = @(
    @('text', 'bg'),
    @('text', 'surface'),
    @('accent', 'bg'),
    @('danger', 'bg')
)
foreach ($pair in $contrastChecks) {
    $ratio = Get-Contrast $palette[$pair[0]] $palette[$pair[1]]
    if ($ratio -lt 4.5) {
        throw ('build\.colors: {0} on {1} is {2:0.0}:1, need at least 4.5:1' -f $pair[0], $pair[1], $ratio)
    }
}

# Text on a filled accent or danger surface: pick whichever of black or white
# wins, so a light accent does not get unreadable white labels.
function Get-OnColor($Rgb) {
    if ((Get-Luminance $Rgb) -gt 0.179) { return @(0, 0, 0) }
    return @(255, 255, 255)
}

$accentText = Get-OnColor $palette['accent']
$dangerText = Get-OnColor $palette['danger']


$required = @(
    $logo,
    $icon,
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

$hostList = ($hosts | ForEach-Object { ConvertTo-CsString $_ }) -join ', '
$siteInfo = @"
static class SiteInfo
{
    public const string AppName = $(ConvertTo-CsString $appName);
    public const string ServiceName = $(ConvertTo-CsString $serviceName);
    public const string HomeUrl = $(ConvertTo-CsString $homeUrl);
    public static readonly string[] AllowedHosts = new string[] { $hostList };
}
"@
Set-Content -LiteralPath 'obj\SiteInfo.cs' -Value $siteInfo -Encoding UTF8

$themeLines = New-Object System.Collections.Generic.List[string]
$themeLines.Add('using System.Drawing;')
$themeLines.Add('')
$themeLines.Add('// Generated from build\.colors. Do not edit.')
$themeLines.Add('static class ThemeInfo')
$themeLines.Add('{')
foreach ($key in $colorNames.Keys) {
    $rgb = $palette[$key]
    $themeLines.Add(('    public static readonly Color {0} = Color.FromArgb(0x{1:x2}, 0x{2:x2}, 0x{3:x2});' -f $colorNames[$key], $rgb[0], $rgb[1], $rgb[2]))
}
$themeLines.Add(('    public static readonly Color AccentText = Color.FromArgb(0x{0:x2}, 0x{1:x2}, 0x{2:x2});' -f $accentText[0], $accentText[1], $accentText[2]))
$themeLines.Add(('    public static readonly Color DangerText = Color.FromArgb(0x{0:x2}, 0x{1:x2}, 0x{2:x2});' -f $dangerText[0], $dangerText[1], $dangerText[2]))
$themeLines.Add('}')
Set-Content -LiteralPath 'obj\ThemeInfo.cs' -Value $themeLines -Encoding ascii

$core = Join-Path $cache 'Microsoft.Web.WebView2.Core.dll'
$winforms = Join-Path $cache 'Microsoft.Web.WebView2.WinForms.dll'

& $csc /nologo /target:winexe /platform:x64 /optimize+ /out:"bin\$appName.exe" `
    "/win32icon:$icon" `
    /recurse:src\*.cs obj\BuildInfo.cs obj\SiteInfo.cs obj\ThemeInfo.cs `
    "/reference:$core" `
    "/reference:$winforms"
if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }

Get-ChildItem 'bin' -Filter *.exe -File |
    Where-Object { $_.Name -ne "$appName.exe" } |
    Remove-Item -Force

if (Test-Path -LiteralPath 'bin\UI') { Remove-Item -LiteralPath 'bin\UI' -Recurse -Force }
New-Item -ItemType Directory -Path 'bin\UI' | Out-Null
Copy-Item -Path 'ui\*' -Destination 'bin\UI' -Recurse -Force
Copy-Item -LiteralPath 'assets\MonaSans\MonaSansVF.woff2' -Destination 'bin\UI\MonaSansVF.woff2' -Force
Copy-Item -LiteralPath 'assets\MonaSans\OFL.txt' -Destination 'bin\UI\OFL-MonaSans.txt' -Force
Copy-Item -LiteralPath $logo -Destination 'bin\UI\logo.svg' -Force

$themeCss = New-Object System.Collections.Generic.List[string]
$themeCss.Add(':root {')
foreach ($key in $colorNames.Keys) {
    $cssKey = ($key -creplace '([A-Z])', '-$1').ToLowerInvariant()
    $themeCss.Add(('    --{0}: {1};' -f $cssKey, (ConvertTo-HexCode $palette[$key])))
}
$themeCss.Add(('    --accent-text: {0};' -f (ConvertTo-HexCode $accentText)))
$themeCss.Add(('    --accent-soft: rgba({0}, {1}, {2}, 0.35);' -f $palette['accent'][0], $palette['accent'][1], $palette['accent'][2]))
$themeCss.Add(('    --danger-text: {0};' -f (ConvertTo-HexCode $dangerText)))
$themeCss.Add('}')
Set-Content -LiteralPath 'bin\UI\theme.css' -Value $themeCss -Encoding ascii

$siteJs = [ordered]@{
    serviceName = $serviceName
    homeUrl = $homeUrl
    homeLabel = 'Open ' + $serviceName
    serviceAfnName = $serviceAfnName
    serviceAfnUrl = $serviceAfnUrl
    projectUrl = $projectUrl
} | ConvertTo-Json -Compress
Set-Content -LiteralPath 'bin\UI\site.js' -Value ('window.SITE = ' + $siteJs + ';') -Encoding UTF8

$uiRequired = @(
    'bin\UI\index.html',
    'bin\UI\config.html',
    'bin\UI\app.css',
    'bin\UI\app.js',
    'bin\UI\theme.css',
    'bin\UI\site.js',
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

Write-Host "Built bin\$appName.exe ($appVer)"
