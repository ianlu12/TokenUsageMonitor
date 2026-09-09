namespace TokenUsageMonitor.Models;

/// <summary>
/// Static catalog of every usage meter this app can display, independent of
/// whether an API key has been configured yet. Used to populate the settings
/// window's model selector.
/// </summary>
public static class UsageCatalog
{
    public readonly record struct Meter(string ProviderId, string ProviderName, string MeterId, string MeterName)
    {
        public string Key => $"{ProviderId}:{MeterId}";
    }

    public static readonly Meter[] All =
    {
        new("github-copilot", "GitHub Copilot", "premium_interactions", "Premium 請求"),
        new("github-copilot", "GitHub Copilot", "chat", "Chat 請求"),
        new("github-copilot", "GitHub Copilot", "completions", "程式碼補全"),
        new("opencode-go", "OpenCode Go", "rolling", "5 小時滾動"),
        new("opencode-go", "OpenCode Go", "weekly", "每週"),
        new("opencode-go", "OpenCode Go", "monthly", "每月"),
        new("chatgpt-consumer", "ChatGPT", "five-hour", "5 小時滾動"),
        new("chatgpt-consumer", "ChatGPT", "weekly", "每週"),
    };
}
