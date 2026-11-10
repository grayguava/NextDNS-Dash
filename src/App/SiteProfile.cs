using System;

// Site identity comes from build\.properties, compiled into SiteInfo by the build.
static class SiteProfile
{
    public static string AppName { get { return SiteInfo.AppName; } }
    public static string ServiceName { get { return SiteInfo.ServiceName; } }
    public static string HomeUrl { get { return SiteInfo.HomeUrl; } }

    public static string LocalHost { get { return SiteInfo.AppName.ToLowerInvariant() + ".localhost"; } }

    public static bool IsAllowedHost(string host)
    {
        if (string.IsNullOrEmpty(host)) return false;

        foreach (string allowed in SiteInfo.AllowedHosts)
        {
            if (host.Equals(allowed, StringComparison.OrdinalIgnoreCase)) return true;
            if (host.EndsWith("." + allowed, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}
