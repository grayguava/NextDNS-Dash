# NextDNS-Dash

Portable Windows desktop wrapper for the [NextDNS](https://my.nextdns.io) dashboard.
One small exe, no installer and no background service: it opens a local Home
screen in a native window backed by the WebView2 runtime and launches the
dashboard on demand. The browsing profile stays in the same folder.

> [!NOTE]
> This is an unofficial, community-made client. It is not affiliated with,
> endorsed by, sponsored by, or supported by NextDNS. **NextDNS** and the
> NextDNS logo are trademarks of their respective owner and are used here only
> to describe what this app connects to. No trademark rights are claimed.

## Requirements

- Windows 10 or 11, x64
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
  (already present on current Windows builds)
- To build: the `csc.exe` bundled with .NET Framework 4.x, nothing else

## Run

1. Download `<version>_x64.zip` from the latest release.
2. Unzip it anywhere and run `NextDNS-Dash.exe`.

The app starts on Home: the NextDNS logo, **Open NextDNS Dashboard**, and
**Settings**. The dashboard opens in the same window and signs in through the
NextDNS site. With **Require Windows sign-in on launch** turned on in Settings,
Windows asks for Windows Hello before Home appears.

The first launch creates `data\profile` beside the exe. The session lives there,
so the whole folder can be copied to another machine and stay signed in.
`config\app.ini` appears when you change a setting. To start over, use
**Settings → Clear data**; the exe, `DLLs\`, `ui\` and `config\` are kept.

### Portable layout

```
NextDNS-Dash.exe
DLLs\
  Microsoft.Web.WebView2.Core.dll
  Microsoft.Web.WebView2.WinForms.dll
  WebView2Loader.dll
ui\
  index.html
  config.html
  lock.html
  app.css
  app.js
  lock.js
  logo.svg
  MonaSansVF.woff2
  OFL-MonaSans.txt
LICENSE
NOTICE
README.md
data\        created on first run
config\      created when a setting changes
```

## Settings

- **Downloads** — allow or block downloads. On by default; allowed files save
  to the user's Downloads folder. The preference lives in `config\app.ini` and
  survives **Clear data**.
- **Security** — **Require Windows sign-in on launch** unlocks the app with
  Windows Hello, PIN, or the account password. Off by default; turn it on in
  Settings. When enabled, the dashboard stays hidden behind the lock screen at
  `ui\lock.html` until verification succeeds. If verification fails or Windows
  Hello is not available, the only ways out are **Try again** and the
  **Unable to sign in? Reset the app** link, which deletes the profile, session
  and window state and turns the lock off. The preference lives in
  `config\app.ini` and survives **Clear data**.
- **Data** — shows the profile path, opens the folder, and clears the profile,
  session and window state. Returns to Home with a fresh profile.
- **About** — version and non-affiliation note.

## Behaviour

- Navigation stays on HTTPS `nextdns.io` and its subdomains, plus the local Home
  and Settings pages. Other http(s) links open in the default browser; other
  schemes are ignored.
- Popups pointed at `nextdns.io` load in the same window; everything else goes
  to the default browser.
- Permission requests (camera, microphone, notifications, geolocation) are
  denied.
- One instance only; a second launch focuses the existing window.
- Window size, position and maximized state are restored. The last dashboard
  URL is used only for offline retry and browser-process restarts; every launch
  starts on Home.
- The embedded browser restarts if its process exits and reloads after a
  renderer crash.
- If the dashboard is unreachable, a short offline page offers **Back to home**
  and retries automatically when the network returns.
- Right-click is disabled throughout the app. Clicking the title bar icon,
  left or right, returns to Home; caption right-click and `Alt+Space` do
  nothing. Keyboard shortcuts still work.
- DevTools, the status bar, password autosave and general autofill are off.
  The window title is empty, so title-based password-manager auto-type has
  nothing to match.
- SmartScreen reputation checks are off, and Chromium's own background traffic
  (Edge config, component updates, domain reliability, sync, crash upload,
  pings) is disabled. Idle measurement went from five external hosts to none.
- The wrapper makes no network connections of its own; all traffic comes from
  the WebView2 runtime.

## Build

```
build.bat
```

The script compiles `src\*.cs` into `bin\NextDNS-Dash.exe`, embeds
`assets\icon.ico`, copies the vendored WebView2 SDK DLLs from `lib\` into
`bin\DLLs\`, and assembles `bin\ui\` from `ui\` plus the Mona Sans font in
`assets\MonaSans\`.

The WebView2 SDK is pinned to 1.0.4314.35; the Chromium engine itself comes from
the system-wide runtime. Vendored DLL hashes are in `lib\SHA256SUMS.txt`:

```
Get-ChildItem lib\*.dll | Get-FileHash -Algorithm SHA256
```

## Release

`version` holds the version (for example `v1.1.0`). Pushing a change to that
file runs the release workflow, builds the app, packages `<version>_x64.zip`
and `<version>_x64.zip.sha256`, and creates a draft GitHub release. The zip
contains the exe, `DLLs\`, `ui\`, `LICENSE`, `NOTICE` and this README.

## Unsigned binary

The exe is not code-signed, so Windows SmartScreen may show a warning on first
run. Use **More info → Run anyway**, or verify the download against the
published `.sha256` file:

```
Get-FileHash <version>_x64.zip -Algorithm SHA256
```

## License

MIT; see `LICENSE`. Third-party notices, including the Microsoft Edge WebView2
SDK (BSD-3-Clause) and Mona Sans (SIL OFL 1.1), are in `NOTICE`.
