using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TokenUsageMonitor.Models;

namespace TokenUsageMonitor.Services;

/// <summary>
/// GitHub Copilot quota provider.
///
/// Endpoint: GET https://api.github.com/copilot_internal/user
/// Auth: Authorization: Bearer {token}   (github token, e.g. `gh auth token`, gho_* session token)
///          with X-GitHub-Api-Version: 2025-05-01
///
/// The internal endpoint (the same one the VS Code Copilot extension calls)
/// returns real-time quota snapshots:
///   {
///     "copilot_plan": "individual",
///     "quota_snapshots": { "chat": {...}, "completions": {...}, "premium_interactions": {...} },
///     "quota_reset_date": "2026-03-01T05:30:00Z"
///   }
/// Each snapshot exposes entitlement/used/remaining and a percent remaining.
/// </summary>
public sealed class GitHubCopilotProvider : IGptProvider
{
    private const string Endpoint = "https://api.github.com/copilot_internal/user";
    private const string ApiVersion = "2025-05-01";

    private static readonly (string Id, string Name)[] Meters =
    {
        ("premium_interactions", "Premium 請求"),
        ("chat", "Chat 請求"),
        ("completions", "程式碼補全"),
    };

    private readonly HttpClient _http;
    private string? _apiKey;

    public GitHubCopilotProvider(string? apiKey)
    {
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public string ProviderId => "github-copilot";
    public string ProviderName => "GitHub Copilot";
    public string UsageUrl => "https://github.com/settings/copilot";
    public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

    public IReadOnlyList<string> MeterIds => Meters.Select(m => m.Id).ToArray();

    public string GetMeterName(string meterId)
        => Meters.FirstOrDefault(m => m.Id == meterId).Name ?? meterId;

    public void SetApiKey(string? apiKey)
        => _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;

    public async Task<IReadOnlyList<TokenUsage>> FetchUsageAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.UserAgent.ParseAdd("TokenUsageMonitor/1.0");
        request.Headers.Add("X-GitHub-Api-Version", ApiVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
            || resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("GitHub token 無效或無權限（401/403），請重新設定 API Key。");
        }

        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub API 回應錯誤：HTTP {(int)resp.StatusCode}");
        }

        string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return Parse(json);
    }

    private static IReadOnlyList<TokenUsage> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        DateTime? resetAt = TryGetDateTime(root, "quota_reset_date", "quota_reset_date_utc");

        if (!root.TryGetProperty("quota_snapshots", out JsonElement snapshots))
        {
            throw new InvalidOperationException("GitHub API 回應缺少 quota_snapshots 欄位。");
        }

        var now = DateTime.UtcNow;
        var result = new List<TokenUsage>();
        foreach ((string id, string name) in Meters)
        {
            if (!snapshots.TryGetProperty(id, out JsonElement s))
            {
                continue;
            }

            long? total = GetLong(s, "entitlement", "entitlementRequests");
            long? used = GetLong(s, "used", "usedRequests");
            long? remaining = GetLong(s, "remaining", "quota_remaining");
            double? percentRemaining = GetDouble(s, "percent_remaining");
            bool unlimited = GetBool(s, "unlimited") || total == -1;

            double usedPercent = 0;
            if (total is > 0)
            {
                usedPercent = used is > 0 ? 100.0 * used.Value / total.Value : 0;
                if (used is null && remaining is not null)
                {
                    usedPercent = total.Value > remaining.Value
                        ? 100.0 * (total.Value - remaining.Value) / total.Value
                        : 0;
                }
            }
            else if (percentRemaining is not null)
            {
                usedPercent = Math.Max(0, 100 - percentRemaining.Value);
            }

            result.Add(new TokenUsage
            {
                ProviderId = "github-copilot",
                ProviderName = "GitHub Copilot",
                UsageUrl = "https://github.com/settings/copilot",
                MeterId = id,
                MeterName = name,
                Used = used ?? (total is not null && remaining is not null ? Math.Max(0, total.Value - remaining.Value) : 0),
                Total = total ?? 0,
                UsedPercent = Math.Clamp(usedPercent, 0, 100),
                ResetAt = resetAt,
                IsUnlimited = unlimited,
                Status = MeterStatus.Ok,
                LastUpdated = now,
            });
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException("GitHub API 未回傳任何額度快照（可能是免費方案或無 Copilot 訂閱）。");
        }

        return result;
    }

    private static DateTime? TryGetDateTime(JsonElement el, params string[] names)
    {
        foreach (string name in names)
        {
            if (el.TryGetProperty(name, out JsonElement v)
                && v.ValueKind == JsonValueKind.String
                && DateTime.TryParse(v.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
            {
                return dt;
            }
        }

        return null;
    }

    private static long? GetLong(JsonElement el, params string[] names)
    {
        foreach (string name in names)
        {
            if (el.TryGetProperty(name, out JsonElement v))
            {
                if (v.ValueKind == JsonValueKind.Number)
                {
                    return v.TryGetInt64(out long l) ? l : (long)v.GetDouble();
                }

                if (v.ValueKind == JsonValueKind.String
                    && long.TryParse(v.GetString(), out long l2))
                {
                    return l2;
                }
            }
        }

        return null;
    }

    private static double? GetDouble(JsonElement el, params string[] names)
    {
        foreach (string name in names)
        {
            if (el.TryGetProperty(name, out JsonElement v))
            {
                if (v.ValueKind == JsonValueKind.Number)
                {
                    return v.GetDouble();
                }

                if (v.ValueKind == JsonValueKind.String
                    && double.TryParse(v.GetString(), out double d))
                {
                    return d;
                }
            }
        }

        return null;
    }

    private static bool GetBool(JsonElement el, string name)
    {
        return el.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.True;
    }
}