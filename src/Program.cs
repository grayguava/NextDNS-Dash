using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetDllDirectory(string path);

    [STAThread]
    static void Main()
    {
        LoadBundledAssemblies();

        bool created;
        using (var mutex = new Mutex(true, @"Local\NextDNS-Dash.SingleInstance", out created))
        {
            if (!created)
            {
                FocusExisting();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // The release layout keeps the WebView2 files in a sibling DLLs folder, which
    // neither the CLR nor the native loader searches by default.
    static void LoadBundledAssemblies()
    {
        string dllDir = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "DLLs"));
        if (!Directory.Exists(dllDir)) return;

        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
        {
            string candidate = Path.Combine(dllDir, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        };

        SetDllDirectory(dllDir);
    }

    static void FocusExisting()
    {
        int self = Process.GetCurrentProcess().Id;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            foreach (Process p in Process.GetProcessesByName("NextDNS-Dash"))
            {
                if (p.Id == self) continue;
                IntPtr h = p.MainWindowHandle;
                if (h == IntPtr.Zero) continue;
                ShowWindow(h, 9); // SW_RESTORE
                SetForegroundWindow(h);
                return;
            }
            Thread.Sleep(200);
        }
    }
}
