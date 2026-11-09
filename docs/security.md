# Security and privacy

## What the lock protects

The launch lock is a gate in front of the UI. While it is up, the app lets the
WebView load only `ui\lock.html`. It cancels dashboard and Settings navigation,
refuses popups, cancels downloads, and sends the lock page no app state. Windows
Hello verification uses
`Windows.Security.Credentials.UI.UserConsentVerifier`, so PIN and account
password work wherever Windows offers them.

It does not encrypt anything. The session and cookies live as ordinary files in
`data\profile`, readable by anything running as your Windows user. At rest, the
real controls are your Windows account and full-disk encryption (BitLocker or
device encryption). The lock stops someone walking up to an open app; it does
not protect a copied folder or a compromised account.

If Windows Hello cannot run, the lock screen offers **Try again** and **Unable
to sign in? Reset the app**. Reset deletes `data\`, turns the lock off, and
opens Home signed out, so no saved session survives. If deletion fails, the app
stays locked and asks you to close it and delete the data folder.

## Network policy

- In-app navigation stays on HTTPS `nextdns.io` and its subdomains, plus the
  local Home, Settings and lock pages.
- Other http(s) links open in the default browser; other schemes are dropped.
- Popups pointed at `nextdns.io` load in the same window; everything else goes
  to the default browser.
- The app denies camera, microphone, notifications, geolocation and every other
  web permission request.
- Downloads are refused unless the Settings toggle allows them. Downloads are
  always refused while the lock is up.

## Chromium traffic

The wrapper is a privacy tool, so the embedded browser runs with Microsoft's
own traffic switched off. SmartScreen reputation checks are off, and the WebView
starts with:

```
--disable-background-networking
--disable-component-update
--disable-domain-reliability
--disable-sync
--disable-breakpad
--disable-client-side-phishing-detection
--no-pings
```

Measured over 75 idle seconds after Home settled, using TCP samples for the
whole WebView process tree and a Chromium netlog for request URLs:

| | Before | After |
|---|---|---|
| External hosts | 5 | 0 |
| TCP endpoints | 2 | 0 |

The hosts that disappeared were `config.edge.skype.com`,
`edge.microsoft.com`, and three `*-edge.smartscreen.microsoft.com` telemetry
endpoints.

The wrapper itself makes no network connections. All remaining traffic is the
dashboard talking to NextDNS.

## Unsigned binary

The exe is not code-signed. See [Build and release](build.md#unsigned-binary)
for the SmartScreen note and the hash check.
