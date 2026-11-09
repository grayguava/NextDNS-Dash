using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

class MainForm : Form
{
    const string StartUrl = "https://my.nextdns.io";

    // Keep Chromium's own traffic out of a privacy tool: Edge config, SmartScreen
    // reputation, component updates, crash upload and sync have no part in
    // showing the dashboard.
    const string BrowserFlags =
        "--disable-background-networking " +
        "--disable-component-update " +
        "--disable-domain-reliability " +
        "--disable-sync " +
        "--disable-breakpad " +
        "--disable-client-side-phishing-detection " +
        "--no-pings";

    const int WM_SYSCOMMAND = 0x0112;
    const int WM_NCLBUTTONDOWN = 0x00A1;
    const int WM_NCLBUTTONDBLCLK = 0x00A3;
    const int WM_NCRBUTTONDOWN = 0x00A4;
    const int WM_NCRBUTTONUP = 0x00A5;
    const int WM_POPUPSYSTEMMENU = 0x0313;
    const int HTCAPTION = 2;
    const int HTSYSMENU = 3;
    const int SC_KEYMENU = 0xF100;
    const int SC_MOUSEMENU = 0xF090;

    WebView2 web;
    CoreWebView2 core;

    bool showingOffline;
    bool cancelledNavigation;
    bool restarting;
    bool closing;
    bool inPrivateActive;
    string lastNavigation;
    NetworkAvailabilityChangedEventHandler networkHandler;

    public MainForm()
    {
        Text = ""; // caption shows icon + buttons only
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Theme.Accent;

        Settings.Load();
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
        web.DefaultBackgroundColor = Theme.Accent;
        Controls.Add(web);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Backdrop.Apply(Handle);
    }

    protected override void WndProc(ref Message m)
    {
        // The window menu is disabled: the caption keeps only the minimize,
        // maximize and close buttons. Clicking the icon returns to Home.
        if (m.Msg == WM_NCLBUTTONDOWN && (int)m.WParam == HTSYSMENU)
        {
            Activate();
            GoHome();
            m.Result = IntPtr.Zero;
            return;
        }

        // Double-clicking the icon defaults to SC_CLOSE, which would quit the app.
        if (m.Msg == WM_NCLBUTTONDBLCLK && (int)m.WParam == HTSYSMENU)
        {
            Activate();
            GoHome();
            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WM_NCRBUTTONDOWN && (int)m.WParam == HTSYSMENU)
        {
            Activate();
            GoHome();
            m.Result = IntPtr.Zero;
            return;
        }

        if ((m.Msg == WM_NCRBUTTONDOWN || m.Msg == WM_NCRBUTTONUP) &&
            ((int)m.WParam == HTCAPTION || (int)m.WParam == HTSYSMENU))
        {
            Activate();
            m.Result = IntPtr.Zero;
            return;
        }

        // DefWindowProc answers a caption right-click with this message, which
        // opens the system menu directly without going through WM_SYSCOMMAND.
        if (m.Msg == WM_POPUPSYSTEMMENU)
        {
            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WM_SYSCOMMAND)
        {
            long command = m.WParam.ToInt64() & 0xFFF0;
            if (command == SC_KEYMENU || command == SC_MOUSEMENU)
            {
                m.Result = IntPtr.Zero;
                return;
            }
        }

        base.WndProc(ref m);
    }

    void GoHome()
    {
        if (core == null || closing) return;
        try { core.Navigate(WebUi.IsAvailable ? WebUi.HomeUrl : StartUrl); }
        catch (Exception) { }
    }

    async void OnLoad(object sender, EventArgs e)
    {
        try
        {
            await InitAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "NextDNS-Dash could not start.\r\n\r\n" + ex.Message,
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
        }
    }

    async Task InitAsync(string startUrl)
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

