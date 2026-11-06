## What's new

### Features

- remembers window position, size, maximized state and the last dashboard page
- shows a calm offline page and retries automatically when the connection returns
- rebuilds the embedded browser if its process exits

### Fixes

- portable layout: the executable, runtime files, config and user data now sit together, so the zip unpacks to a clean folder
- window dragging and resizing no longer stutter on windows 10

### Security

- navigation is limited to https nextdns.io and its subdomains; other links open in the default browser
- camera, microphone, notification and geolocation prompts are denied
- devtools, autofill and password autosave are off

### Improvements

- releases are built and drafted from a version file by github actions

### Docs

- README covers requirements, the portable layout and the non-affiliation notice

**Downloads**

- `<tag>_x64.zip` - portable build, unzip anywhere and run `NextDNS-Dash.exe`
- `<tag>_x64.zip.sha256` - checksum for the zip

**Requires** the Microsoft Edge WebView2 Runtime, already present on current Windows builds.

**Commit**:
```
<commit>
```

**Full Changelog**: https://github.com/grayguava/NextDNS-Dash/compare/v1.0.0...<tag>
