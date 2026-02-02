using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MouseFood;

public interface ITrayIconManager : IDisposable
{
    event Action? RefreshRequested;
    event Action? ExitRequested;
    void UpdateStatus(BatteryStatus? status);
}

public class TrayIconManager : ITrayIconManager
{
    private readonly ILogger<TrayIconManager> _logger;
    private readonly InMemoryLogBuffer _logBuffer;
    private readonly NotifyIcon _notifyIcon;
    private BatteryStatus? _lastStatus;
    private bool _lowBatteryNotificationShown = false;
    private LogViewerForm? _logViewerForm;

    public TrayIconManager(ILogger<TrayIconManager> logger, InMemoryLogBuffer logBuffer)
    {
        _logger = logger;
        _logBuffer = logBuffer;

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "MouseFood - Initializing..."
        };

        // Create context menu with system style
        var contextMenu = new ContextMenuStrip
        {
            // Use custom renderer that respects Windows dark mode
            Renderer = new DarkModeRenderer(),
            // Remove default padding for cleaner look
            Padding = new Padding(0)
        };

        // Add rounded corners for Windows 11 style
        contextMenu.Opening += (s, e) =>
        {
            var menu = s as ContextMenuStrip;
            if (menu != null)
            {
                // Apply rounded region (8px radius for Windows 11 style)
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                var rect = new Rectangle(0, 0, menu.Width, menu.Height);
                int radius = 8;
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                menu.Region = new Region(path);
            }
        };

        var refreshItem = new ToolStripMenuItem("Refresh Now");
        refreshItem.Click += (s, e) => RefreshRequested?.Invoke();
        contextMenu.Items.Add(refreshItem);

        var showLogItem = new ToolStripMenuItem("Show Log");
        showLogItem.Click += (s, e) =>
        {
            _logViewerForm ??= new LogViewerForm(_logBuffer);
            _logViewerForm.Show();
            _logViewerForm.BringToFront();
        };
        contextMenu.Items.Add(showLogItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var startupItem = new ToolStripMenuItem("Start with Windows");
        startupItem.Checked = IsStartupEnabled();
        startupItem.Click += (s, e) =>
        {
            var item = (ToolStripMenuItem)s!;
            if (item.Checked)
            {
                DisableStartup();
                item.Checked = false;
            }
            else
            {
                EnableStartup();
                item.Checked = true;
            }
        };
        contextMenu.Items.Add(startupItem);

        var taskbarItem = new ToolStripMenuItem("Taskbar Settings...");
        taskbarItem.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:taskbar")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open Taskbar Settings");
            }
        };
        contextMenu.Items.Add(taskbarItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitRequested?.Invoke();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Set initial icon
        UpdateIcon(null);
    }

    public event Action? RefreshRequested;
    public event Action? ExitRequested;

    public void UpdateStatus(BatteryStatus? status)
    {
        _lastStatus = status;
        UpdateIcon(status);
        UpdateTooltip(status);
        CheckLowBattery(status);
    }

    private void CheckLowBattery(BatteryStatus? status)
    {
        if (status == null)
        {
            return;
        }

        // Show notification when battery drops to 20% or below (and not charging)
        if (status.Percentage <= 20 && !status.IsCharging)
        {
            if (!_lowBatteryNotificationShown)
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Low Battery Warning",
                    $"Mouse battery is at {status.Percentage}%. Please charge soon.",
                    ToolTipIcon.Warning);

                _lowBatteryNotificationShown = true;
                _logger.LogInformation("Low battery notification shown at {Percentage}%", status.Percentage);
            }
        }
        else if (status.Percentage > 25)
        {
            // Reset the notification flag when battery goes above 25%
            // This prevents repeated notifications if battery hovers around 20%
            _lowBatteryNotificationShown = false;
        }
    }

    private void UpdateIcon(BatteryStatus? status)
    {
        try
        {
            // Dispose old icon
            var oldIcon = _notifyIcon.Icon;

            // Create new icon with battery percentage
            _notifyIcon.Icon = CreateBatteryIcon(status);

            oldIcon?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tray icon");
        }
    }

    private void UpdateTooltip(BatteryStatus? status)
    {
        if (status == null)
        {
            _notifyIcon.Text = "MouseFood - No data";
        }
        else
        {
            var chargingText = status.IsCharging ? " (Charging)" : "";
            _notifyIcon.Text = $"Razer Mouse: {status.Percentage}%{chargingText}\nUpdated: {status.LastUpdated:HH:mm}";
        }
    }

    private Icon CreateBatteryIcon(BatteryStatus? status)
    {
        // Use the system's preferred icon size (scales with DPI)
        var iconSize = SystemInformation.SmallIconSize;
        var w = iconSize.Width;
        var h = iconSize.Height;
        var bitmap = new Bitmap(w, h);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            if (status == null)
            {
                using (var font = new Font("Arial", h * 0.6f, FontStyle.Bold))
                {
                    var size = g.MeasureString("?", font);
                    g.DrawString("?", font, Brushes.White,
                               (w - size.Width) / 2, (h - size.Height) / 2);
                }
            }
            else
            {
                // Horizontal battery flush to top, scaled to icon size
                var batteryWidth = (int)(w * 0.75f);
                var batteryHeight = (int)(h * 0.3f);
                var x = (w - batteryWidth - 2) / 2; // Center with room for nub
                var y = 0;

                // Fill color
                var fillColor = status.IsCharging ? Color.LightGreen :
                               status.Percentage > 50 ? Color.LimeGreen :
                               status.Percentage > 20 ? Color.Orange :
                               Color.Red;

                // Battery outline in a brighter shade of the fill color
                var borderColor = ControlPaint.Light(fillColor, 0.5f);
                using (var pen = new Pen(borderColor, 1))
                {
                    g.DrawRectangle(pen, x, y, batteryWidth, batteryHeight);
                    // Battery terminal (right nub)
                    using (var nubBrush = new SolidBrush(borderColor))
                        g.FillRectangle(nubBrush, x + batteryWidth + 1, y + 1, 1, batteryHeight - 1);
                }

                var fillWidth = (int)(batteryWidth * status.Percentage / 100.0);
                if (fillWidth > 0)
                {
                    using (var brush = new SolidBrush(fillColor))
                    {
                        g.FillRectangle(brush, x + 1, y + 1,
                                      Math.Min(fillWidth, batteryWidth - 1), batteryHeight - 1);
                    }
                }

                // Percentage text below the battery
                var text = status.Percentage.ToString();
                var fontSize = h * 0.4f;
                using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold))
                using (var brush = new SolidBrush(fillColor))
                {
                    var size = g.MeasureString(text, font);
                    g.DrawString(text, font, brush,
                               (w - size.Width) / 2, batteryHeight + 1);
                }
            }
        }

        var icon = Icon.FromHandle(bitmap.GetHicon());
        return icon;
    }

    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "MouseFood";

    private static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey);
            return key?.GetValue(StartupValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    private void EnableStartup()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath == null) return;

            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            key?.SetValue(StartupValueName, $"\"{exePath}\"");
            _logger.LogInformation("Startup entry added: {Path}", exePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable startup");
        }
    }

    private void DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            key?.DeleteValue(StartupValueName, throwOnMissingValue: false);
            _logger.LogInformation("Startup entry removed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable startup");
        }
    }

    public void Dispose()
    {
        _logViewerForm?.Dispose();
        _notifyIcon?.Icon?.Dispose();
        _notifyIcon?.Dispose();
    }
}

