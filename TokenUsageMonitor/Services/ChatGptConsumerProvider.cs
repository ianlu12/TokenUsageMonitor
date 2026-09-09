using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TokenUsageMonitor.Models;

namespace TokenUsageMonitor.Services;

/// <summary>
/// ChatGPT consumer subscription (Plus / Pro) quota provider.
///
/// Unlike OpenAI's API billing (api.openai.com), this provider reads the
/// consumer-plan quota that the ChatGPT web/app clients show: a 5-hour sliding
/// window and a weekly window.
///
/// Endpoint: GET https://chatgpt.com/backend-api/wham/usage
/// Auth: Authorization: Bearer {access token from the chatgpt.com session}
///       ChatGPT-Account-Id: {account id}   (optional, some accounts require it)
///
/// Response shape:
///   {
///     "plan_type": "plus",
///     "rate_limit": {
///       "primary_window":   { "used_percent": 55, "limit_window_seconds": 18000,  "reset_at": 1778670307 },
///       "secondary_window": { "used_percent": 51, "limit_window_seconds": 604800, "reset_at": 1779157165 }
///     }
///   }
///
/// The windows only carry a used percentage + reset timestamp (no absolute
/// used/total numbers), so meters are produced with Used=0 / Total=0 and the
/// percentage drives the display. Plan display names are resolved best-effort
/// from /accounts/check (falling back to plan_type) and never block usage.
/// </summary>
public sealed class ChatGptConsumerProvider : IGptProvider
{
    private const string UsageEndpoint = "https://chatgpt.com/backend-api/wham/usage";
    private const string AccountCheckEndpoint = "https://chatgpt.com/backend-api/accounts/check/v4-2023-04-27";

    private static readonly (string Id, string Name)[] Meters =
    {
        ("five-hour", "5 小時滾動"),
        ("weekly", "每週"),
    };

    private readonly HttpClient _http;
    private string? _token;
    private string? _accountId;
    private string _planName = "ChatGPT";

    public ChatGptConsumerProvider(string? token, string? accountId = null)
    {
        _token = string.IsNullOrWhiteSpace(token) ? null : token;
        _accountId = string.IsNullOrWhiteSpace(accountId) ? null : accountId;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public string ProviderId => "chatgpt-consumer";

    public string ProviderName => _planName;

    public string UsageUrl => "https://chatgpt.com/";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_token);

    public IReadOnlyList<string> MeterIds => Meters.Select(m => m.Id).ToArray();

    public string GetMeterName(string meterId)
        => Meters.FirstOrDefault(m => m.Id == meterId).Name ?? meterId;

    public void SetApiKey(string? apiKey)
        => _token = NormalizeToken(apiKey);

    public void SetAccountId(string? accountId)
        => _accountId = string.IsNullOrWhiteSpace(accountId) ? null : accountId;

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        const string BearerPrefix = "Bearer";
        if (trimmed.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.AsSpan(BearerPrefix.Length).Trim().ToString();
        }

