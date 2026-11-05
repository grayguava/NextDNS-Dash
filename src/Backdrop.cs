using System;
using System.Runtime.InteropServices;

static class Backdrop
{
    [StructLayout(LayoutKind.Sequential)]
    struct Margins { public int Left, Right, Top, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct AccentPolicy { public int State, Flags, GradientColor, AnimationId; }

    [StructLayout(LayoutKind.Sequential)]
    struct CompositionData { public int Attribute; public IntPtr Data; public int Size; }

    const int RGB = 0x212529;
    const int WCA_ACCENT_POLICY = 19;
    const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;
    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    const int DWMWA_BORDER_COLOR = 34;
    const int DWMWA_CAPTION_COLOR = 35;
    const int DWMWA_TEXT_COLOR = 36;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    [DllImport("user32.dll")]
    static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref CompositionData data);

    static int windowsBuild = -1;

    public static void Apply(IntPtr hwnd)
    {
        int r = (RGB >> 16) & 0xFF;
        int g = (RGB >> 8) & 0xFF;
        int b = RGB & 0xFF;
        bool dark = (r * 299 + g * 587 + b * 114) / 1000 < 128;

        int darkMode = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, 4);

        if (WindowsBuild() >= 22000)
        {
            int caption = (b << 16) | (g << 8) | r; // COLORREF is 0x00BBGGRR
            int text = dark ? 0xFFFFFF : 0x000000;
            int border = caption;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, 4);
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, 4);
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref border, 4);
            return;
        }

        // Windows 10: no caption-colour API, so tint the frame via the accent policy.
        var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);

        int opaque = unchecked((int)(0xFF000000u | (uint)((b << 16) | (g << 8) | r)));
        var accent = new AccentPolicy
        {
            State = ACCENT_ENABLE_ACRYLICBLURBEHIND,
            Flags = 0,
            GradientColor = opaque,
            AnimationId = 0
        };

        int size = Marshal.SizeOf(typeof(AccentPolicy));
        IntPtr block = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, block, false);
            var data = new CompositionData { Attribute = WCA_ACCENT_POLICY, Data = block, Size = size };
            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(block);
        }
    }

    // Environment.OSVersion reports 6.2 on apps without a Windows 10 manifest.
    static int WindowsBuild()
    {
        if (windowsBuild < 0)
        {
            windowsBuild = 0;
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                int build;
                if (key != null && int.TryParse(key.GetValue("CurrentBuild") as string, out build))
                    windowsBuild = build;
            }
        }
        return windowsBuild;
    }
}
