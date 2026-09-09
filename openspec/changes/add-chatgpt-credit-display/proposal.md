# Add ChatGPT Credit Display

## Why

The app currently monitors GitHub Copilot and OpenCode Go quota, but users of ChatGPT / OpenAI API have no way to see their remaining credit balance. Users need a lightweight, always-visible readout of their OpenAI billing usage and prepaid credit so they never hit a billing cap mid-task.

## What Changes

- Add a new provider, **ChatGPT / OpenAI** (`chatgpt`), polled alongside the existing providers.
- Add a `ChatGptApiKey` setting (obfuscated like the existing keys) with a new input field in the settings window.
- Use the OpenAI dashboard/billing endpoints (`/v1/dashboard/billing/subscription`, `/v1/dashboard/billing/usage`, `/v1/dashboard/billing/credit_grants`) to report:
  - monthly billing usage vs. the hard limit, and
  - prepaid credit remaining (granted / used / available).
- Extend the standardized usage model with a **unit** concept so money-based meters are displayed as `US$ x.xx` instead of raw integer counts, while request-based meters keep today's formatting.
- Register the new meters in `UsageCatalog` so they are selectable in the tray icon and floating overlay with no per-file UI changes.
- Add failure/not-configured handling consistent with the existing providers.

## Capabilities

### New Capabilities

- `chatgpt-credit-monitoring`: query OpenAI/ChatGPT billing endpoints, standardize credits/limits into the shared usage model, and surface them in the tray icon and overlay with correct US-dollar formatting.

### Modified Capabilities

- `token-quota-monitoring`: "解析與標準化使用量資料" requirement now also standardizes money-denominated meters (USD credits) in addition to request counts, so the shared model carries an explicit unit.

## Impact

- **Services**: new `ChatGptProvider` (implements `IGptProvider`); extended `UsageService.ApplyApiKeys` and `App` composition root.
- **Models**: `TokenUsage` gains a unit field; `UsageCatalog` gains the new meters; `AppSettings` gains `ChatGptApiKey`.
- **State/UI**: `UsageItem`/overlay formatting branches on unit to render `US$` values; settings window gets a new API key field.
- **NuGet / dependencies**: none new (uses built-in `HttpClient` + `System.Text.Json`).