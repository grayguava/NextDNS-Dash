using System;
using System.IO;
using System.Text;
using Microsoft.Web.WebView2.Core;

static class WebUi
{
    public const string LocalHost = "nextdns-dash.localhost";
    public const string HomeUrl = "https://" + LocalHost + "/index.html";
    public const string ConfigUrl = "https://" + LocalHost + "/config.html";
    public const string LockUrl = "https://" + LocalHost + "/lock.html";

    public static string UiDirectory
    {
        get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui"); }
    }

    public static bool IsAvailable
    {
        get { return File.Exists(Path.Combine(UiDirectory, "index.html")); }
    }

    public static bool HasLockPage
    {
        get { return File.Exists(Path.Combine(UiDirectory, "lock.html")); }
    }

    public static bool IsLockUrl(string url)
    {
        return string.Equals(url, LockUrl, StringComparison.OrdinalIgnoreCase);
    }

    public static void Map(CoreWebView2 core)
    {
        if (!IsAvailable) return;
        core.SetVirtualHostNameToFolderMapping(
            LocalHost, UiDirectory, CoreWebView2HostResourceAccessKind.DenyCors);
    }

    public static bool IsLocalSource(string source)
    {
        Uri u;
        if (!Uri.TryCreate(source, UriKind.Absolute, out u)) return false;
        return u.Scheme == Uri.UriSchemeHttps &&
            string.Equals(u.Host, LocalHost, StringComparison.OrdinalIgnoreCase);
    }

    public static string StateJson(string lockError)
    {
        return "{\"type\":\"state\"" +
            ",\"version\":" + Quote(BuildInfo.Version) +
            ",\"downloads\":" + (Settings.DownloadsEnabled ? "true" : "false") +
            ",\"lock\":" + (Settings.LockOnLaunch ? "true" : "false") +
            ",\"lockAvailable\":" + (WindowsHello.IsAvailable ? "true" : "false") +
            (lockError == null ? "" : ",\"lockError\":" + Quote(lockError)) +
            ",\"dataDir\":" + Quote(Program.DataDirectory) +
            "}";
    }

    public static string LockStateJson(string state, string message)
    {
        return "{\"type\":\"lock\"" +
            ",\"state\":" + Quote(state) +
            (message == null ? "" : ",\"message\":" + Quote(message)) +
            "}";
    }

    static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        var text = new StringBuilder("\"");
        foreach (char ch in value)
        {
            switch (ch)
            {
                case '\\': text.Append("\\\\"); break;
                case '"': text.Append("\\\""); break;
                case '\r': text.Append("\\r"); break;
                case '\n': text.Append("\\n"); break;
                default:
                    if (ch < ' ')
                        text.Append("\\u").Append(((int)ch).ToString("x4"));
                    else
                        text.Append(ch);
                    break;
            }
        }
        return text.Append('"').ToString();
    }
}
