# Usage

## Home

Without the launch lock, the app opens Home: the NextDNS logo, **Open NextDNS
Dashboard**, and **Settings**. With the lock on, Home appears after you unlock.
The dashboard opens in the same window and signs in through the NextDNS site.

The first launch creates `data\profile` beside the exe. The session lives there,
so the whole folder can be copied to another machine and stay signed in.

## Settings

### Downloads

Allow or block downloads. On by default; allowed files save to the user's
Downloads folder. The preference lives in `config\app.ini` and survives
**Clear data**.

### Security

**Require Windows sign-in on launch** asks for Windows Hello, PIN, or your
account password before the app opens. Off by default; turn it on in Settings.

When enabled, the dashboard stays hidden behind the lock screen at
`ui\lock.html` until verification succeeds. If verification fails or Windows
Hello is not available, the only ways out are **Try again** and the **Unable to
sign in? Reset the app** link. Reset deletes the profile, session and window
state and turns the lock off, so nothing sensitive is left behind. A failed
delete keeps the app locked.

The preference lives in `config\app.ini` and survives **Clear data**.

### Data

Shows the profile path, opens the folder, and clears the profile, session and
window state. Returns to Home with a fresh profile. Settings are kept.

### About

Version and the non-affiliation note.

## Portable layout

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
docs\
data\        created on first run
config\      created when a setting changes
```

## Window and input

- One instance only; a second launch focuses the existing window.
- Window size, position and maximized state are restored. The last dashboard
  URL is used only for offline retry and browser-process restarts; every launch
  opens Home, after the lock screen when the lock is on.
- Right-click is disabled throughout the app. Clicking the title bar icon, left
  or right, returns to Home; caption right-click and `Alt+Space` do nothing.
  Keyboard shortcuts still work.
- DevTools, the status bar, password autosave and general autofill are off. The
  window title is empty, so title-based password-manager auto-type has nothing
  to match.
- The embedded browser restarts if its process exits and reloads after a
  renderer crash.

## Offline

If the dashboard is unreachable, a short offline page offers **Back to home**
and retries automatically when the network returns.
