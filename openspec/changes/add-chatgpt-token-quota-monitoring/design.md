# Design: ChatGPT Consumer Subscription Quota Monitoring

## Context

`TokenUsageMonitor` 目前輪詢三個 provider（`GitHubCopilotProvider`、`OpenCodeGoProvider`、`ChatGptProvider`），全部都實作 `IGptProvider` 並產出標準化的 `TokenUsage`（`Used`/`Total`/`UsedPercent`/`ResetAt` 等）供托盤與懸浮窗顯示。`OpenCodeGoProvider` 已經示範了「只有百分比、沒有絕對數字」的 meter 可以正常顯示（`Total <= 0` 時顯示「使用量 X%」）。

既有的 `ChatGptProvider` 走 `api.openai.com` 的 billing API，auth 是 `sk-...` API key，回報的是美元帳單與預付餘額。使用者要新增的是 **ChatGPT 消費版（Plus/Pro）訂閱**的額度：ChatGPT 網頁/用戶端把額度分成 5 小時滾動視窗與每週視窗，資料源是 `chatgpt.com/backend-api`，auth 是 ChatGPT 登入 session 的 access token（`Authorization: Bearer`），兩套 API 的 host、auth、回應語意完全不同。

經研究確認的資料源（Codex CLI 與多個開源專案共用同一端點）：

```http
GET https://chatgpt.com/backend-api/wham/usage
Authorization: Bearer <access_token>
ChatGPT-Account-Id: <account_id>          # 選填，多帳號或缺省時建議帶上
```

回應重點欄位：

```json
{
  "plan_type": "plus",
  "rate_limit": {
    "primary_window":   { "used_percent": 55, "limit_window_seconds": 18000,  "reset_after_seconds": 2547, "reset_at": 1778670307 },
    "secondary_window": { "used_percent": 51, "limit_window_seconds": 604800, "reset_after_seconds": 489405, "reset_at": 1779157165 }
  },
  "rate_limit_reset_credits": { "available_count": 2 }
}
```

`primary_window` ＝ 5 小時滾動視窗（`limit_window_seconds` ≈ 18000），`secondary_window` ＝ 每週視窗（≈ 604800）。`reset_at` 是 Unix epoch 秒。另可呼叫 `GET /backend-api/accounts/check/v4-2023-04-27`（best-effort）取得訂閱方案（`consumer_plan.display_name`、`subscription_expires_at`）。

## Goals / Non-Goals

**Goals:**

- 新增 provider `ChatGptConsumerProvider`（id `chatgpt-consumer`），輪詢 `wham/usage` 的 5 小時與每週視窗。
- 把要帶的認證（access token + 選填 account id）寫入 `AppSettings`（XOR 混淆，與既有 key 一致），並在設定視窗提供輸入欄位與取得方式說明。
- 標準化為百分比 + 重置時間的 `TokenUsage`，沿用現有顯示路徑（「使用量 X% · yyyy-MM-dd HH:mm 重置」）。
- 解析方案名稱（如 Plus/Pro）並反映在顯示的服務名稱上（best-effort）。
- 沿用既有的未設定 / 錯誤 / 保留上次資料機制。

**Non-Goals:**

- 不實作 ChatGPT 的登入流程（NextAuth session-token cookie → `/api/auth/session` → access token 的自動換發）。token 過期由使用者重新貼上。
- 不從 `~/.codex/auth.json` 自動匯入（僅在設定介面的說明文字中提示可從該檔案複製 token / account id）。
- 不顯示 `additional_rate_limits`（個別模型額度）與 `rate_limit_reset_credits`（重置券）。
- 不計算絕對 token 數目——`wham/usage` 只回傳百分比，刻意不做推估。

## Decisions

### D1. 新 provider，而非擴充 `ChatGptProvider`

`ChatGptProvider` 處理的是 API-key billing（`api.openai.com`），新功能是 ChatGPT session 訂閱（`chatgpt.com/backend-api`），auth、host、語意、錯誤訊息都不同。合併會讓設定欄位與錯誤處理互相干擾（同一 provider 有「API key 尚未設定」又出現「token 過期」兩種狀態）。採納 `UsageService` 既有的多 provider 模型，新增獨立 `ChatGptConsumerProvider`。
*替代方案*：塞進 `ChatGptProvider` —— 拒絕，原因是認證模型綁死在同一欄位，且 401/403 的錯誤文案會含糊。

### D2. 資料源：`wham/usage`（主要）+ `accounts/check/v4`（best-effort）

