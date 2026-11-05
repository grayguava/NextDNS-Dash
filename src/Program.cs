using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [STAThread]
    static void Main()
    {
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
