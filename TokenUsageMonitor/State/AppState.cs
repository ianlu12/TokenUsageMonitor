using System.Collections.ObjectModel;
using TokenUsageMonitor.Common;
using TokenUsageMonitor.Models;

namespace TokenUsageMonitor.State;

/// <summary>
/// Central application state. Holds the last fetched usages, resolves the
/// currently-selected meter for the tray icon / overlay, and notifies UI
/// (overlay bindings, tray rebuild) whenever data changes.
/// </summary>
public sealed class AppState : ObservableObject
{
    private TokenUsage? _selected;
    private TokenUsage? _trayDisplay;
    private TokenUsage? _worstStatus;
    private double _opacity = 1.0;

    public AppState(AppSettings settings)
    {
        Settings = settings;
        _opacity = settings.OverlayOpacity;
    }

    public AppSettings Settings { get; }

    public ObservableCollection<TokenUsage> AllUsages { get; } = new();

    /// <summary>
    /// Meters currently shown on the floating bar (filtered by
    /// <see cref="AppSettings.VisibleMeters"/>), wrapped for display binding.
    /// </summary>
    public ObservableCollection<UsageItem> VisibleUsages { get; } = new();

    public TokenUsage? Selected
    {
        get => _selected;
        private set => Set(ref _selected, value);
    }

    /// <summary>The meter presented on the tray icon (per TrayMeter setting).</summary>
    public TokenUsage? TrayDisplay
    {
        get => _trayDisplay;
        private set => Set(ref _trayDisplay, value);
    }

    /// <summary>The "worst" visible meter (error &gt; not-configured &gt; high usage)
    /// used to color the overlay's top-level status dot.</summary>
    public TokenUsage? WorstStatus
    {
        get => _worstStatus;
        private set => Set(ref _worstStatus, value);
    }

    /// <summary>Overlay background opacity (mirrors <see cref="AppSettings.OverlayOpacity"/>).</summary>
    public double Opacity
    {
        get => _opacity;
        set => Set(ref _opacity, value);
    }

    /// <summary>Raised with the newly selected meter whenever usage data changes.</summary>
    public event Action<TokenUsage?>? UsagesChanged;

    /// <summary>Raised when <see cref="VisibleUsages"/> content changes.</summary>
    public event Action? VisibilityChanged;

    /// <summary>Raised when <see cref="TrayDisplay"/> changes (tray icon must redraw).</summary>
    public event Action? TrayDisplayChanged;

    /// <summary>Apply a fresh set of usages (from a poll cycle).</summary>
    public void ApplyUsages(IEnumerable<TokenUsage> usages)
    {
        var list = usages.ToList();
        AllUsages.Clear();
        foreach (var u in list)
        {
            AllUsages.Add(u);
        }

        Selected = ResolveMeter(Settings.SelectedMeter);
        RefreshVisibility();
        RefreshTrayDisplay();

        UsagesChanged?.Invoke(Selected);
    }

    /// <summary>
    /// Re-filter <see cref="VisibleUsages"/> from the current VisibleMeters
    /// setting. Call this after the overlay's context menu toggles a meter.
    /// </summary>
    public void RefreshVisibility()
    {
        var keys = EffectiveVisibleKeys();
        var items = AllUsages
            .Where(u => keys.Contains(u.Key))
            .Select(u => new UsageItem(u))
            .ToList();

        VisibleUsages.Clear();
        foreach (var item in items)
        {
            VisibleUsages.Add(item);
        }

        ComputeWorst();
        VisibilityChanged?.Invoke();
    }

    /// <summary>
    /// Mutate the visible-meters list (materializing it from the effective
    /// default on first use) and refresh. The updated list is written to
    /// <see cref="AppSettings.VisibleMeters"/>.
    /// </summary>
    public void UpdateVisibleMeters(Action<List<string>> mutate)
    {
        var keys = Settings.VisibleMeters is null
            ? EffectiveVisibleKeys().ToList()
            : new List<string>(Settings.VisibleMeters);
        mutate(keys);
        Settings.VisibleMeters = keys;
        RefreshVisibility();
    }

    /// <summary>Resolve a meter by key with the same fallback chain as Selected.</summary>
    public TokenUsage? ResolveMeter(string key)
    {
        var list = AllUsages.ToList();
        if (string.IsNullOrWhiteSpace(key))
        {
            return FallbackSelect(list);
        }

        return list.FirstOrDefault(u => u.Key == key && u.Status != MeterStatus.NotConfigured)
               ?? list.FirstOrDefault(u => u.Key == key)
               ?? FallbackSelect(list);
    }

    /// <summary>Change which meter the tray icon shows.</summary>
    public void SetTrayMeter(string key)
    {
        Settings.TrayMeter = key;
        RefreshTrayDisplay();
    }

    private HashSet<string> EffectiveVisibleKeys()
    {
        if (Settings.VisibleMeters is not null)
        {
            return Settings.VisibleMeters.ToHashSet(StringComparer.Ordinal);
        }

        string key = Settings.SelectedMeter;
        if (AllUsages.Any(u => u.Key == key))
        {
            return new HashSet<string>(StringComparer.Ordinal) { key };
        }

        var first = AllUsages.FirstOrDefault();
        return first is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal) { first.Key };
    }

    private void RefreshTrayDisplay()
    {
        TrayDisplay = ResolveMeter(Settings.TrayMeter);
        TrayDisplayChanged?.Invoke();
    }

    private void ComputeWorst()
    {
        WorstStatus = VisibleUsages
            .Select(i => i.Usage)
            .OrderByDescending(StatusSeverity)
            .FirstOrDefault();
    }

    private static int StatusSeverity(TokenUsage usage)
    {
        if (usage.Status == MeterStatus.Error)
        {
            return 4;
        }

        if (usage.Status == MeterStatus.NotConfigured)
        {
            return 3;
        }

        if (usage.UsedPercent >= 100)
        {
            return 2;
        }

        return usage.UsedPercent >= 80 ? 1 : 0;
    }

    private static TokenUsage? FallbackSelect(IReadOnlyList<TokenUsage> list)
        => list.FirstOrDefault(u => u.Status == MeterStatus.Ok) ?? list.FirstOrDefault();
}