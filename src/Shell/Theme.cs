using System.Drawing;

// Shell colors, generated from build\.colors into ThemeInfo by the build.
static class Theme
{
    // Title bar and window background.
    public static readonly Color Bg = ThemeInfo.Bg;
    public static readonly Color Surface = ThemeInfo.Surface;
    public static readonly Color SurfaceHover = ThemeInfo.SurfaceHover;
    public static readonly Color Line = ThemeInfo.Line;
    public static readonly Color Text = ThemeInfo.Text;
    public static readonly Color Muted = ThemeInfo.Muted;
    public static readonly Color Accent = ThemeInfo.Accent;
    public static readonly Color AccentHover = ThemeInfo.AccentHover;
    public static readonly Color AccentText = ThemeInfo.AccentText;

    public static string Hex(Color color)
    {
        return "#" + color.R.ToString("x2") + color.G.ToString("x2") + color.B.ToString("x2");
    }
}
