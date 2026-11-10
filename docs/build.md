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

`build\build.ps1` reads `build\.properties` and `build\.colors`, validates them,
and compiles `src\` recursively into `bin\<appName>.exe`. The profile supplies
the exe name, the user-facing service name, the Home URL, the in-app host
allowlist, the logo, the icon and the Settings affiliation line. The build
embeds `obj\SiteInfo.cs` and `obj\ThemeInfo.cs`, stages the cached SDK DLLs in
`bin\DLLs\`, and assembles `bin\UI\` from `ui\` plus the Mona Sans font, the
logo and generated `site.js` and `theme.css`.

The compiler is C# 5, so keep the source free of newer language features. The
scripts fail fast when the profile or palette is invalid, or a required file is
missing.

## Site profile

`build\.properties` defines what the wrapper wraps:

```properties
appName=NextDNS-Dash
serviceName=NextDNS Dashboard
homeUrl=https://my.nextdns.io
allowedHosts=nextdns.io
```

`appName` is the machine identity: exe file name, single-instance mutex and the
local virtual host. Use ASCII letters, digits and hyphens. `serviceName` is
the display name; the Home button becomes **Open <serviceName>** and the
offline page names it. `allowedHosts` is a comma-separated allowlist; each
entry also allows its subdomains. `serviceAfnName` and `serviceAfnUrl` are
optional and build the Settings disclaimer, with the company name linked to
that URL. `projectUrl` adds the source link.

Change the file, run `build.bat`, and the next build is a different wrapper.
The allowlist is compiled in, so it is not a text file a user can repoint at
runtime. Rebuilding is the supported way to retarget the app.

## Colors

`build\.colors` sets the palette for one wrapper:

```properties
bg=#212529
surface=#2c3136
surfaceHover=#363c42
line=#3a4047
text=#e9ecef
muted=#adb5bd
mutedDim=#91989f
accent=#00a6ff
accentHover=#33b8ff
danger=#ff7b72
dangerHover=#ff8f87
```

Every key is required and must be `#rgb` or `#rrggbb`. The build rejects a
palette if any of these pairs falls below 4.5:1 contrast: text on bg, text on
surface, accent on bg, danger on bg.

Derived at build time: the text color on filled accent and danger surfaces
(black or white, whichever contrasts more) and the switch-on track (accent at
35% alpha). The title bar uses bg and picks black or white caption text from
it; the dialog backdrop stays black at 60%. The InPrivate badge keeps its fixed
colors because it sits over third-party pages.

The build writes `obj\ThemeInfo.cs` for the window chrome and
`bin\UI\theme.css` for the pages. `ui\app.css` is layout only; change colors in
`build\.colors`.

## Source layout

```
build\.properties  site profile: appName, URL, allowlist, branding
build\.colors     palette for the window chrome and the pages
src\App\
  Program.cs        entry point, single instance, data folder
  SiteProfile.cs    profile values and host matching
  Settings.cs       data\appConfig.ini
  State.cs          data\state.ini (window bounds, last URL)
src\Shell\
  MainForm.cs       window, WebView, navigation, offline page
  ClearingForm.cs   prompt shown while the private profile is removed
  Backdrop.cs       DWM caption and border colors
  Theme.cs          shell colors from ThemeInfo
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

- Pages are served from `https://<appName>.localhost/` through
  `SetVirtualHostNameToFolderMapping`, using the lowercased `appName`.
- `site.js` is generated from `build\.properties` at build time. `app.js` applies
  the service name, Home label, Home URL, logo alt text and affiliation, so the
  pages stay generic.
- `theme.css` is generated from `build\.colors` at build time and loads before
  `app.css`, which keeps the layout rules and reads the palette variables.
- Each page sets a strict Content-Security-Policy (`default-src 'self'`,
  `connect-src 'none'`, no inline code, no forms), so scripts and styles must
  be separate local files and nothing may be loaded remotely.
- `index.html` is Home and `config.html` is Settings. Layout styles are in
  `app.css`; the palette comes from `theme.css`. `app.js` handles the Settings
  page and the host messages.
- Pages post `init` to request state. The host replies with
  `{"type":"state","version":"...","downloads":true|false,"saveSession":true|false,"dataDir":"..."}`.
- Home posts `open-dashboard`; the host opens the last dashboard URL it knows,
  or the default dashboard when there is none.
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
the SDK on every run. The zip contains `<appName>.exe`, `DLLs\`, `UI\`,
`LICENSE`, `NOTICE`, `README.md` and `docs\`, with the package folder named
after the profile.

## Unsigned binary

The exe is not code-signed, so Windows SmartScreen may show a warning on first
run. Use **More info → Run anyway**, or verify the download against the
published `.sha256` file:

```
Get-FileHash <version>_x64.zip -Algorithm SHA256
```
