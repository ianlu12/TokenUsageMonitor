namespace TokenUsageMonitor.Models;

public enum UsageUnit
{
    Request,
    Usd,
}

public enum MeterStatus
{
    /// <summary>No API key configured yet; the meter is not polled.</summary>
    NotConfigured,

    /// <summary>Data is available and up to date.</summary>
    Ok,

    /// <summary>The last fetch failed; values (if any) are from the last successful fetch.</summary>
    Error,
}

/// <summary>
/// Standardized usage meter shared by all providers so the tray icon and the
/// overlay can render every service uniformly.
/// </summary>
public sealed class TokenUsage
{
    public string ProviderId { get; init; } = string.Empty;

    public string ProviderName { get; init; } = string.Empty;

    /// <summary>Provider-specific meter id (e.g. chat, completions, rolling, weekly).</summary>
    public string MeterId { get; init; } = string.Empty;

    public string MeterName { get; init; } = string.Empty;

    /// <summary>URL to the provider's token usage dashboard.</summary>
    public string UsageUrl { get; init; } = string.Empty;

    /// <summary>Credits / requests used this period.</summary>
    public long Used { get; init; }

    /// <summary>Total entitlement for this period. 0 means unknown/unlimited.</summary>
    public long Total { get; init; }

    /// <summary>Used percentage, 0-100.</summary>
    public double UsedPercent { get; init; }

    public DateTime? ResetAt { get; init; }

    public bool IsUnlimited { get; init; }

    /// <summary>Unit used to format the meter's amounts.</summary>
    public UsageUnit Unit { get; init; } = UsageUnit.Request;

    public MeterStatus Status { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime LastUpdated { get; init; }

    /// <summary>Stable key ("{ProviderId}:{MeterId}") used to remember the selection.</summary>
    public string Key => $"{ProviderId}:{MeterId}";

    public long Remaining => Total > Used ? Total - Used : 0;
}
