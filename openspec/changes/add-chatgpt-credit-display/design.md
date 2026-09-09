# Design: ChatGPT Credit Display

## Context

The app (`TokenUsageMonitor`, .NET 8 WPF) polls quota providers through the `IGptProvider` interface and renders every meter uniformly via the `TokenUsage` model in the tray icon and floating overlay. Today two providers exist: GitHub Copilot (request counts per meter) and OpenCode Go (request counts per window). Both express usage as whole-number request counts (`long Used/Total`, integer `UsedPercent`).

ChatGPT/OpenAI quota is denominated in US dollars, not requests. The OpenAI billing endpoints change frequently and are largely undocumented for this use, so the provider must be tolerant of several response shapes.

## Goals / Non-Goals

**Goals:**
- Add a third provider (`chatgpt`) reading OpenAI billing endpoints.
- Display monthly billing usage vs. hard limit and prepaid credit remaining.
- Support US-dollar meters in the shared model + overlay/tray formatting without touching the two existing providers.
- Consistent behavior with existing providers: polling, not-configured, error/stale handling, settings field.

**Non-Goals:**
- No support for API response caching, usage history graphs, or exporting billing data (no period history beyond current month).
- No encrypted (DPAPI) secret storage — stays consistent with the existing XOR-obfuscation limitation.
- No automatic key discovery; the key is pasted into settings like the other providers.

## Decisions

### D1. Provider id "chatgpt" with two meters

`ProviderId = "chatgpt"`, `ProviderName = "ChatGPT / OpenAI"`, `UsageUrl = https://platform.openai.com/usage`.
Two meters in the catalog:

- `monthly-billing` → 「每月帳單」: `Used` = current-month usage, `Total` = `hard_limit_usd`, reset at `access_until` (or start of next month).
- `prepaid-credit` → 「預付餘額」: `Used` = used credits, `Total` = granted credits; the display naturally shows remaining = available.

Rationale: one provider mapping to the two billing concepts OpenAI actually exposes; selecting either is a normal catalog meter pick. Alternative (single "credit" meter) loses the subscription-limit view.

### D2. Model gains a `UsageUnit` (money vs. request)

Add `UsageUnit Unit` to `TokenUsage` with values `Request` (default) and `Usd`. Money amounts are stored as **cents** in the existing `long Used/Total/Remaining` fields so no new numeric type is needed and math (percent, remaining) stays integer-safe.

`UsageItem.FormatDetail` and `Percent` branch on `Unit`:
- `Usd`: `"US$ {Used/100:N2} / US$ {Total/100:N2}（剩餘 US$ {Remaining/100:N2}）"`; `Percent` unchanged (`F0`%).
- `Request`: today's formatting.

Rationale: minimal diff; existing request providers set `Unit` implicitly. Alternative (decimal `decimal? AmountUsd` field) splits the model and complicates every display path.

### D3. Endpoint strategy — tolerant single fetch

The provider calls, in preference order, and tolerates absence of any of them:

1. `GET https://api.openai.com/v1/dashboard/billing/subscription` (Bearer `sk-...`) → `hard_limit_usd`, `access_until`.
2. `GET https://api.openai.com/v1/dashboard/billing/usage?start_date={1st of month}&end_date={today}` → `total_usage` (cents).
3. `GET https://api.openai.com/v1/dashboard/billing/credit_grants` → `total_granted` / `total_used` / `total_available`.

- If subscription+usage present → build `monthly-billing` meter.
- If credit_grants present → build `prepaid-credit` meter.
- If neither present → throw a descriptive error (UI shows stale/error state).

Hard-coded value over a `/v1/models`-style "fetch by key" check because the app's pattern is a fixed endpoint per provider. Alternative (key check via `GET /v1/models`) adds an extra round-trip and rate-limit surface without value.

### D4. Wiring follows the existing pattern

- `AppSettings.ChatGptApiKey` (obfuscated with `KeyObfuscator` like the others).
- `UsageService.ApplyApiKeys` switch gains a `ChatGptProvider => settings.ChatGptApiKey` case.
- `App.OnStartup` constructs `ChatGptProvider` and passes it to `UsageService`.
- `UsageCatalog.All` gains the two meters.
- Settings window adds a `ChatGptKeyBox` PasswordBox labeled with how to find the key (platform.openai.com → API keys).

### D5. Auth is a plain OpenAI API key

`sk-...` key used as `Authorization: Bearer`. Same 401/403 → "key invalid" error mapping as the other providers.

## Risks / Trade-offs

- **[OpenAI billing endpoints are undocumented/unstable]** → Parse leniently with the same fallback-field helpers used by the other providers; treat missing fields per-meter as "skip"; never crash the poll loop.
- **[Hard limit semantics vary (some accounts have no hard limit)]** → When `hard_limit_usd` is absent, absolute usage (`soft_limit`) becomes `Total=0` → UI shows usage percent only / "無限額度".
- **[`total_usage` units ambiguity (cents vs dollars)]** → Normalize to cents explicitly in one place (`ToCents`), documented in the provider header; `credit_grants` already returns dollars.
- **[ChatGPT plans that aren't API-billed return useless data]** → Such accounts show the standard error/stale state rather than crashing; message hints at checking the OpenAI platform billing page.

## Migration Plan

Settings are additive (`ChatGptApiKey` default empty string), so existing `settings.json` loads unchanged. No schema migration. No dependencies or deployment steps. Rollback = remove the provider construction line in `App.OnStartup` and the settings field.

## Open Questions

- Whether to treat `access_until` (subscription renewal) or month-end as the reset time for `monthly-billing` — **Decision**: use `access_until` when present, else month-end of the current billing month.
- Endpoint availability for free-tier/ChatGPT-Plus-without-API usage — acceptable to show error state until a paid API account is used.