`wham/usage` 是唯一會回傳百分比視窗的端點。`accounts/check/v4-2023-04-27` 用於解析 `consumer_plan.display_name`（如 "ChatGPT Plus"）供顯示；此呼叫失敗時不影響額度資料，改用 `wham/usage` 的 `plan_type` 對映，最糟退回泛用名稱「ChatGPT 訂閱」。
*替代方案*：只用 `wham/usage` —— 少了方案名稱；改用其他網頁端點（如 `/backend-api/limits`）—— 文件化程度低、不穩定。

### D3. 認證：access token 手動貼上 + 選填 account id

Access token 是 JWT（約 10 天效期），直接以 `Authorization: Bearer` 送出；`ChatGPT-Account-Id` 頭在部分帳號是必要條件，故提供選填欄位。設定視窗的說明文字會指引從瀏覽器 DevTools（`Application → Cookies` 之外，`Network` 內任一 backend-api 請求的 Authorization 頭）或 `~/.codex/auth.json` 複製。
*替代方案*：完整 NextAuth 登入換 token —— 脆弱且複雜，列為 non-goal。

### D4. 視窗額度的模型化：百分比 + 重置時間，不推估絕對數字

`primary_window`/`secondary_window` 只提供 `used_percent` 與 `reset_at`。映射為 `TokenUsage { Used = 0, Total = 0, UsedPercent = used_percent, ResetAt = unix→UTC, IsUnlimited = false, Unit = Request }`。既有 `UsageItem.FormatAmount` 對 `Total <= 0` 的鉤子已輸出「使用量 X%」，`FormatDetail` 再附加重置時間，因此**無需更動 `TokenUsage` 模型或顯示程式碼**。
*替代方案*：用百分比反推假 `Used`/`Total` —— 拒絕，會誤導使用者。

### D5. 彈性解析以耐受 schema 漂移

端點非正式文件化，欄位可能改名或省略。採用與其他 provider 相同的「多候選欄位名」解析（如 `used_percent`/`usage_percent`），`secondary_window` 缺漏時只回報 `five-hour`，`reset_at` 缺漏則留 `null`。`limit_window_seconds` 不參與收入模型（僅供除錯）。

### D6. 錯誤語意

- 401 → 「ChatGPT 訂閱 token 已過期或無效，請重新設定」（提示重新貼上）。
- 403 → 「此帳號無法存取額度資料（免費版或無權限）」。
- 其他非 2xx → HTTP 錯誤；解析失敗 → 明確的「回應缺少 rate_limit 欄位」訊息。
- 上述一律讓 `UsageService` 保留上次成功資料並標 `MeterStatus.Error`，下次 tick 自動重試——與既有 provider 一致。

## Risks / Trade-offs

- **端點非正式且可能異動** → 多候選欄位名稱的彈性解析 + `UsageService` 的 keep-last-good；若端點永久下線，provider 只會顯示錯誤而不會崩潰。
- **Access token 效期短（約 10 天）** → 401 錯誤訊息明確指引重新取得；設定介面附取得步驟說明。
- **Cloudflare / bot 偵測可能 403** → 送出接近瀏覽器的 headers（`User-Agent`、`Referer`、`Origin`、`Accept`）；若仍被擋，error 狀態會呈現於介面，讓使用者知道需在瀏覽器端重新登入。
- **部分帳號缺少 `ChatGPT-Account-Id` 會 401** → 提供選填欄位並在初次 401 的錯誤訊息一併提示。
- **免費版帳號無額度視窗** → 403 或無 `rate_limit`，顯示對應錯誤文案，不當成 crash。
- **新增設定欄位對舊版 `settings.json` 相容** → 新欄位 camelCase、預設空字串，反序列化自動補預設值，無需遷移。

## Migration Plan

純新增（additive）：`AppSettings` 新增兩個欄位，預設空字串；`App` 組合根新增 provider 註冊；`UsageCatalog` 新增 meters。既有使用者升級後新 provider 顯示「尚未設定」狀態，不影響其他 provider。回滾方式：移除 provider 註冊與設定欄位即可，無資料結構變更。

## Open Questions

- `accounts/check/v4` 可能需要 `timezone_offset_min` query parameter；實作時若 200 但缺 `consumer_plan`，則省略此呼叫，改以 `plan_type` 對映（不阻塞）。
- 是否需要把訂閱到期日（`subscription_expires_at`）顯示出來？目前 `ResetAt` 語意是「視窗重置」而非「訂閱到期」，本變更不混用；如需顯示到期日屬後續增強。