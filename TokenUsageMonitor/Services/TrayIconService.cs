using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TokenUsageMonitor.Models;
using TokenUsageMonitor.State;

namespace TokenUsageMonitor.Services;

/// <summary>
/// System tray integration: self-drawn icon showing the selected meter's
/// usage percentage, a right-click menu with full usage details for both
/// providers plus action items, and a tooltip.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly AppState _state;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _menu;

    public TrayIconService(AppState state)
    {
        _state = state;
    }

    // Wired up by the composition root (App).
    public Func<bool>? StartupEnabled { get; set; }
    public Action? OpenSettings { get; set; }
    public Action? ToggleOverlay { get; set; }
    public Action? ToggleAutoStart { get; set; }
    public Action? ManualRefresh { get; set; }
    public Action? PersistSettings { get; set; }
    public Action? ExitApp { get; set; }

    public void Build()
    {
        _menu = new ContextMenuStrip();
        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Text = "Token Usage Monitor",
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => OpenSettings?.Invoke();
        _state.TrayDisplayChanged += OnTrayDisplayChanged;

        Update();
    }

    private void OnTrayDisplayChanged()
    {
        UpdateIcon();
        UpdateTooltip();
    }

    /// <summary>Rebuild the menu and redraw the icon from current state.</summary>
    public void Update()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        RebuildMenu(_menu ??= new ContextMenuStrip());
        UpdateIcon();
        UpdateTooltip();
    }

    /// <summary>Redraw icon + tooltip only (avoids rebuilding an open menu on polls).</summary>
    public void RefreshData()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        UpdateIcon();
        UpdateTooltip();
    }

    // ---------------------------------------------------------------- icon

    private void UpdateIcon()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        TokenUsage? usage = _state.TrayDisplay;

        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Match the app icon's dark circular gauge style: dark disc
            // background with a status-colored percentage glyph.
            using (var bgBrush = new SolidBrush(Color.FromArgb(43, 43, 43)))
            {
                g.FillEllipse(bgBrush, 1, 1, 14, 14);
            }

            Color fg;
            string text;
            if (usage is null)
            {
                text = "?";
                fg = Color.FromArgb(176, 176, 176);
            }
            else if (usage.Status == MeterStatus.NotConfigured)
            {
                text = "?";
                fg = Color.FromArgb(158, 158, 158);
            }
            else if (usage.Status == MeterStatus.Error)
            {
                text = "!";
                fg = Color.FromArgb(224, 72, 76);
            }
            else if (usage.UsedPercent >= 100)
            {
                text = "!!";
                fg = Color.FromArgb(224, 72, 76);
            }
            else
            {
                text = ((int)Math.Round(usage.UsedPercent)).ToString();
                fg = usage.UsedPercent >= 80
                    ? Color.FromArgb(230, 126, 34)
                    : Color.FromArgb(52, 208, 88);
            }

            using var font = new Font("Segoe UI", 7f, FontStyle.Bold, GraphicsUnit.Pixel);
            SizeF sz = g.MeasureString(text, font);
            using var textBrush = new SolidBrush(fg);
            g.DrawString(text, font, textBrush, (16 - sz.Width) / 2f, (16 - sz.Height) / 2f);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            Icon newIcon = (Icon)Icon.FromHandle(hIcon).Clone();
            Icon? old = _notifyIcon.Icon;
            _notifyIcon.Icon = newIcon;
            old?.Dispose();
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
            {
                DestroyIcon(hIcon);
            }
        }
    }

    private void UpdateTooltip()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        TokenUsage? s = _state.TrayDisplay;
        string text = s is null
            ? "Token Usage Monitor — 未設定 API Key"
            : $"{s.ProviderName} · {s.MeterName}: {StatusText(s)}";

        // NotifyIcon.Text is limited to 63 chars.
        _notifyIcon.Text = text.Length <= 63 ? text : text[..60] + "...";
    }

    // ---------------------------------------------------------------- menu

    private void RebuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();

        int infoCount = 0;
        foreach (var group in _state.AllUsages.GroupBy(u => u.ProviderName))
        {
            if (infoCount > 0)
            {
                menu.Items.Add(new ToolStripSeparator());
            }

            string? url = _state.AllUsages
                .FirstOrDefault(u => u.ProviderName == group.Key)?
                .UsageUrl;
            var header = new ToolStripMenuItem($"▸ {group.Key}", null, (_, _) => OpenUsagePage(url))
            {
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
            };
            menu.Items.Add(header);
            foreach (TokenUsage usage in group)
            {
                string line = $"  {usage.MeterName}: {StatusText(usage)}";
                menu.Items.Add(new ToolStripMenuItem(line) { Enabled = false });
            }

            infoCount++;
        }

        if (infoCount == 0)
        {
            menu.Items.Add(new ToolStripMenuItem("尚未取得任何使用量資料") { Enabled = false });
        }

        menu.Items.Add(new ToolStripSeparator());

        var trayDisplayMenu = new ToolStripMenuItem("托盤顯示項目");
        foreach (var meter in UsageCatalog.All)
        {
            var item = new ToolStripMenuItem($"{meter.ProviderName} · {meter.MeterName}")
            {
                CheckOnClick = true,
                Checked = meter.Key == _state.Settings.TrayMeter,
                Tag = meter.Key,
            };
            item.Click += (sender, _) =>
            {
                if (sender is ToolStripMenuItem mi && mi.Tag is string key)
                {
                    OnTrayDisplayClicked(key);
                }
            };
            trayDisplayMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(trayDisplayMenu);

        var settings = new ToolStripMenuItem("設定 API Key…", null, (_, _) => OpenSettings?.Invoke());
        menu.Items.Add(settings);

        var overlay = new ToolStripMenuItem("顯示懸浮窗")
        {
            CheckOnClick = false,
            Checked = _state.Settings.OverlayVisible,
        };
        overlay.Click += (_, _) => ToggleOverlay?.Invoke();
        menu.Items.Add(overlay);

        var autoStart = new ToolStripMenuItem("開機自動啟動")
        {
            Checked = StartupEnabled?.Invoke() ?? false,
        };
        autoStart.Click += (_, _) => ToggleAutoStart?.Invoke();
        menu.Items.Add(autoStart);

        var refresh = new ToolStripMenuItem("手動重新整理", null, (_, _) => ManualRefresh?.Invoke());
        menu.Items.Add(refresh);

        menu.Items.Add(new ToolStripSeparator());

        var exit = new ToolStripMenuItem("退出", null, (_, _) => ExitApp?.Invoke());
        menu.Items.Add(exit);
    }

    private void OnTrayDisplayClicked(string key)
    {
        if (_state.Settings.TrayMeter == key)
        {
            return;
        }

        _state.SetTrayMeter(key);
        PersistSettings?.Invoke();
        Update();
    }

    private static string StatusText(TokenUsage u)
    {
        return u.Status switch
        {
            MeterStatus.NotConfigured => "未設定 API Key",
            MeterStatus.Error => $"取得失敗：{u.ErrorMessage}（保留上次資料）",
            _ => FormatStatus(u) + ResetSuffix(u.ResetAt),
        };
    }

    private static string FormatStatus(TokenUsage u)
    {
        if (u.Unit == UsageUnit.Usd)
        {
            string used = FormatUsd(u.Used);
            if (u.IsUnlimited || u.Total <= 0)
            {
                return $"已使用 {u.UsedPercent:F0}%（{used}）";
            }

            return $"{FormatUsd(u.Used)} / {FormatUsd(u.Total)}（{u.UsedPercent:F0}%）";
        }

        return u.IsUnlimited
            ? $"已使用 {u.UsedPercent:F0}%（無限額度）"
            : u.Total > 0
                ? $"{u.Used:N0} / {u.Total:N0}（{u.UsedPercent:F0}%）"
                : $"已使用 {u.Used:N0}（{u.UsedPercent:F0}%）";
    }

    private static string FormatUsd(long cents)
        => $"US$ {(cents / 100.0):N2}";

    private static void OpenUsagePage(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Best effort; ignore any failure to open the browser.
        }
    }

    private static string ResetSuffix(DateTime? resetAt)
    {
        return resetAt is { } rt ? $" · {rt.ToLocalTime():yyyy-MM-dd HH:mm} 重置" : string.Empty;
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public void Dispose()
    {
        _state.TrayDisplayChanged -= OnTrayDisplayChanged;

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _menu?.Dispose();
        _menu = null;
        GC.SuppressFinalize(this);
    }
}
