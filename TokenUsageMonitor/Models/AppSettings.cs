using System.Text.Json.Serialization;

namespace TokenUsageMonitor.Models;

/// <summary>
/// Application settings persisted to %APPDATA%\TokenUsageMonitor\settings.json.
/// API keys are stored XOR-obfuscated (reversible obfuscation only, NOT encryption —
/// this is a documented limitation for the first version).
/// </summary>
public sealed class AppSettings
{
    public const int DefaultPollIntervalSeconds = 30;

    public const string DefaultMeterKey = "github-copilot:premium_interactions";

    /// <summary>Selected usage meter key in "{providerId}:{meterId}" format.</summary>
    public string SelectedMeter { get; set; } = DefaultMeterKey;

    public int PollIntervalSeconds { get; set; } = DefaultPollIntervalSeconds;

    public bool OverlayVisible { get; set; } = true;

    /// <summary>Last known overlay position. null / NaN means "default to bottom-right".</summary>
    public double? OverlayX { get; set; }

    public double? OverlayY { get; set; }

    public bool AutoStartEnabled { get; set; }

    /// <summary>
    /// Meter keys shown on the floating bar. null = not customized yet (fall
    /// back to <see cref="SelectedMeter"/>); empty list = user intentionally
    /// disabled every meter (placeholder shown).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? VisibleMeters { get; set; }

    /// <summary>Overlay background opacity, 1.0 = fully opaque.</summary>
    public double OverlayOpacity { get; set; } = 1.0;

    /// <summary>Meter key displayed on the tray icon.</summary>
    public string TrayMeter { get; set; } = DefaultMeterKey;

    /// <summary>Obfuscated GitHub Copilot token (gh auth token / gho_* session token).</summary>
    public string GitHubCopilotApiKey { get; set; } = string.Empty;

    /// <summary>Obfuscated OpenCode Go API key (~/.local/share/opencode/auth.json).</summary>
    public string OpenCodeGoApiKey { get; set; } = string.Empty;

    /// <summary>Obfuscated ChatGPT consumer subscription access token (chatgpt.com session).</summary>
    public string ChatGptConsumerToken { get; set; } = string.Empty;

    /// <summary>Obfuscated ChatGPT consumer account id (optional, e.g. from ChatDev/~/.codex/auth.json).</summary>
    public string ChatGptConsumerAccountId { get; set; } = string.Empty;
}
