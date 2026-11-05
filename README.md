# NextDNS-Dash

Portable Windows desktop wrapper around the [NextDNS](https://my.nextdns.io) dashboard.
One small exe, no installer, no background service: it opens the dashboard in a
native window backed by the WebView2 runtime and keeps its browsing profile in
the same folder.

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

```
bin\NextDNS-Dash.exe
```

The first launch creates `data\profile` beside the exe, next to `DLLs\` and
`config\`, and stores the session there, so the whole folder can be copied to
another machine and stay logged in. Deleting `data` resets the app to a fresh
login.

## Build

```
build.bat
```

Compiles `src\*.cs` into `bin\NextDNS-Dash.exe`, embeds `assets\icon.ico`, and
copies the WebView2 assemblies from `lib\` into `bin\DLLs\`. The app resolves
both managed and native runtime files from that subfolder, so the release zip
keeps the package root tidy. The `lib\` DLLs are the managed wrapper only
(`Microsoft.Web.WebView2` 1.0.4314.35); the Chromium engine itself comes from the
system-wide runtime.

## Behaviour

- Navigation is limited to HTTPS on `nextdns.io` and its subdomains. Anything
  else opens in your default browser.
- Popups pointed at that domain load in the app; everything else goes to the
  default browser.
- All permission requests (camera, microphone, notifications, geolocation) are
  denied.
- A second launch focuses the existing window instead of starting another copy.
- DevTools, the status bar, password autosave, and general autofill are off.
- The title bar shows the app icon and window buttons only. Windows 11 colours
  it exactly `#212529`; Windows 10 uses the same tint via the frame accent
  policy, so the taskbar label is intentionally empty.

The window has no address bar, back button, or reload control; navigation is
whatever the dashboard itself does.
