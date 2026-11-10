# Build and release

## Requirements

- Windows 10 or 11, x64
- .NET Framework 4.x, for the bundled `csc.exe`
- Network access on the first build, for the pinned WebView2 SDK. No NuGet or
  Visual Studio.

## Build

```
build.bat
```

`build.bat` runs `build\fetch-deps.ps1`, then `build\build.ps1`.

The first run downloads the pinned WebView2 SDK into `build\cache\webview2\`,
checks the package and DLL hashes, and keeps it there. Later runs reuse the
cache; `build\fetch-deps.ps1 -Force` downloads it again.

`build\build.ps1` compiles `src\` recursively into `bin\NextDNS-Dash.exe`,
embeds `assets\icon.ico`, stages the cached SDK DLLs in `bin\DLLs\`, and
assembles `bin\UI\` from `ui\` plus the Mona Sans font and logo.

The compiler is C# 5, so keep the source free of newer language features. The
scripts fail fast when a required file is missing.

## Source layout

```
src\App\
  Program.cs        entry point, single instance, data folder
  Settings.cs       data\appConfig.ini
  State.cs          data\state.ini (window bounds, last URL)
src\Shell\
  MainForm.cs       window, WebView, navigation, offline page
  ClearingForm.cs   prompt shown while the private profile is removed
  Backdrop.cs       DWM caption and border colours
  Theme.cs          shared colours
src\Web\
  Policy.cs         allowed URLs, external links
  WebUi.cs          virtual host mapping, state JSON
  PrivateBadge.cs   InPrivate badge injected into every page
ui\                 Home and Settings
assets\             icon and Mona Sans
build\
  fetch-deps.ps1    downloads and verifies the WebView2 SDK
  build.ps1         compiles and stages bin\
  webview2.sha256   pinned SDK DLL hashes
  cache\            downloaded SDK (ignored)
```

## Editing the UI

The pages in `ui\` are plain HTML, CSS and JavaScript. Edit them, run
`build.bat`, and the result lands in `bin\UI\`.

- Pages are served from `https://nextdns-dash.localhost/` through
  `SetVirtualHostNameToFolderMapping`.
- Each page sets a strict Content-Security-Policy (`default-src 'self'`,
  `connect-src 'none'`, no inline code, no forms), so scripts and styles must
  be separate local files and nothing may be loaded remotely.
- `index.html` is Home and `config.html` is Settings. Shared styles are in
  `app.css`; `app.js` handles the Settings page and the host messages.
- Pages post `init` to request state. The host replies with
  `{"type":"state","version":"...","downloads":true|false,"saveSession":true|false,"dataDir":"..."}`.
- In private mode the host injects `PrivateBadge.cs` with
  `AddScriptToExecuteOnDocumentCreatedAsync`, so the badge also appears on the
  dashboard and the offline page.
- When you add a UI file, add it to the file lists in `build\build.ps1` and to
  `uiFiles` and `required` in `.github/workflows/release.yml`.

## WebView2 SDK

The project pins the WebView2 SDK to 1.0.4314.35; the Chromium engine itself
comes from the system-wide runtime. `build\fetch-deps.ps1` downloads the NuGet
package, checks its SHA-256, and extracts three files into
`build\cache\webview2\`:

- `Microsoft.Web.WebView2.Core.dll` from `lib/net462/`
- `Microsoft.Web.WebView2.WinForms.dll` from `lib/net462/`
- `WebView2Loader.dll` from `runtimes/win-x64/native/`

The expected DLL hashes are in `build\webview2.sha256`. To bump the SDK, update
the version and package hash in `build\fetch-deps.ps1`, update the manifest, and
run `build.bat`. Delete `build\cache\webview2\` or pass `-Force` to refresh it.

```
Get-ChildItem build\cache\webview2\*.dll | Get-FileHash -Algorithm SHA256
```

## Release

`version` holds the version (for example `v1.1.0`). Pushing a change to that
file runs the release workflow, builds the app, packages
`<version>_x64.zip` and `<version>_x64.zip.sha256`, and creates a draft GitHub
release. The workflow starts from a clean checkout, so it downloads and verifies
the SDK on every run. The zip contains the exe, `DLLs\`, `UI\`, `LICENSE`,
`NOTICE`, `README.md` and `docs\`.

## Unsigned binary

The exe is not code-signed, so Windows SmartScreen may show a warning on first
run. Use **More info → Run anyway**, or verify the download against the
published `.sha256` file:

```
Get-FileHash <version>_x64.zip -Algorithm SHA256
```
