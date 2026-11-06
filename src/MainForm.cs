using System;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

class MainForm : Form
{
    const string StartUrl = "https://my.nextdns.io";

    const string OfflineHtml =
        "<!doctype html><html><head><meta charset=\"utf-8\"><style>" +
        "html,body{height:100%;margin:0}" +
        "body{background:#212529;color:#e9ecef;font:15px/1.5 'Segoe UI',sans-serif;" +
        "display:flex;align-items:center;justify-content:center}" +
        ".box{text-align:center;max-width:420px;padding:24px}" +
        "h1{font-size:18px;font-weight:600;margin:0 0 8px}" +
        "p{margin:0 0 20px;color:#adb5bd}" +
        "a{display:inline-block;padding:8px 18px;border-radius:4px;background:#2c3136;" +
        "color:#e9ecef;text-decoration:none}a:hover{background:#3a4047}" +
        "</style></head><body><div class=\"box\">" +
        "<h1>Can't reach the dashboard</h1>" +
        "<p>The app retries automatically when the connection comes back.</p>" +
        "<a href=\"https://my.nextdns.io\">Try again</a>" +
        "</div></body></html>";

    WebView2 web;
    CoreWebView2 core;

    bool showingOffline;
    bool cancelledNavigation;
    bool restarting;
    bool closing;
    NetworkAvailabilityChangedEventHandler networkHandler;

    public MainForm()
    {
        Text = ""; // caption shows icon + buttons only
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Color.FromArgb(0x21, 0x25, 0x29);

        State.Load();
        if (State.HasBounds)
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = State.Bounds;
            if (State.Maximized) WindowState = FormWindowState.Maximized;
        }
        else
        {
            Width = 1035;
            Height = 665;
            StartPosition = FormStartPosition.CenterScreen;
        }

        CreateWebView();
        Load += OnLoad;

        networkHandler = delegate(object s, NetworkAvailabilityEventArgs e)
        {
            if (!e.IsAvailable) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (showingOffline && core != null)
                        core.Navigate(State.LastUrl ?? StartUrl);
                });
            }
            catch (InvalidOperationException) { }
        };
        NetworkChange.NetworkAvailabilityChanged += networkHandler;
    }

    void CreateWebView()
    {
        web = new WebView2();
        web.Dock = DockStyle.Fill;
        web.DefaultBackgroundColor = Color.FromArgb(0x21, 0x25, 0x29);
        Controls.Add(web);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Backdrop.Apply(Handle);
    }

    async void OnLoad(object sender, EventArgs e)
    {
        try
        {
            await InitAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "NextDNS-Dash could not start.\r\n\r\n" + ex.Message,
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
        }
    }

    async Task InitAsync()
    {
        try
        {
            CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (Exception)
        {
            MessageBox.Show(this,
                "The Microsoft Edge WebView2 runtime is not installed.\r\n\r\n" +
                "Install it from https://developer.microsoft.com/microsoft-edge/webview2/ and try again.",
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            return;
        }

        string profileDir = Path.Combine(Program.DataDirectory, "profile");

        CoreWebView2Environment env =
            await CoreWebView2Environment.CreateAsync(null, profileDir);
        if (closing) return;
        await web.EnsureCoreWebView2Async(env);
        if (closing) return;

        CoreWebView2 c = web.CoreWebView2;
        core = c;

        CoreWebView2Settings st = c.Settings;
        st.AreDevToolsEnabled = false;
        st.IsStatusBarEnabled = false;
        st.IsPasswordAutosaveEnabled = false;
        st.IsGeneralAutofillEnabled = false;
        // Otherwise Chromium paints its own error page until NavigationCompleted
        // fires and we can swap in ours.
        st.IsBuiltInErrorPageEnabled = false;

        c.NavigationStarting += delegate(object s, CoreWebView2NavigationStartingEventArgs args)
        {
            cancelledNavigation = false;
            if (args.Uri.StartsWith("data:") || args.Uri == "about:blank") return;
            if (Policy.IsAllowed(args.Uri)) return;
            args.Cancel = true;
            cancelledNavigation = true;
            Policy.OpenExternal(args.Uri);
        };

        c.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs args)
        {
            args.Handled = true;
            if (Policy.IsAllowed(args.Uri))
                c.Navigate(args.Uri);
            else
                Policy.OpenExternal(args.Uri);
        };

        c.PermissionRequested += delegate(object s, CoreWebView2PermissionRequestedEventArgs args)
        {
            args.State = CoreWebView2PermissionState.Deny;
        };

        c.ProcessFailed += delegate(object s, CoreWebView2ProcessFailedEventArgs args)
        {
            if (args.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
                RestartBrowserSoon();
            else
                c.Reload();
        };

        c.NavigationCompleted += delegate(object s, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (cancelledNavigation)
            {
                cancelledNavigation = false;
                return;
            }

            if (args.IsSuccess)
            {
                if (showingOffline && c.Source == "about:blank") return;
                showingOffline = false;
                State.SaveUrl(c.Source);
                State.Save(this);
                return;
            }

            if (IsOfflineStatus(args.WebErrorStatus)) ShowOffline(c);
        };

        c.Navigate(State.LastUrl ?? StartUrl);
    }

    void RestartBrowserSoon()
    {
        try
        {
            BeginInvoke((MethodInvoker)RestartBrowser);
        }
        catch (InvalidOperationException) { }
    }

    // The browser process owns the whole WebView2 tree, so the control has to be
    // thrown away and built again. Everything else (window, state) survives.
    async void RestartBrowser()
    {
        if (restarting || closing) return;
        restarting = true;

        try
        {
            core = null;
            showingOffline = false;
            cancelledNavigation = false;

            try { web.Dispose(); }
            catch (Exception) { }
            web = null;

            Exception last = null;
            for (int attempt = 0; attempt < 5 && !closing; attempt++)
            {
                await Task.Delay(800);
                if (closing) return;

                try
                {
                    CreateWebView();
                    await InitAsync();
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (web != null)
                    {
                        try { web.Dispose(); }
                        catch (Exception) { }
                        web = null;
                    }
                }
            }

            if (!closing && last != null)
            {
                MessageBox.Show(this,
                    "NextDNS-Dash could not restart its browser.\r\n\r\n" + last.Message,
                    "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            restarting = false;
        }
    }

    void ShowOffline(CoreWebView2 c)
    {
        if (showingOffline && c.Source == "about:blank") return;
        showingOffline = true;
        c.NavigateToString(OfflineHtml);
    }

    static bool IsOfflineStatus(CoreWebView2WebErrorStatus status)
    {
        switch (status)
        {
            case CoreWebView2WebErrorStatus.CannotConnect:
            case CoreWebView2WebErrorStatus.Timeout:
            case CoreWebView2WebErrorStatus.Disconnected:
            case CoreWebView2WebErrorStatus.ConnectionAborted:
            case CoreWebView2WebErrorStatus.ConnectionReset:
            case CoreWebView2WebErrorStatus.HostNameNotResolved:
            case CoreWebView2WebErrorStatus.ServerUnreachable:
                return true;
            default:
                return false;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        closing = true;
        State.Save(this);
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (networkHandler != null)
        {
            NetworkChange.NetworkAvailabilityChanged -= networkHandler;
            networkHandler = null;
        }
        base.OnFormClosed(e);
    }
}
