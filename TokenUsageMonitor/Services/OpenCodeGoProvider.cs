using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TokenUsageMonitor.Models;

namespace TokenUsageMonitor.Services;

/// <summary>
/// OpenCode Go quota provider.
///
/// Endpoint: GET https://opencode.ai/zen/go/v1/usage
/// Auth: Authorization: Bearer {apiKey}   (the opencode-go key from ~/.local/share/opencode/auth.json)
///
/// Production response shape (used percent, matching the Go dashboard):
///   { "usage": { "rolling": { "status": "...", "percent": 65, "resetsAt": "..." },
///                "weekly":  {...}, "monthly": {...} } }
/// Older/predicted shapes ("usage_percent" / "resets_in_seconds") are also tolerated.
/// </summary>
public sealed class OpenCodeGoProvider : IGptProvider
{
    private const string Endpoint = "https://opencode.ai/zen/go/v1/usage";

    private static readonly (string Id, string Name)[] Meters =
    {
        ("rolling", "5 小時滾動"),
        ("weekly", "每週"),
        ("monthly", "每月"),
    };

    private readonly HttpClient _http;
    private string? _apiKey;

    public OpenCodeGoProvider(string? apiKey)
    {
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public string ProviderId => "opencode-go";
    public string ProviderName => "OpenCode Go";
    public string UsageUrl => "https://opencode.ai/dashboard";
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
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("OpenCode Go API Key 無效（401/403），請重新設定。");
        }

        // 403 EntitlementError is what the API returns for a valid key without an active Go plan.
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound
            || (int)resp.StatusCode == 403)
        {
            throw new InvalidOperationException("此 API Key 尚無有效的 OpenCode Go 方案。");
        }

        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenCode Go API 回應錯誤：HTTP {(int)resp.StatusCode}");
        }

        string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return Parse(json);
    }

    private static IReadOnlyList<TokenUsage> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        JsonElement usage = root.TryGetProperty("usage", out JsonElement u) ? u : root;

        var now = DateTime.UtcNow;
        var result = new List<TokenUsage>();
        foreach ((string id, string name) in Meters)
        {
            if (!usage.TryGetProperty(id, out JsonElement window))
            {
                continue;
            }

            double percent = GetDouble(window, "percent", "usage_percent", "used_percent") ?? 0;
            percent = Math.Clamp(percent, 0, 100);

            long? used = GetLong(window, "used", "used_requests", "usage");
            long? total = GetLong(window, "limit", "total", "entitlement", "quota", "allowed");

            DateTime? resetsAt = GetDateTime(window, "resetsAt", "resets_at", "reset_at");
            if (resetsAt is null && GetLong(window, "resets_in_seconds", "resets_in_sec") is long secs)
            {
                resetsAt = now.AddSeconds(secs);
            }

            result.Add(new TokenUsage
            {
                ProviderId = "opencode-go",
                ProviderName = "OpenCode Go",
                UsageUrl = "https://opencode.ai/dashboard",
                MeterId = id,
                MeterName = name,
                Used = used ?? (total is not null ? (long)Math.Round(total.Value * percent / 100.0) : 0),
                Total = total ?? 0,
                UsedPercent = percent,
                ResetAt = resetsAt,
                IsUnlimited = false,
                Status = MeterStatus.Ok,
                LastUpdated = now,
            });
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException("OpenCode Go API 未回傳任何用量視窗資料。");
        }

        return result;
    }

    private static DateTime? GetDateTime(JsonElement el, params string[] names)
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
            if (el.TryGetProperty(name, out JsonElement v)
                && v.ValueKind == JsonValueKind.Number
                && v.TryGetInt64(out long value))
            {
                return value;
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
}