        if (Settings.PrivateMode)
        {
            bool cleared = await ClearProfile();
            if (closing) return;
            if (!cleared)
            {
                MessageBox.Show(this,
                    "NextDNS-Dash could not clear the saved browser profile.\r\n\r\n" +
                    "Close any other NextDNS-Dash or msedgewebview2 process, then try again.",
                    "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }
        }

        var options = new CoreWebView2EnvironmentOptions();
        options.AdditionalBrowserArguments = BrowserFlags;

        CoreWebView2Environment env =
            await CoreWebView2Environment.CreateAsync(null, profileDir, options);
        if (closing) return;

        inPrivateActive = false;
        CoreWebView2ControllerOptions controller = null;
        if (Settings.PrivateMode)
        {
            try
            {
                controller = env.CreateCoreWebView2ControllerOptions();
                controller.IsInPrivateModeEnabled = true;
                inPrivateActive = controller.IsInPrivateModeEnabled;
            }
            catch (Exception)
            {
                controller = null;
                inPrivateActive = false;
            }
        }

        if (controller != null)
            await web.EnsureCoreWebView2Async(env, controller);
        else
            await web.EnsureCoreWebView2Async(env);
        if (closing) return;

        CoreWebView2 c = web.CoreWebView2;
        core = c;
        WebUi.Map(c);

        if (inPrivateActive)
        {
            try { await c.AddScriptToExecuteOnDocumentCreatedAsync(PrivateBadge.Script); }
            catch (Exception) { }
        }

        CoreWebView2Settings st = c.Settings;
        st.AreDevToolsEnabled = false;
        st.IsStatusBarEnabled = false;
        st.AreDefaultContextMenusEnabled = false;
        st.IsPasswordAutosaveEnabled = false;
        st.IsGeneralAutofillEnabled = false;
        st.IsReputationCheckingRequired = false;
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

        c.WebMessageReceived += delegate(object s, CoreWebView2WebMessageReceivedEventArgs args)
        {
            if (!WebUi.IsLocalSource(args.Source)) return;

            string message;
            try { message = args.TryGetWebMessageAsString(); }
            catch (Exception) { return; }

            HandleMessage(c, message);
        };

        c.PermissionRequested += delegate(object s, CoreWebView2PermissionRequestedEventArgs args)
        {
            args.State = CoreWebView2PermissionState.Deny;
        };

        // Downloads are refused unless the Settings toggle allows them.
        c.DownloadStarting += delegate(object s, CoreWebView2DownloadStartingEventArgs args)
        {
            if (!Settings.DownloadsEnabled) args.Cancel = true;
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
                lastNavigation = c.Source;
                State.SaveUrl(c.Source);
                State.Save(this);
                return;
            }

            if (IsOfflineStatus(args.WebErrorStatus)) ShowOffline(c);
        };

        c.Navigate(startUrl ?? LaunchUrl());
    }

    static string LaunchUrl()
    {
        return WebUi.IsAvailable ? WebUi.HomeUrl : StartUrl;
    }

    void HandleMessage(CoreWebView2 c, string message)
    {
        if (message == "init")
        {
            SendState(c);
            return;
        }

        if (message == "open-data-folder")
        {
            OpenDataFolder();
            return;
        }

        if (message == "clear-data")
        {
            ClearData();
            return;
        }

        if (message != null && message.StartsWith("save-session:", StringComparison.Ordinal))
        {
            ApplySessionMode(message.Substring("save-session:".Length) == "on");
            return;
        }

        if (message != null && message.StartsWith("downloads:", StringComparison.Ordinal))
        {
            Settings.SetDownloads(message.Substring("downloads:".Length) == "on");
            SendState(c);
        }
    }

    static void SendState(CoreWebView2 c)
    {
        try { c.PostWebMessageAsJson(WebUi.StateJson()); }
        catch (Exception) { }
    }

    // Changing the session mode changes the WebView2 profile, so the browser
    // has to be rebuilt. Both directions start from a clean profile.
    async void ApplySessionMode(bool save)
    {
        if (restarting || closing) return;

        if (save == Settings.SaveSession)
        {
            if (core != null) SendState(core);
            return;
        }

        Settings.SetSaveSession(save);
        State.Load();
        restarting = true;

        try
        {
            core = null;
            showingOffline = false;
            cancelledNavigation = false;
            lastNavigation = null;

            try { web.Dispose(); }
            catch (Exception) { }
            web = null;

            await ClearProfile();
            if (closing) return;

            CreateWebView();
            await InitAsync(WebUi.IsAvailable ? WebUi.HomeUrl : null);
        }
        catch (Exception ex)
        {
            if (!closing)
            {
                MessageBox.Show(this,
                    "NextDNS-Dash could not switch session mode.\r\n\r\n" + ex.Message,
                    "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            restarting = false;
        }
    }

    static void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(Program.DataDirectory);
            Process.Start(new ProcessStartInfo(Program.DataDirectory) { UseShellExecute = true });
        }
        catch (Exception) { }
    }

