using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

class MainForm : Form
{
    const string StartUrl = "https://my.nextdns.io";

    readonly WebView2 web = new WebView2();

    public MainForm()
    {
        Text = ""; // caption shows icon + buttons only
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Width = 1035;
        Height = 665;
        StartPosition = FormStartPosition.CenterScreen;
        web.Dock = DockStyle.Fill;
        Controls.Add(web);
        Load += OnLoad;
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
        await web.EnsureCoreWebView2Async(env);

        CoreWebView2 core = web.CoreWebView2;

        CoreWebView2Settings st = core.Settings;
        st.AreDevToolsEnabled = false;
        st.IsStatusBarEnabled = false;
        st.IsPasswordAutosaveEnabled = false;
        st.IsGeneralAutofillEnabled = false;

        core.NavigationStarting += delegate(object s, CoreWebView2NavigationStartingEventArgs args)
        {
            if (Policy.IsAllowed(args.Uri)) return;
            args.Cancel = true;
            Policy.OpenExternal(args.Uri);
        };

        core.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs args)
        {
            args.Handled = true;
            if (Policy.IsAllowed(args.Uri))
                core.Navigate(args.Uri);
            else
                Policy.OpenExternal(args.Uri);
        };

        core.PermissionRequested += delegate(object s, CoreWebView2PermissionRequestedEventArgs args)
        {
            args.State = CoreWebView2PermissionState.Deny;
        };

        core.ProcessFailed += delegate(object s, CoreWebView2ProcessFailedEventArgs args)
        {
            if (args.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
                Close();
            else
                core.Reload();
        };

        core.Navigate(StartUrl);
    }
}
