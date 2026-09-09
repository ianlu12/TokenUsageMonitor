using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokenUsageMonitor.Common;
using TokenUsageMonitor.Models;
using TokenUsageMonitor.State;

namespace TokenUsageMonitor.Views;

/// <summary>
/// Borderless, always-on-top floating bar showing the selected meters.
/// Can be dragged anywhere with the mouse (position remembered) and exposes a
/// right-click context menu to pick visible meters, adjust background opacity,
/// refresh and hide the overlay.
/// </summary>
public partial class OverlayWindow : Window
{
    private const double DefaultMargin = 24;

    private readonly AppState _state;
    private readonly Action _persistSettings;
    private readonly Func<Task> _manualRefresh;
    private readonly Action _toggleOverlay;
    private bool _positionInitialized;

    public OverlayWindow(
        AppState state,
        Action persistSettings,
        Func<Task> manualRefresh,
        Action toggleOverlay)
    {
        InitializeComponent();
        _state = state;
        _persistSettings = persistSettings;
        _manualRefresh = manualRefresh;
        _toggleOverlay = toggleOverlay;

        DataContext = state;
        LocationChanged += OnLocationChanged;

        PopulateDisplayItems();
        UpdateOpacityCheckState();
        UpdateEmptyState();

        _state.VisibilityChanged += UpdateEmptyState;
    }

    /// <summary>Show the overlay at the saved position (or bottom-right on first run).</summary>
    public void ShowOverlay()
    {
        Show();
        // Let SizeToContent measure the window first so Width/Height are valid
        // when computing the bottom-right default position.
        if (!_positionInitialized)
        {
            ApplySavedPosition();
            _positionInitialized = true;
        }

        UpdateEmptyState();
        RefreshStatusColor();
    }

    public void HideOverlay() => Hide();

    /// <summary>Toggle visibility; returns true when the overlay is now visible.</summary>
    public bool ToggleVisibility()
    {
        if (IsVisible)
        {
            Hide();
            return false;
        }

        ShowOverlay();
        return true;
    }

    /// <summary>Re-read the worst visible meter's state to update the status dot color.</summary>
    public void RefreshStatusColor()
    {
        StatusDot.Fill = UsageItem.StatusBrushFor(_state.WorstStatus);
    }

    private void ApplySavedPosition()
    {
        double? x = _state.Settings.OverlayX;
        double? y = _state.Settings.OverlayY;

        if (x is null || double.IsNaN(x.Value) || y is null || double.IsNaN(y.Value))
        {
            Rect wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - DefaultMargin;
            Top = wa.Bottom - Height - DefaultMargin;
        }
        else
        {
            Left = x.Value;
            Top = y.Value;
        }
    }

    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            // DragMove() blocks until the mouse button is released and moves
            // the whole borderless window (WM_NCLBUTTONDOWN capture).
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // Left button released before DragMove was called; ignore.
        }

        // Persist the final position once, after the drag finishes.
        _state.Settings.OverlayX = Left;
        _state.Settings.OverlayY = Top;
        _persistSettings();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (!_positionInitialized)
        {
            return;
        }

        // Track live position in settings (persisted on drag release).
        _state.Settings.OverlayX = Left;
        _state.Settings.OverlayY = Top;
    }

    // ------------------------------------------------------------ context menu

    private void PopulateDisplayItems()
    {
        HashSet<string> checkedKeys = EffectiveDisplayKeys();

        // Meters appear as top-level checkable items at the top of the context
        // menu; the static entries (separator, opacity, actions) follow below.
        int index = 0;
        foreach (var meter in UsageCatalog.All)
        {
            var item = new MenuItem
            {
                Header = $"{meter.ProviderName} · {meter.MeterName}",
                Tag = meter.Key,
                IsCheckable = true,
                // Set the initial state BEFORE attaching the events so startup
                // does not trigger the Checked/Unchecked persistence path.
                IsChecked = checkedKeys.Contains(meter.Key),
            };
            item.Checked += OnDisplayItemChecked;
            item.Unchecked += OnDisplayItemUnchecked;
            OverlayMenu.Items.Insert(index++, item);
        }
    }

    private HashSet<string> EffectiveDisplayKeys()
    {
        if (_state.Settings.VisibleMeters is { } list)
        {
            return list.ToHashSet(StringComparer.Ordinal);
        }

        // Not customized yet → mirror the fallback used by AppState.
        return new HashSet<string>(StringComparer.Ordinal)
        {
            _state.Settings.SelectedMeter,
        };
    }

    private void OnDisplayItemChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string key })
        {
            return;
        }

        _state.UpdateVisibleMeters(keys =>
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        });
        _persistSettings();
    }

    private void OnDisplayItemUnchecked(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string key })
        {
            return;
        }

        _state.UpdateVisibleMeters(keys => keys.Remove(key));
        _persistSettings();
    }

    private void OnOpacityChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag } || !double.TryParse(tag, out double opacity))
        {
            return;
        }

        _state.Opacity = opacity;
        _state.Settings.OverlayOpacity = opacity;
        UpdateOpacityCheckState();
        _persistSettings();
    }

    private void UpdateOpacityCheckState()
    {
        (MenuItem Item, double Value)[] options =
        {
            (Opacity100, 1.0),
            (Opacity75, 0.75),
            (Opacity50, 0.5),
            (Opacity25, 0.25),
        };

        foreach (var (item, value) in options)
        {
            item.IsChecked = Math.Abs(value - _state.Opacity) < 0.001;
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) => _ = _manualRefresh();

    private void OnHideClick(object sender, RoutedEventArgs e) => _toggleOverlay();

    private void UpdateEmptyState()
    {
        bool hasItems = _state.VisibleUsages.Count > 0;
        EmptyRow.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
        MeterList.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        RefreshStatusColor();
    }
}