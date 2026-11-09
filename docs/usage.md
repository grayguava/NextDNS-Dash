# Usage

## Home

The app opens Home: the NextDNS logo, **Open NextDNS Dashboard**, and
**Settings**. The dashboard opens in the same window and signs in through the
NextDNS site.

Private mode is the default, so the session is not saved and you sign in again
after each restart. Turn on **Save my session** in Settings to keep the session.
While InPrivate is active, a small eye-off badge stays in the bottom-left corner
of every page, including the dashboard.

## Settings

### Downloads

Allow or block downloads. On by default; allowed files save to the user's
Downloads folder. The preference lives in `data\appConfig.ini` and survives
**Clear data**.

### Session

**Save my session** is off by default. When it is off, the app runs in private
mode: cookies, cache, history and the session are not saved, and closing the app
signs you out.

When it is on, the profile in `data\profile` persists and the dashboard stays
signed in. That profile is ordinary files readable by anything running as your
Windows user.

Turning **Save my session** off asks for confirmation, then deletes the saved
profile and signs you out. The preference lives in `data\appConfig.ini` and survives
**Clear data**.

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
UI\
  index.html
  config.html
  app.css
  app.js
  logo.svg
  MonaSansVF.woff2
  OFL-MonaSans.txt
LICENSE
NOTICE
README.md
docs\
data\        created on first run (appConfig.ini, state.ini, profile\)
```

## Window and input

- One instance only; a second launch focuses the existing window.
- Window size, position and maximized state are restored. The last dashboard
  URL is kept in memory for offline retry and browser-process restarts; in
  private mode it is not written to disk. Every launch opens Home. Private mode
  signs you out; **Save my session** keeps you signed in.
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
