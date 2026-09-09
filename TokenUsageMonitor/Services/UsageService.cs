using System.Collections.Concurrent;
using System.Windows.Threading;
using TokenUsageMonitor.Models;
using TokenUsageMonitor.State;

namespace TokenUsageMonitor.Services;

/// <summary>
/// Orchestrates polling of every token-quota provider.
/// Uses a DispatcherTimer so updates arrive on the UI thread and can directly
/// update notified bindings / the tray icon.
/// On provider failure the last successful data is kept and the meter is
/// flagged with an error status; the next tick simply retries.
/// </summary>
public sealed class UsageService
{
    private readonly AppState _state;
    private readonly IReadOnlyList<IGptProvider> _providers;
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, TokenUsage> _cache = new(StringComparer.Ordinal);
    private bool _refreshing;

    public UsageService(AppState state, params IGptProvider[] providers)
    {
        _state = state;
        _providers = providers;

        int seconds = Math.Max(10, state.Settings.PollIntervalSeconds);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        _timer.Tick += async (_, _) => await RefreshAsync();
    }

    /// <summary>Push API keys from settings into every provider.</summary>
    public void ApplyApiKeys(AppSettings settings)
    {
        foreach (var provider in _providers)
        {
            string key = provider switch
            {
                GitHubCopilotProvider => settings.GitHubCopilotApiKey,
                OpenCodeGoProvider => settings.OpenCodeGoApiKey,
                ChatGptConsumerProvider => settings.ChatGptConsumerToken,
                _ => string.Empty,
            };

            provider.SetApiKey(KeyObfuscator.Deobfuscate(key));

            if (provider is ChatGptConsumerProvider consumer)
            {
                consumer.SetAccountId(KeyObfuscator.Deobfuscate(settings.ChatGptConsumerAccountId));
            }
        }
    }

    /// <summary>Change the polling interval at runtime (from the settings window).</summary>
    public void SetPollInterval(int seconds)
    {
        int clamped = Math.Max(10, seconds);
        _timer.Interval = TimeSpan.FromSeconds(clamped);
        _state.Settings.PollIntervalSeconds = clamped;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _cts.Cancel();
    }

    /// <summary>Force an immediate refresh (asynchronous, fire-and-forget friendly).</summary>
    public async Task RefreshAsync()
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        try
        {
            var tasks = _providers.Select(p => FetchProviderAsync(p, _cts.Token)).ToList();
            var all = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();
            foreach (var u in all)
            {
                _cache[u.Key] = u;
            }

            _state.ApplyUsages(all);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task<IReadOnlyList<TokenUsage>> FetchProviderAsync(IGptProvider provider, CancellationToken ct)
    {
        if (!provider.HasApiKey)
        {
            return NotConfiguredUsages(provider);
        }

        try
        {
            return await provider.FetchUsageAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return StaleUsages(provider, "已取消");
        }
        catch (Exception ex)
        {
            return StaleUsages(provider, ex.Message);
        }
    }

    private IReadOnlyList<TokenUsage> NotConfiguredUsages(IGptProvider provider)
    {
        return provider.MeterIds.Select(meterId => new TokenUsage
        {
            ProviderId = provider.ProviderId,
            ProviderName = provider.ProviderName,
            UsageUrl = provider.UsageUrl,
            MeterId = meterId,
            MeterName = provider.GetMeterName(meterId),
            Status = MeterStatus.NotConfigured,
            LastUpdated = DateTime.UtcNow,
        }).ToList();
    }

    /// <summary>
    /// On failure, reuse the last good numbers for this provider's meters but
    /// flag them as errors so the UI can show the stale state.
    /// </summary>
    private IReadOnlyList<TokenUsage> StaleUsages(IGptProvider provider, string error)
    {
        return provider.MeterIds.Select(meterId =>
        {
            string key = $"{provider.ProviderId}:{meterId}";
            return _cache.TryGetValue(key, out TokenUsage? last)
                ? new TokenUsage
                {
                    ProviderId = last.ProviderId,
                    ProviderName = last.ProviderName,
                    UsageUrl = last.UsageUrl,
                    MeterId = last.MeterId,
                    MeterName = last.MeterName,
                    Used = last.Used,
                    Total = last.Total,
                    UsedPercent = last.UsedPercent,
                    ResetAt = last.ResetAt,
                    IsUnlimited = last.IsUnlimited,
                    Unit = last.Unit,
                    Status = MeterStatus.Error,
                    ErrorMessage = error,
                    LastUpdated = last.LastUpdated,
                }
                : new TokenUsage
                {
                    ProviderId = provider.ProviderId,
                    ProviderName = provider.ProviderName,
                    UsageUrl = provider.UsageUrl,
                    MeterId = meterId,
                    MeterName = provider.GetMeterName(meterId),
                    Status = MeterStatus.Error,
                    ErrorMessage = error,
                    LastUpdated = DateTime.UtcNow,
                };
        }).ToList();
    }
}
