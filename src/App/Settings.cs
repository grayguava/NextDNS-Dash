using System;
using System.IO;
using System.Text;

static class Settings
{
    public static bool DownloadsEnabled { get; private set; }

    static Settings()
    {
        DownloadsEnabled = true;
    }

    static string FilePath
    {
        get { return Path.Combine(Program.ConfigDirectory, "app.ini"); }
    }

    public static void Load()
    {
        DownloadsEnabled = true;

        try
        {
            if (!File.Exists(FilePath)) return;

            foreach (string raw in File.ReadAllLines(FilePath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

                int eq = line.IndexOf('=');
                if (eq < 0) continue;

                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string value = line.Substring(eq + 1).Trim().ToLowerInvariant();

                if (key == "downloads")
                    DownloadsEnabled = value != "off" && value != "0" && value != "false";
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static bool SetDownloads(bool enabled)
    {
        if (enabled == DownloadsEnabled) return false;

        DownloadsEnabled = enabled;
        Save();
        return true;
    }

    static void Save()
    {
        var text = new StringBuilder();
        text.AppendLine("# NextDNS-Dash settings");
        text.AppendLine("downloads=" + (DownloadsEnabled ? "on" : "off"));

        try
        {
            Directory.CreateDirectory(Program.ConfigDirectory);
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
}
