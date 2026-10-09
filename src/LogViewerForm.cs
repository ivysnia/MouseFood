using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MouseFood;

public sealed class LogViewerForm : Form
{
    private readonly InMemoryLogBuffer _buffer;
    private readonly TextBox _logTextBox;
    private readonly bool _isDarkMode;

    public LogViewerForm(InMemoryLogBuffer buffer)
    {
        _buffer = buffer;
        _isDarkMode = DarkModeColorTable.IsWindowsDarkMode();

        Text = "MouseFood - Log Viewer";
        Size = new Size(700, 450);
        MinimumSize = new Size(400, 250);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        Icon = null;

        if (_isDarkMode)
        {
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
        }

        // Log text box
        _logTextBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            MaxLength = 0,
            Font = new Font("Consolas", 9f),
            BackColor = _isDarkMode ? Color.FromArgb(20, 20, 20) : SystemColors.Window,
            ForeColor = _isDarkMode ? Color.FromArgb(220, 220, 220) : SystemColors.WindowText
        };

        // Button panel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40,
            Padding = new Padding(4),
            BackColor = _isDarkMode ? Color.FromArgb(30, 30, 30) : SystemColors.Control
        };

        var closeButton = CreateButton("Close");
        closeButton.Click += (s, e) => Hide();

        var saveButton = CreateButton("Save As...");
        saveButton.Click += OnSaveClick;

        var copyButton = CreateButton("Copy All");
        copyButton.Click += OnCopyClick;

        // Added right-to-left, so Close is rightmost
        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(copyButton);

        Controls.Add(_logTextBox);
        Controls.Add(buttonPanel);

        // Subscribe to live updates
        _buffer.LineAdded += OnLineAdded;
    }

    private Button CreateButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(8, 2, 8, 2)
        };

        if (_isDarkMode)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(50, 50, 50);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
        }

        return button;
    }

    private void OnLineAdded(string line)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        try
        {
            BeginInvoke(() =>
            {
                _logTextBox.AppendText(line + Environment.NewLine);
            });
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private void OnCopyClick(object? sender, EventArgs e)
    {
        if (_logTextBox.TextLength > 0)
            Clipboard.SetText(_logTextBox.Text);
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = $"MouseFood-log-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _logTextBox.Text);
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible)
        {
            // Populate with current buffer contents
            var lines = _buffer.GetLines();
            _logTextBox.Text = string.Join(Environment.NewLine, lines);
            if (_logTextBox.TextLength > 0)
                _logTextBox.AppendText(Environment.NewLine);

            // Scroll to bottom
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.ScrollToCaret();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _buffer.LineAdded -= OnLineAdded;
        }
        base.Dispose(disposing);
    }
}