// Custom renderer that respects Windows dark mode
internal class DarkModeRenderer : ToolStripProfessionalRenderer
{
    private readonly bool _isDarkMode;

    public DarkModeRenderer() : base(new DarkModeColorTable())
    {
        _isDarkMode = DarkModeColorTable.IsWindowsDarkMode();
        RoundedEdges = false; // Disable default rounded edges
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (_isDarkMode)
        {
            e.TextColor = Color.White;
        }
        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (_isDarkMode && e.ToolStrip is ContextMenuStrip)
        {
            // Draw rounded border for Windows 11 style (much darker border)
            using var pen = new Pen(Color.FromArgb(20, 20, 20), 1);
            using var path = CreateRoundedRectanglePath(e.AffectedBounds, 8);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        }
        else
        {
            base.OnRenderToolStripBorder(e);
        }
    }

    private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        // Top left arc
        path.AddArc(arc, 180, 90);

        // Top right arc
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom right arc
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom left arc
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}

// Custom color table that respects Windows dark mode
internal class DarkModeColorTable : ProfessionalColorTable
{
    private readonly bool _isDarkMode;

    public DarkModeColorTable()
    {
        _isDarkMode = IsWindowsDarkMode();
    }

    internal static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    // Windows 11 dark theme colors (darker and closer to native menus)
    public override Color MenuItemSelected => _isDarkMode ? Color.FromArgb(40, 40, 40) : base.MenuItemSelected;
    public override Color MenuItemBorder => _isDarkMode ? Color.FromArgb(20, 20, 20) : base.MenuItemBorder;
    public override Color MenuItemSelectedGradientBegin => _isDarkMode ? Color.FromArgb(40, 40, 40) : base.MenuItemSelectedGradientBegin;
    public override Color MenuItemSelectedGradientEnd => _isDarkMode ? Color.FromArgb(40, 40, 40) : base.MenuItemSelectedGradientEnd;
    public override Color MenuItemPressedGradientBegin => _isDarkMode ? Color.FromArgb(35, 35, 35) : base.MenuItemPressedGradientBegin;
    public override Color MenuItemPressedGradientEnd => _isDarkMode ? Color.FromArgb(35, 35, 35) : base.MenuItemPressedGradientEnd;
    public override Color ImageMarginGradientBegin => _isDarkMode ? Color.FromArgb(30, 30, 30) : base.ImageMarginGradientBegin;
    public override Color ImageMarginGradientMiddle => _isDarkMode ? Color.FromArgb(30, 30, 30) : base.ImageMarginGradientMiddle;
    public override Color ImageMarginGradientEnd => _isDarkMode ? Color.FromArgb(30, 30, 30) : base.ImageMarginGradientEnd;
    public override Color ToolStripDropDownBackground => _isDarkMode ? Color.FromArgb(30, 30, 30) : base.ToolStripDropDownBackground;
    public override Color SeparatorDark => _isDarkMode ? Color.FromArgb(45, 45, 45) : base.SeparatorDark;
    public override Color SeparatorLight => _isDarkMode ? Color.FromArgb(35, 35, 35) : base.SeparatorLight;
}
