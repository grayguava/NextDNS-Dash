param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$root = Split-Path $PSScriptRoot -Parent
$version = '1.0.4314.35'
$packageUrl = "https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/$version"
$packageSha256 = '90778deb1b77e36292828b4b5833c9255c27533f40e0ddd55891ae718944edc4'
$cache = Join-Path $root 'build\cache\webview2'
$marker = Join-Path $cache '.version'

# Get-FileHash is missing on some Windows PowerShell hosts; hash with .NET.
function Get-Sha256([string]$Path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        try {
            $bytes = $sha.ComputeHash($stream)
            return ([BitConverter]::ToString($bytes) -replace '-', '').ToLowerInvariant()
        }
        finally { $stream.Dispose() }
    }
    finally { $sha.Dispose() }
}

$entries = @(
    @{ Name = 'Microsoft.Web.WebView2.Core.dll';     Entry = 'lib/net462/Microsoft.Web.WebView2.Core.dll' },
    @{ Name = 'Microsoft.Web.WebView2.WinForms.dll'; Entry = 'lib/net462/Microsoft.Web.WebView2.WinForms.dll' },
    @{ Name = 'WebView2Loader.dll';                  Entry = 'runtimes/win-x64/native/WebView2Loader.dll' }
)

$manifest = Join-Path $PSScriptRoot 'webview2.sha256'
if (-not (Test-Path -LiteralPath $manifest)) { throw "Missing $manifest" }

$hashes = @{}
foreach ($line in Get-Content -LiteralPath $manifest) {
    if ($line -match '^\s*([0-9a-fA-F]{64})\s+(\S.*?)\s*$') {
        $hashes[$Matches[2]] = $Matches[1].ToLowerInvariant()
    }
}
foreach ($entry in $entries) {
    if (-not $hashes.ContainsKey($entry.Name)) { throw "No hash for $($entry.Name) in $manifest" }
}

$haveAll = $true
foreach ($entry in $entries) {
    if (-not (Test-Path -LiteralPath (Join-Path $cache $entry.Name))) { $haveAll = $false }
}

$cachedVersion = ''
if (Test-Path -LiteralPath $marker) {
    $raw = Get-Content -LiteralPath $marker -Raw
    if ($raw) { $cachedVersion = $raw.Trim() }
}

if (-not $Force -and $haveAll -and $cachedVersion -eq $version) {
    Write-Host "WebView2 SDK $version already cached"
    exit 0
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('webview2-' + [Guid]::NewGuid().ToString('N'))
$package = Join-Path $temp 'package.nupkg'

try {
    New-Item -ItemType Directory -Path $temp -Force | Out-Null

    Write-Host "Downloading WebView2 SDK $version"
    Invoke-WebRequest -Uri $packageUrl -OutFile $package -UseBasicParsing

    $actual = Get-Sha256 $package
    if ($actual -ne $packageSha256) {
        throw "Package hash mismatch: expected $packageSha256, got $actual"
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    New-Item -ItemType Directory -Path $cache -Force | Out-Null

    $zip = [IO.Compression.ZipFile]::OpenRead($package)
    try {
        foreach ($entry in $entries) {
            $source = $zip.GetEntry($entry.Entry)
            if (-not $source) { throw "Package missing $($entry.Entry)" }

            $file = Join-Path $temp $entry.Name
            [IO.Compression.ZipFileExtensions]::ExtractToFile($source, $file, $true)

            $actual = Get-Sha256 $file
            if ($actual -ne $hashes[$entry.Name]) {
                throw "Hash mismatch for $($entry.Name): expected $($hashes[$entry.Name]), got $actual"
            }
        }
    }
    finally {
        $zip.Dispose()
    }

    foreach ($entry in $entries) {
        Copy-Item -LiteralPath (Join-Path $temp $entry.Name) -Destination (Join-Path $cache $entry.Name) -Force
    }

    $shaDir = Join-Path $cache 'SHA256'
    New-Item -ItemType Directory -Path $shaDir -Force | Out-Null
    foreach ($entry in $entries) {
        $hash = Get-Sha256 (Join-Path $cache $entry.Name)
        Set-Content -LiteralPath (Join-Path $shaDir ($entry.Name + '.sha256')) -Value "$hash  $($entry.Name)" -Encoding ascii
    }

    Set-Content -LiteralPath $marker -Value $version -Encoding ascii
    Write-Host "WebView2 SDK $version cached in build\cache\webview2"
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
