using System.IO;
using System.Windows;
using System.Windows.Threading;
using TokenUsageMonitor.Models;
using TokenUsageMonitor.Services;
using TokenUsageMonitor.State;
using TokenUsageMonitor.Views;

namespace TokenUsageMonitor;

/// <summary>
/// Application entry point / composition root. Owns services, the single-instance
/// mutex, the tray icon, the overlay window and the shutdown flow.
/// </summary>
public partial class App : System.Windows.Application
{
    private const string MutexName = @"Global\TokenUsageMonitor.SingleInstance";

    private Mutex? _mutex;
    private SettingsService? _settingsService;
    private StartupManager? _startupManager;
    private UsageService? _usageService;
    private TrayIconService? _tray;
    private OverlayWindow? _overlay;
    private SettingsWindow? _settingsWindow;
    private AppState? _state;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance: only the first process proceeds.
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Exit += OnExit;

        _settingsService = new SettingsService();
        AppSettings settings = _settingsService.Load();

        _state = new AppState(settings);

        var github = new GitHubCopilotProvider(
            KeyObfuscator.Deobfuscate(settings.GitHubCopilotApiKey));
        var opencode = new OpenCodeGoProvider(
            KeyObfuscator.Deobfuscate(settings.OpenCodeGoApiKey));
        var chatgptConsumer = new ChatGptConsumerProvider(
            KeyObfuscator.Deobfuscate(settings.ChatGptConsumerToken),
            KeyObfuscator.Deobfuscate(settings.ChatGptConsumerAccountId));

        _usageService = new UsageService(_state, github, opencode, chatgptConsumer);
        _startupManager = new StartupManager();

        _tray = new TrayIconService(_state)
        {
            StartupEnabled = () => _startupManager.IsEnabled(),
            OpenSettings = OpenSettingsWindow,
            ToggleOverlay = ToggleOverlay,
            ToggleAutoStart = ToggleAutoStart,
            ManualRefresh = () => _ = SafeRefreshAsync(),
            ExitApp = () => Shutdown(),
        };
        _tray.Build();

        _overlay = new OverlayWindow(
            _state,
            () => _settingsService.Save(),
            SafeRefreshAsync,
            ToggleOverlay);

        _state.UsagesChanged += _ =>
        {
            _state.RefreshVisibility();
            _overlay.RefreshStatusColor();
            _tray?.RefreshData();
        };

        // Start polling. Without API keys the providers simply report
        // "NotConfigured" and the app sits in a waiting state (no dialogs).
        _usageService.Start();
        _ = SafeRefreshAsync();

        if (settings.OverlayVisible)
        {
            _overlay.ShowOverlay();
        }
    }

    private Task SafeRefreshAsync() => _usageService?.RefreshAsync() ?? Task.CompletedTask;

    private void OpenSettingsWindow()
    {
        if (_settingsService is null || _state is null)
        {
            return;
        }

        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_state, _settingsService);
        _settingsWindow.SettingsSaved += OnSettingsSaved;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnSettingsSaved()
    {
        if (_settingsService is null || _usageService is null || _overlay is null || _tray is null)
        {
            return;
        }

        _usageService.ApplyApiKeys(_settingsService.Current);
        _usageService.SetPollInterval(_settingsService.Current.PollIntervalSeconds);
        _ = SafeRefreshAsync();

        if (_settingsService.Current.OverlayVisible)
        {
            _overlay.ShowOverlay();
        }
        else
        {
            _overlay.HideOverlay();
        }

        _tray.Update();
        _overlay.RefreshStatusColor();
    }

    private void ToggleOverlay()
    {
        if (_overlay is null || _settingsService is null || _tray is null)
        {
            return;
        }

        bool visible = _overlay.ToggleVisibility();
        _settingsService.Current.OverlayVisible = visible;
        _settingsService.Save();
        _tray.Update();
    }

    private void ToggleAutoStart()
    {
        if (_settingsService is null || _startupManager is null || _tray is null)
        {
            return;
        }

        bool enable = !_startupManager.IsEnabled();
        if (enable)
        {
            _startupManager.Enable();
        }
        else
        {
            _startupManager.Disable();
        }

        _settingsService.Current.AutoStartEnabled = enable;
        _settingsService.Save();
        _tray.Update();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _usageService?.Stop();
        _tray?.Dispose();
        _overlay?.Close();
        _mutex?.Dispose();
        _mutex = null;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Log to a file in %APPDATA% and keep the app alive for background errors.
        try
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                SettingsService.AppFolderName);
            Directory.CreateDirectory(logDir);
            File.AppendAllText(
                Path.Combine(logDir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}{Environment.NewLine}");
        }
        catch
        {
            // Nothing more we can do.
        }

        e.Handled = true;
    }
}
