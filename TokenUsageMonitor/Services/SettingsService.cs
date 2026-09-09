using System.IO;
using System.Text.Json;
using TokenUsageMonitor.Models;

namespace TokenUsageMonitor.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> to the user's %APPDATA% directory.
/// </summary>
public sealed class SettingsService
{
    public const string AppFolderName = "TokenUsageMonitor";
    public const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appDir = Path.Combine(baseDir, AppFolderName);
        Directory.CreateDirectory(appDir);
        _settingsPath = Path.Combine(appDir, SettingsFileName);
    }

    public string SettingsPath => _settingsPath;

    public AppSettings Current { get; private set; } = Metrics();

    /// <summary>Load settings from disk, falling back to defaults if missing/corrupt.</summary>
    public AppSettings Load()
    {
        Current = Read();
        return Current;
    }

    public AppSettings Read()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                string json = File.ReadAllText(_settingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    return Normalize(settings);
                }
            }
            catch (JsonException)
            {
                // Corrupt file: fall back to defaults and let Save overwrite it.
            }
            catch (IOException)
            {
            }
        }

        return Metrics();
    }

    public void Save()
    {
        Save(Current);
    }

    public void Save(AppSettings settings)
    {
        Current = Normalize(settings);
        string json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    /// <summary>Factory for default settings: overlay visible at bottom-right, 30s polling.</summary>
    public static AppSettings Defaults() => Metrics();

    private static AppSettings Metrics() => new();

    private static AppSettings Normalize(AppSettings settings)
    {
        if (settings.PollIntervalSeconds < 10)
        {
            settings.PollIntervalSeconds = AppSettings.DefaultPollIntervalSeconds;
        }

        if (string.IsNullOrWhiteSpace(settings.SelectedMeter))
        {
            settings.SelectedMeter = AppSettings.DefaultMeterKey;
        }

        if (string.IsNullOrWhiteSpace(settings.TrayMeter))
        {
            settings.TrayMeter = settings.SelectedMeter;
        }

        if (double.IsNaN(settings.OverlayOpacity) ||
            settings.OverlayOpacity is < 0.25 or > 1.0)
        {
            settings.OverlayOpacity = 1.0;
        }

        if (settings.VisibleMeters is { Count: > 0 })
        {
            var known = UsageCatalog.All.Select(m => m.Key).ToHashSet(StringComparer.Ordinal);
            settings.VisibleMeters = settings.VisibleMeters
                .Where(known.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        return settings;
    }
}