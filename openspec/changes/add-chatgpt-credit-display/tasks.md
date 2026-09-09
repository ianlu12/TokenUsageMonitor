# Tasks: Add ChatGPT Credit Display

## 1. Model & Catalog

- [x] 1.1 Add `UsageUnit` enum (`Request`, `Usd`) and `Unit` property to `TokenUsage` in `Models/TokenUsage.cs`
- [x] 1.2 Register the two new meters (`chatgpt:monthly-billing`「每月帳單」, `chatgpt:prepaid-credit`「預付餘額」) in `Models/UsageCatalog.cs`
- [x] 1.3 Add `ChatGptApiKey` setting to `Models/AppSettings.cs`

## 2. Provider

- [x] 2.1 Create `Services/ChatGptProvider.cs` implementing `IGptProvider` (`ProviderId="chatgpt"`, `ProviderName="ChatGPT / OpenAI"`, `UsageUrl=https://platform.openai.com/usage`)
- [x] 2.2 Implement `FetchUsageAsync` calling `/v1/dashboard/billing/subscription` + `/v1/dashboard/billing/usage` for the `monthly-billing` meter, normalizing `total_usage` cents and `hard_limit_usd` via `ToCents`
- [x] 2.3 Implement `credit_grants` parsing for the `prepaid-credit` meter (`total_granted`/`total_used`/`total_available`)
- [x] 2.4 Add 401/403 → invalid-key error and a descriptive "no usable billing data" error (throw → standard stale/error state)
- [x] 2.5 Set `Unit = UsageUnit.Usd` and `ResetAt` (`access_until`, else month-end) on returned meters; leave `Request` unit untouched for existing providers

## 3. Wiring

- [x] 3.1 Add `ChatGptProvider` construction and pass it to `UsageService` in `App.xaml.cs`
- [x] 3.2 Add `ChatGptProvider => settings.ChatGptApiKey` case in `UsageService.ApplyApiKeys`
- [x] 3.3 Add ChatGPT API key PasswordBox + hint text in `Views/SettingsWindow.xaml` and persist it in `SettingsWindow.xaml.cs`

## 4. Display

- [x] 4.1 Branch `UsageItem.FormatDetail` and `Percent` on `Unit` in `State/UsageItem.cs` to render `US$ x.xx` amounts for `Usd` meters (and usage-percent-only/「無限額度」 when `Total <= 0`)
- [x] 4.2 Verify no other display path assumes integer request counts; adjust `OverlayWindow`/tray formatting only if needed

## 5. Verification

- [ ] 5.1 Build with `dotnet build` — no warnings/errors introduced
- [ ] 5.2 Test: with a valid `sk-...` key, both meters appear and show US-dollar values; with an invalid key, error/stale state shows; without a key, 未設定 state shows
