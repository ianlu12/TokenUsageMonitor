using System.Windows.Media;
using TokenUsageMonitor.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace TokenUsageMonitor.State;

/// <summary>
/// Overlay view-model wrapping a <see cref="TokenUsage"/> with the formatted
/// display properties (name, percent, detail) and the status-dot brush used by
/// the floating bar's per-meter cards.
/// </summary>
public sealed class UsageItem
{
    public UsageItem(TokenUsage usage)
    {
        Usage = usage;
    }

    public TokenUsage Usage { get; }

    public string Display => $"{Usage.ProviderName} · {Usage.MeterName}";

    public string Percent => Usage.Status == MeterStatus.Ok
        ? $"{Usage.UsedPercent:F0}%"
        : "-";

    public string Detail => FormatDetail(Usage);

    public MeterStatus Status => Usage.Status;

    public double UsedPercent => Usage.UsedPercent;

    public Brush StatusBrush => StatusBrushFor(Usage);

    /// <summary>
    /// Status color used for the per-meter dot: gray when nothing is selected,
    /// orange-red on error / not configured, orange at ≥80%, else lawn green.
    /// </summary>
    public static Brush StatusBrushFor(TokenUsage? usage)
    {
        if (usage is null)
        {
            return Brushes.Gray;
        }

        return usage.Status != MeterStatus.Ok
            ? Brushes.OrangeRed
            : usage.UsedPercent >= 80
                ? Brushes.Orange
                : Brushes.LawnGreen;
    }

    private static string FormatDetail(TokenUsage usage)
    {
        switch (usage.Status)
        {
            case MeterStatus.NotConfigured:
                return $"{usage.ProviderName} 尚未設定 API Key";
            case MeterStatus.Error:
                return $"取得失敗：{usage.ErrorMessage}（保留上次資料）";
            default:
            {
                string amount = FormatAmount(usage);
                string reset = usage.ResetAt is { } rt
                    ? $" · {rt.ToLocalTime():yyyy-MM-dd HH:mm} 重置"
                    : string.Empty;
                return $"{amount}{reset}";
            }
        }
    }

    private static string FormatAmount(TokenUsage usage)
    {
        if (usage.Unit == UsageUnit.Usd)
        {
            string used = FormatUsd(usage.Used);
            if (usage.IsUnlimited || usage.Total <= 0)
            {
                return $"使用量 {usage.UsedPercent:F0}%（{used}）";
            }

            return $"{FormatUsd(usage.Used)} / {FormatUsd(usage.Total)}（剩餘 {FormatUsd(usage.Remaining)}）";
        }

        if (usage.IsUnlimited)
        {
            return "無限額度";
        }

        return usage.Total <= 0
            ? $"使用量 {usage.UsedPercent:F0}%"
            : $"{usage.Used:#,##0} / {usage.Total:#,##0}（剩餘 {usage.Remaining:#,##0}）";
    }

    private static string FormatUsd(long cents)
        => $"US$ {(cents / 100.0):N2}";
}