    async void ClearData()
    {
        if (restarting || closing) return;
        restarting = true;

        bool removed;
        try { removed = await DeleteData(); }
        finally { restarting = false; }

        if (closing) return;

        if (!removed)
        {
            MessageBox.Show(this,
                "Some app data could not be removed. Close NextDNS-Dash and delete the data folder.",
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        try
        {
            CreateWebView();
            await InitAsync(WebUi.IsAvailable ? WebUi.HomeUrl : null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "NextDNS-Dash could not reopen its browser.\r\n\r\n" + ex.Message,
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    async Task<bool> ClearProfile()
    {
        string profileDir = Path.Combine(Program.DataDirectory, "profile");
        for (int attempt = 0; attempt < 25 && !closing; attempt++)
        {
            try
            {
                if (Directory.Exists(profileDir))
                    Directory.Delete(profileDir, true);
                return true;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            await Task.Delay(400);
        }
        return !Directory.Exists(profileDir);
    }

    // Drops the browser and removes data\. Returns true only when the folder is
    // gone, so callers can refuse to show anything that survived.
    async Task<bool> DeleteData()
    {
        core = null;
        showingOffline = false;
        cancelledNavigation = false;
        lastNavigation = null;

        try { web.Dispose(); }
        catch (Exception) { }
        web = null;

        bool removed = false;
        for (int attempt = 0; attempt < 25 && !closing; attempt++)
        {
            try
            {
                if (Directory.Exists(Program.DataDirectory))
                    Directory.Delete(Program.DataDirectory, true);
                removed = true;
                break;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            await Task.Delay(400);
        }

        if (removed) Settings.Save();
        State.Load();
        return removed;
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
                    await InitAsync(lastNavigation ?? LaunchUrl());
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
        c.NavigateToString(BuildOfflineHtml());
    }

    static string BuildOfflineHtml()
    {
        string back = WebUi.IsAvailable
            ? "<a class=\"secondary\" href=\"" + WebUi.HomeUrl + "\">Back to home</a>"
            : "";

        return "<!doctype html><html><head><meta charset=\"utf-8\"><style>" +
            "html,body{height:100%;margin:0}" +
            "body{background:#212529;color:#e9ecef;font:15px/1.5 'Segoe UI',sans-serif;" +
            "display:flex;align-items:center;justify-content:center}" +
            ".box{text-align:center;max-width:420px;padding:24px}" +
            "h1{font-size:18px;font-weight:600;margin:0 0 8px}" +
            "p{margin:0 0 20px;color:#adb5bd}" +
            "a{display:inline-block;margin:0 4px;padding:8px 18px;border-radius:6px;" +
            "text-decoration:none}" +
            "a.primary{background:#00a6ff;color:#07131c}a.primary:hover{background:#33b8ff}" +
            "a.secondary{background:#2c3136;color:#e9ecef}a.secondary:hover{background:#3a4047}" +
            "</style></head><body><div class=\"box\">" +
            "<h1>Can't reach the dashboard</h1>" +
            "<p>The app retries automatically when the connection comes back.</p>" +
            "<a class=\"primary\" href=\"https://my.nextdns.io\">Try again</a>" +
            back +
            "</div></body></html>";
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

        if (Settings.PrivateMode)
        {
            try { if (web != null) web.Dispose(); }
            catch (Exception) { }
            web = null;
            DeleteProfileBestEffort();
        }

        base.OnFormClosing(e);
    }

    static void DeleteProfileBestEffort()
    {
        string profileDir = Path.Combine(Program.DataDirectory, "profile");
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                if (Directory.Exists(profileDir))
                    Directory.Delete(profileDir, true);
                return;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            System.Threading.Thread.Sleep(100);
        }
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
