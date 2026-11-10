using System.Drawing;
using System.Windows.Forms;

sealed class ClearingForm : Form
{
    public ClearingForm()
    {
        Text = "Clearing private data";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Theme.Bg;
        ClientSize = new Size(320, 112);
        Padding = new Padding(24);

        var title = new Label();
        title.Text = "Clearing private data";
        title.ForeColor = Theme.Text;
        title.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        title.AutoSize = true;
        title.Location = new Point(24, 24);

        var note = new Label();
        note.Text = "Removing the browser profile before the app closes.";
        note.ForeColor = Theme.Muted;
        note.Font = new Font("Segoe UI", 9f);
        note.AutoSize = false;
        note.Size = new Size(272, 36);
        note.Location = new Point(24, 54);

        Controls.Add(title);
        Controls.Add(note);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (var pen = new Pen(Theme.Line))
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