        return trimmed;
    }

    public async Task<IReadOnlyList<TokenUsage>> FetchUsageAsync(CancellationToken ct)
    {
        if (!HasApiKey)
        {
            throw new InvalidOperationException("ChatGPT access token 尚未設定。");
        }

        JsonElement usage = await FetchUsageElementAsync(ct).ConfigureAwait(false);
        _planName = await ResolvePlanNameAsync(usage, ct).ConfigureAwait(false);

        JsonElement rateLimit = GetRateLimit(usage);

        var now = DateTime.UtcNow;
        var result = new List<TokenUsage>();

        if (TryGetWindow(rateLimit, "primary_window", "five_hour", "five_hour_limit", "five_hour_rate_limit") is { } primary)
        {
            result.Add(BuildUsage(Meters[0].Id, Meters[0].Name, primary, now));
        }

        if (TryGetWindow(rateLimit, "secondary_window", "weekly", "weekly_limit", "weekly_rate_limit") is { } secondary)
        {
            result.Add(BuildUsage(Meters[1].Id, Meters[1].Name, secondary, now));
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException("ChatGPT API 未回傳任何額度視窗（此帳號可能無可用額度）。");
        }

        return result;
    }

    // ------------------------------------------------------------ HTTP

    private async Task<JsonElement> FetchUsageElementAsync(CancellationToken ct)
    {
        using var resp = await SendAsync(UsageEndpoint, ct).ConfigureAwait(false);

        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "ChatGPT access token 已過期或無效，請重新設定（部分帳號需一併設定 ChatGPT Account Id）。");
        }

        if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("無權限存取 ChatGPT 額度（可能為免費版或帳號遭停用）。");
        }

        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ChatGPT API 回應錯誤：HTTP {(int)resp.StatusCode}");
        }

        string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private async Task<HttpResponseMessage> SendAsync(string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        request.Headers.Referrer = new Uri("https://chatgpt.com/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Origin", "https://chatgpt.com");
        if (!string.IsNullOrWhiteSpace(_accountId))
        {
            request.Headers.Add("ChatGPT-Account-Id", _accountId);
        }

        return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------ plan name

    /// <summary>
    /// Best-effort plan display name: prefer /accounts/check display_name,
    /// fall back to the plan_type seen in /wham/usage, then the generic label.
    /// </summary>
    private async Task<string> ResolvePlanNameAsync(JsonElement usage, CancellationToken ct)
    {
        string strongFallback = PlanNameForType(GetString(usage, "plan_type") ?? string.Empty);
        string? display = await TryFetchPlanDisplayNameAsync(ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(display) ? strongFallback : display;
    }

    private async Task<string?> TryFetchPlanDisplayNameAsync(CancellationToken ct)
    {
        try
        {
            using var resp = await SendAsync(AccountCheckEndpoint, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("accounts", out JsonElement accounts)
                && accounts.TryGetProperty("default", out JsonElement account)
                && account.TryGetProperty("consumer_plan", out JsonElement plan)
                && GetString(plan, "display_name") is { Length: > 0 } display)
            {
                return display;
            }

            if (root.TryGetProperty("consumer_plan", out JsonElement topLevel)
                && GetString(topLevel, "display_name") is { Length: > 0 } topDisplay)
            {
                return topDisplay;
            }
        }
        catch (HttpRequestException)
        {
            // best-effort：網路失敗不中斷額度顯示
        }
        catch (JsonException)
        {
        }
        catch (TimeoutException)
        {
        }

        return null;
    }

    private static string PlanNameForType(string planType)
    {
        return planType.ToLowerInvariant() switch
        {
            "free" => "ChatGPT 免費版",
            "plus" => "ChatGPT Plus",
            "pro" => "ChatGPT Pro",
            "proteam" or "team" => "ChatGPT Team",
            "prolite" => "ChatGPT Pro Lite",
            "enterprise" => "ChatGPT Enterprise",
            _ => "ChatGPT",
        };
    }

    // ------------------------------------------------------------ parse

    private static JsonElement GetRateLimit(JsonElement root)
    {
        if (root.TryGetProperty("rate_limit", out JsonElement rateLimit))
        {
            return rateLimit;
        }

        throw new InvalidOperationException("ChatGPT API 回應缺少 rate_limit 欄位。");
    }

    private static JsonElement? TryGetWindow(JsonElement rateLimit, params string[] names)
    {
        foreach (string name in names)
        {
            if (rateLimit.TryGetProperty(name, out JsonElement window) && window.ValueKind == JsonValueKind.Object)
            {
                return window;
            }
        }

        return null;
    }

    private TokenUsage BuildUsage(string meterId, string meterName, JsonElement window, DateTime now)
    {
        double percent = Math.Clamp(GetDouble(window, "used_percent", "usage_percent", "percent") ?? 0, 0, 100);

        return new TokenUsage
        {
            ProviderId = ProviderId,
            ProviderName = ProviderName,
            UsageUrl = UsageUrl,
            MeterId = meterId,
            MeterName = meterName,
            Used = 0,
            Total = 0,
            UsedPercent = percent,
            ResetAt = GetResetAt(window),
            IsUnlimited = false,
            Unit = UsageUnit.Request,
            Status = MeterStatus.Ok,
            LastUpdated = now,
        };
    }

    private static DateTime? GetResetAt(JsonElement window)
    {
        if (GetLong(window, "reset_at", "resetAt", "resets_at") is long unix && unix > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        }

        if (GetLong(window, "reset_after_seconds", "resets_in_seconds", "resets_in_sec") is long seconds && seconds > 0)
        {
            return DateTime.UtcNow.AddSeconds(seconds);
        }

        return null;
    }

    private static string? GetString(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String)
        {
            return v.GetString();
        }

        return null;
    }

    private static long? GetLong(JsonElement el, params string[] names)
    {
        foreach (string name in names)
        {
            if (el.TryGetProperty(name, out JsonElement v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out long value))
                {
                    return value;
                }

                if (v.ValueKind == JsonValueKind.String
                    && long.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
                {
                    return parsed;
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
                    && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }
}