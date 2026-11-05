using System;
using System.Diagnostics;

static class Policy
{
    static readonly string[] AllowedHosts = { "nextdns.io" };

    public static bool IsAllowed(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return false;
        if (u.Scheme != Uri.UriSchemeHttps) return false;
        foreach (string host in AllowedHosts)
            if (u.Host == host || u.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public static void OpenExternal(string uri)
    {
        Uri u;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out u)) return;
        if (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp) return;
        Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
    }
}
