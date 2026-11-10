using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

static class State
{
    static bool hasBounds;
    static Rectangle savedBounds;
    static bool maximized;
    static string lastUrl;

    public static bool HasBounds { get { return hasBounds; } }
    public static Rectangle Bounds { get { return savedBounds; } }
    public static bool Maximized { get { return maximized; } }
    public static string LastUrl { get { return lastUrl; } }

    static string FilePath
    {
        get { return Path.Combine(Program.DataDirectory, "state.ini"); }
    }

    public static void Load()
    {
        hasBounds = false;
        maximized = false;
        lastUrl = null;

        string path = FilePath;
        if (!File.Exists(path)) return;

        int x = 0, y = 0, width = 0, height = 0;
        bool haveX = false, haveY = false, haveWidth = false, haveHeight = false;

        try
        {
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

                int eq = line.IndexOf('=');
                if (eq < 0) continue;

                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string value = line.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "x": haveX = TryInt(value, out x); break;
                    case "y": haveY = TryInt(value, out y); break;
                    case "width": haveWidth = TryInt(value, out width); break;
                    case "height": haveHeight = TryInt(value, out height); break;
                    case "maximized": maximized = value == "1"; break;
                    case "last_url": lastUrl = SafeUrl(value); break;
                }
            }
        }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }

        if (Settings.PrivateMode) lastUrl = null;

        if (!haveX || !haveY || !haveWidth || !haveHeight) return;

        var bounds = new Rectangle(x, y, width, height);
        if (IsVisible(bounds))
        {
            savedBounds = bounds;
            hasBounds = true;
        }
    }

    public static void Save(Form form)
    {
        Rectangle bounds = form.WindowState == FormWindowState.Normal
            ? form.Bounds
            : form.RestoreBounds;

        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var text = new StringBuilder();
        text.AppendLine("x=" + bounds.X.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("y=" + bounds.Y.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("width=" + bounds.Width.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("height=" + bounds.Height.ToString(CultureInfo.InvariantCulture));
        text.AppendLine("maximized=" + (form.WindowState == FormWindowState.Maximized ? "1" : "0"));
        if (lastUrl != null && Settings.SaveSession) text.AppendLine("last_url=" + lastUrl);

        try
        {
            Directory.CreateDirectory(Program.DataDirectory);
            string path = FilePath;
            string temp = path + ".tmp";
            File.WriteAllText(temp, text.ToString(), Encoding.UTF8);
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    // Only scheme, host and path survive: query strings and fragments can carry tokens.
    public static void SaveUrl(string url)
    {
        string safe = SafeUrl(url);
        if (safe != null) lastUrl = safe;
    }

    static string SafeUrl(string url)
    {
        Uri uri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
        if (!Policy.IsService(uri.AbsoluteUri)) return null;
        return uri.GetLeftPart(UriPartial.Path);
    }

    static bool TryInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    static bool IsVisible(Rectangle bounds)
    {
        if (bounds.Width < 320 || bounds.Height < 240) return false;

        foreach (Screen screen in Screen.AllScreens)
        {
            Rectangle overlap = Rectangle.Intersect(screen.WorkingArea, bounds);
            if (overlap.Width >= 100 && overlap.Height >= 100) return true;
        }

        return false;
    }
}
