# NextDNS-Dash

Portable Windows desktop wrapper for the [NextDNS](https://my.nextdns.io)
dashboard. One small exe, no installer and no background service: it opens a
local Home screen in a native window backed by the WebView2 runtime and
launches the dashboard on demand. The browsing profile stays in the same folder
when you choose to save your session; private mode is the default.

> [!NOTE]
> This is an unofficial, community-made client. It is not affiliated with,
> endorsed by, sponsored by, or supported by NextDNS. **NextDNS** and the
> NextDNS logo are trademarks of their respective owner and are used here only
> to describe what this app connects to. No trademark rights are claimed.

## Requirements

- Windows 10 or 11, x64
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
  (already present on current Windows builds)
- To build: the `csc.exe` bundled with .NET Framework 4.x. The first build
  downloads the pinned WebView2 SDK; no NuGet or Visual Studio.

## Run

1. Download `<version>_x64.zip` from the latest release.
2. Unzip it anywhere and run `NextDNS-Dash.exe`.

The first launch creates `data\profile` beside the exe. In private mode the
session is cleared when you close the app; turn on **Save my session** to keep
it. `data\appConfig.ini` appears when you change a setting. To start over, use
**Settings → Clear data**; the exe, `DLLs\`, `UI\` and your settings are kept.

## Docs

- [Usage](docs/usage.md): Home, dashboard, settings, private mode, data and the
  portable layout.
- [Security and privacy](docs/security.md): private mode, the network policy,
  and the Chromium traffic that is switched off.
- [Build and release](docs/build.md): `build.bat`, source layout, editing the
  UI, releases and the unsigned-binary note.

## License

MIT; see [LICENSE](LICENSE). Third-party notices, including the Microsoft Edge WebView2
SDK (BSD-3-Clause) and Mona Sans (SIL OFL 1.1), are in [NOTICE](NOTICE).
