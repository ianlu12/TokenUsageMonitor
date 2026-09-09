using TokenUsageMonitor.Models;

namespace TokenUsageMonitor.Services;

/// <summary>
/// Abstraction over a token-quota service (GitHub Copilot, OpenCode Go, ...).
/// Each provider exposes one or more usage "meters" (e.g. chat/completions,
/// or rolling/weekly/monthly windows).
/// </summary>
public interface IGptProvider
{
    /// <summary>Stable provider id, e.g. "github-copilot".</summary>
    string ProviderId { get; }

    /// <summary>Display name, e.g. "GitHub Copilot".</summary>
    string ProviderName { get; }

    /// <summary>URL to the provider's token usage dashboard.</summary>
    string UsageUrl { get; }

    bool HasApiKey { get; }

    void SetApiKey(string? apiKey);

    /// <summary>Meter ids this provider can report.</summary>
    IReadOnlyList<string> MeterIds { get; }

    /// <summary>Human-readable name for a meter id.</summary>
    string GetMeterName(string meterId);

    /// <summary>
    /// Fetch current usage for every meter.
    /// Must only be called when <see cref="HasApiKey"/> is true.
    /// </summary>
    Task<IReadOnlyList<TokenUsage>> FetchUsageAsync(CancellationToken ct);
}