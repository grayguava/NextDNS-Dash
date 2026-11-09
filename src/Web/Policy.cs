using System;
using System.Diagnostics;

static class Policy
{
    const string DashboardHost = "nextdns.io";

    public static bool IsDashboard(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return false;
        if (u.Scheme != Uri.UriSchemeHttps) return false;
        return u.Host == DashboardHost ||
            u.Host.EndsWith("." + DashboardHost, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLocalPage(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return false;
        if (u.Scheme != Uri.UriSchemeHttps) return false;
        if (!string.Equals(u.Host, WebUi.LocalHost, StringComparison.OrdinalIgnoreCase)) return false;

        string path = u.AbsolutePath;
        return path == "/" || path == "/index.html" || path == "/config.html" ||
            path == "/lock.html";
    }

    public static bool IsAllowed(string uri)
    {
        return IsDashboard(uri) || IsLocalPage(uri);
    }

    public static void OpenExternal(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return;
        if (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp) return;
        Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
    }
}
