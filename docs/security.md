# Security and privacy

## Private mode

Private mode is the default. The app opens an InPrivate WebView2 profile, so
cookies, cache, history and the dashboard session are not saved across restarts.
Closing the app signs you out. A small eye-off badge sits in the bottom-left of
every page, including the dashboard, while InPrivate is active.

Private mode is not a no-write mode. WebView2 still creates and uses a user data
folder for shared resources, and the app writes `data\appConfig.ini` for settings
and `data\state.ini` for window bounds. The app clears `data\profile` before each
private session and again on close. Closing shows a short clearing prompt while
the profile is removed. If a WebView2 child process still holds the folder at
close, the next private start clears it before the browser starts.
Pagefile, hibernation and crash dumps are outside the app's control; full-disk
encryption covers those.

Downloads are the only user-requested writes outside the app folder. With
downloads on, files save to the user's Downloads folder.

If the installed WebView2 runtime does not support InPrivate, the app falls back
to a normal profile that is still cleared at startup and close. The badge is not
shown in that fallback because the session is not actually InPrivate.

## Saved session (opt-in)

**Save my session** turns private mode off. The profile in `data\profile` then
persists, so the dashboard stays signed in. That profile is ordinary files
readable by anything running as your Windows user, and anyone who can run the
app gets the saved dashboard.

## Switching modes

- Enabling private mode asks for confirmation, deletes `data\profile` and signs
  you out before the private session starts.
- A missing or corrupt `data\appConfig.ini` means private mode. On startup the app
  clears any stale profile, so deleting the config cannot reveal an old
  session.
- The mode is a plain setting and can be edited outside the app. The private
  badge shows the effective mode, so an unexpected change is visible. The flag
  controls future behavior; it cannot reveal a session that was already
  cleared.

## What this does not protect

- The running app: private mode keeps the session in memory, but anyone at the
  keyboard can use the open window.
- Persistent mode: a copied folder or another process running as your user can
  read the saved profile.
- The disk: pagefile, hibernation, crash dumps, VSS and backups. Use BitLocker
  or device encryption.
- A compromised Windows account: the app cannot separate two people who share
  the same account.

For a real boundary, use a separate Windows account and NTFS permissions, or
full-disk encryption, not an app-level flag.

## Network policy

- In-app navigation stays on HTTPS hosts listed in `build\.properties`
  (`nextdns.io` for this build), plus the local Home and Settings pages.
- Other http(s) links open in the default browser; other schemes are dropped.
- Popups pointed at an allowed host load in the same window; everything else
  goes to the default browser.
- The app denies camera, microphone, notifications, geolocation and every other
  web permission request.
- Downloads are refused unless the Settings toggle allows them.

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
