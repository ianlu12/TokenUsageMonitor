using System.Windows;
using TokenUsageMonitor.Models;
using TokenUsageMonitor.Services;
using TokenUsageMonitor.State;

namespace TokenUsageMonitor.Views;

/// <summary>
/// Settings dialog: API keys for both providers, overlay visibility and the
/// polling interval.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppState _state;
    private readonly SettingsService _settingsService;

    public event Action? SettingsSaved;

    public SettingsWindow(AppState state, SettingsService settingsService)
    {
        InitializeComponent();
        _state = state;
        _settingsService = settingsService;

        GithubKeyBox.Password = KeyObfuscator.Deobfuscate(state.Settings.GitHubCopilotApiKey);
        OpencodeKeyBox.Password = KeyObfuscator.Deobfuscate(state.Settings.OpenCodeGoApiKey);
        ChatGptConsumerTokenBox.Password = KeyObfuscator.Deobfuscate(state.Settings.ChatGptConsumerToken);
        ChatGptConsumerAccountIdBox.Password = KeyObfuscator.Deobfuscate(state.Settings.ChatGptConsumerAccountId);
        OverlayVisibleCheck.IsChecked = state.Settings.OverlayVisible;
        PollIntervalBox.Text = state.Settings.PollIntervalSeconds.ToString();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        AppSettings settings = _state.Settings;

        settings.GitHubCopilotApiKey = KeyObfuscator.Obfuscate(GithubKeyBox.Password);
        settings.OpenCodeGoApiKey = KeyObfuscator.Obfuscate(OpencodeKeyBox.Password);
        settings.ChatGptConsumerToken = KeyObfuscator.Obfuscate(ChatGptConsumerTokenBox.Password);
        settings.ChatGptConsumerAccountId = KeyObfuscator.Obfuscate(ChatGptConsumerAccountIdBox.Password);
        settings.OverlayVisible = OverlayVisibleCheck.IsChecked == true;

        int.TryParse(PollIntervalBox.Text, out int seconds);
        if (seconds >= 10)
        {
            settings.PollIntervalSeconds = seconds;
        }

        _settingsService.Save(settings);
        SettingsSaved?.Invoke();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
