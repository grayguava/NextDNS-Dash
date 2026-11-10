using System;
using System.Diagnostics;

static class Policy
{
    public static bool IsService(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return false;
        if (u.Scheme != Uri.UriSchemeHttps) return false;
        return SiteProfile.IsAllowedHost(u.Host);
    }

    public static bool IsLocalPage(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return false;
        if (u.Scheme != Uri.UriSchemeHttps) return false;
        if (!string.Equals(u.Host, WebUi.LocalHost, StringComparison.OrdinalIgnoreCase)) return false;

        string path = u.AbsolutePath;
        return path == "/" || path == "/index.html" || path == "/config.html";
    }

    public static bool IsAllowed(string uri)
    {
        return IsService(uri) || IsLocalPage(uri);
    }

    public static void OpenExternal(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return;
        if (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp) return;
        Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
    }
}
