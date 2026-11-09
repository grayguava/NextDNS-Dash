using System;
using System.Diagnostics;
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

    Panel lockPanel;
    Label lockStatus;
    Button unlockButton;
    Button setupButton;
    Button bypassButton;

    bool showingOffline;
    bool cancelledNavigation;
    bool restarting;
    bool closing;
    bool locked;
    bool unlockBusy;
    string pendingUrl;
    string lastNavigation;
    NetworkAvailabilityChangedEventHandler networkHandler;

    public MainForm()
    {
        Text = ""; // caption shows icon + buttons only
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Theme.Accent;

        State.Load();
        Settings.Load();
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
        CreateLockPanel();
        Load += OnLoad;
        Shown += delegate { if (locked) BeginUnlock(); };

        if (Settings.LockOnLaunch)
        {
            locked = true;
            web.Visible = false;
            lockPanel.Visible = true;
        }

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
        web.Visible = !locked;
        Controls.Add(web);
    }

    void CreateLockPanel()
    {
        lockPanel = new Panel();
        lockPanel.Dock = DockStyle.Fill;
        lockPanel.BackColor = Theme.Accent;
        lockPanel.Visible = false;

        var title = new Label();
        title.Text = "NextDNS-Dash is locked";
        title.ForeColor = Theme.Text;
        title.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        title.AutoSize = true;
        title.Margin = new Padding(0, 0, 0, 6);

        lockStatus = new Label();
        lockStatus.Text = "Unlock with Windows Hello to continue.";
        lockStatus.ForeColor = Color.FromArgb(0xAD, 0xB5, 0xBD);
        lockStatus.Font = new Font("Segoe UI", 9F);
        lockStatus.AutoSize = true;
        lockStatus.MaximumSize = new Size(440, 0);
        lockStatus.TextAlign = ContentAlignment.MiddleCenter;
        lockStatus.Margin = new Padding(0, 0, 0, 12);

        unlockButton = MakeButton("Unlock");
        unlockButton.Click += delegate { BeginUnlock(); };

        setupButton = MakeButton("Open sign-in settings");
        setupButton.Visible = false;
        setupButton.Click += delegate { OpenSignInSettings(); };

        bypassButton = MakeButton("Open without lock");
        bypassButton.Visible = false;
        bypassButton.Click += delegate
        {
            Settings.SetLock(false);
            Unlock();
        };

        var buttons = new FlowLayoutPanel();
        buttons.AutoSize = true;
        buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttons.FlowDirection = FlowDirection.LeftToRight;
        buttons.WrapContents = false;
        buttons.Anchor = AnchorStyles.None;
        buttons.BackColor = Theme.Accent;
        buttons.Controls.Add(unlockButton);
        buttons.Controls.Add(setupButton);
        buttons.Controls.Add(bypassButton);

        var inner = new FlowLayoutPanel();
        inner.AutoSize = true;
        inner.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        inner.FlowDirection = FlowDirection.TopDown;
        inner.WrapContents = false;
        inner.Anchor = AnchorStyles.None;
        inner.BackColor = Theme.Accent;
        inner.Controls.Add(title);
        inner.Controls.Add(lockStatus);
        inner.Controls.Add(buttons);

        var host = new TableLayoutPanel();
        host.Dock = DockStyle.Fill;
        host.BackColor = Theme.Accent;
        host.ColumnCount = 1;
        host.RowCount = 1;
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.Controls.Add(inner, 0, 0);

        lockPanel.Controls.Add(host);
        Controls.Add(lockPanel);
        lockPanel.BringToFront();
    }

    static Button MakeButton(string text)
    {
        var button = new Button();
        button.Text = text;
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Padding = new Padding(14, 5, 14, 5);
        button.Margin = new Padding(0, 0, 8, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x3A, 0x40, 0x47);
        button.BackColor = Theme.Hover;
        button.ForeColor = Theme.Text;
        button.Font = new Font("Segoe UI", 9F);
        button.Cursor = Cursors.Hand;
        return button;
    }

    async void BeginUnlock()
    {
        if (unlockBusy || !locked) return;

        unlockBusy = true;
        unlockButton.Enabled = false;
        setupButton.Visible = false;
        bypassButton.Visible = false;
        lockStatus.Text = "Waiting for Windows Hello...";
        WindowsHello.Recheck();

        bool verified = await Task.Factory.StartNew<bool>(delegate
        {
            if (!WindowsHello.IsAvailable) return false;
            return WindowsHello.Verify("Unlock NextDNS-Dash");
        });

        unlockBusy = false;
        unlockButton.Enabled = true;

        if (closing) return;

        if (verified)
        {
            Unlock();
            return;
        }

        if (!WindowsHello.IsAvailable)
        {
            lockStatus.Text = HelloReason(WindowsHello.Availability) +
                " Set it up in Windows Settings, or open without the lock.";
            unlockButton.Text = "Try again";
            unlockButton.Visible = true;
            unlockButton.Enabled = true;
            setupButton.Visible = true;
            bypassButton.Visible = true;
            setupButton.Focus();
            return;
        }

        lockStatus.Text = "Windows Hello did not verify. Try again.";
        unlockButton.Focus();
    }

    void Unlock()
    {
        if (closing) return;

        locked = false;
        lockPanel.Visible = false;
        web.Visible = true;

        if (core != null)
        {
            string url = pendingUrl ?? LaunchUrl();
            pendingUrl = null;
            try { core.Navigate(url); }
            catch (Exception) { }
        }

        web.Focus();
    }

    static string HelloReason(string status)
    {
        switch (status)
        {
            case "NotConfiguredForUser": return "Windows Hello is not set up for this account.";
            case "DeviceNotPresent": return "No Windows Hello device is available.";
            case "DisabledByPolicy": return "Windows Hello is disabled by policy.";
            case "DeviceBusy": return "Windows Hello is busy. Try again.";
            default: return "Windows Hello is not available.";
        }
    }

    static void OpenSignInSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:signinoptions") { UseShellExecute = true });
        }
        catch (Exception) { }
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
        if (locked || core == null || closing) return;
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

        CoreWebView2Environment env =
            await CoreWebView2Environment.CreateAsync(null, profileDir);
        if (closing) return;
        await web.EnsureCoreWebView2Async(env);
        if (closing) return;

        CoreWebView2 c = web.CoreWebView2;
        core = c;
        WebUi.Map(c);

        CoreWebView2Settings st = c.Settings;
        st.AreDevToolsEnabled = false;
        st.IsStatusBarEnabled = false;
        st.AreDefaultContextMenusEnabled = false;
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

        if (locked)
            pendingUrl = startUrl ?? LaunchUrl();
        else
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

        if (message != null && message.StartsWith("lock:", StringComparison.Ordinal))
        {
            bool enabled = message.Substring("lock:".Length) == "on";
            if (enabled && !WindowsHello.IsAvailable)
            {
                SendState(c, "Windows Hello is not set up on this PC.");
                return;
            }
            Settings.SetLock(enabled);
            SendState(c);
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
        SendState(c, null);
    }

    static void SendState(CoreWebView2 c, string lockError)
    {
        try { c.PostWebMessageAsJson(WebUi.StateJson(lockError)); }
        catch (Exception) { }
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

        bool removed = false;
        Exception failure = null;

        try
        {
            core = null;
            showingOffline = false;
            cancelledNavigation = false;
            lastNavigation = null;

            try { web.Dispose(); }
            catch (Exception) { }
            web = null;

            for (int attempt = 0; attempt < 10 && !closing; attempt++)
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
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            restarting = false;
        }

        if (closing) return;

        if (failure != null)
        {
            MessageBox.Show(this,
                "NextDNS-Dash could not clear its data.\r\n\r\n" + failure.Message,
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        State.Load();

        try
        {
            CreateWebView();
            await InitAsync(failure == null && WebUi.IsAvailable
                ? WebUi.HomeUrl
                : null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "NextDNS-Dash could not reopen its browser.\r\n\r\n" + ex.Message,
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!removed && failure == null)
        {
            MessageBox.Show(this,
                "Some app data could not be removed. Close NextDNS-Dash and delete the data folder.",
                "NextDNS-Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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
