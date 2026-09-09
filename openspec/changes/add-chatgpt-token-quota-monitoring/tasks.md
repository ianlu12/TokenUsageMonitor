## 1. Model & Settings

- [x] 1.1 於 `Models/AppSettings.cs` 新增 `ChatGptConsumerToken`（ChatGPT session access token，將與其他 key 一樣以 XOR 混淆儲存）與 `ChatGptConsumerAccountId`（選填）屬性，預設空字串
- [x] 1.2 於 `Models/UsageCatalog.cs` 新增 `chatgpt-consumer` provider 的兩個 meter：`five-hour`（5 小時滾動）、`weekly`（每週），Key 格式沿用 `{providerId}:{meterId}`

## 2. Provider Implementation

- [x] 2.1 新增 `Services/ChatGptConsumerProvider.cs`（實作 `IGptProvider`）：`ProviderId = "chatgpt-consumer"`、`UsageUrl = "https://chatgpt.com/"`、MeterIds 為 `five-hour`/`weekly`，`SetApiKey` 依共用的「token（含 account id）」語意設定認證欄位
- [x] 2.2 實作 `FetchUsageAsync`：對 `https://chatgpt.com/backend-api/wham/usage` 發送 GET，帶 `Authorization: Bearer {token}`、`Accept: application/json`、瀏覽器風格的 `User-Agent`/`Referer: https://chatgpt.com/`/`Origin`；若設定 account id 則帶 `ChatGPT-Account-Id` header
- [x] 2.3 解析 `rate_limit.primary_window`（5 小時）與 `rate_limit.secondary_window`（每週）：讀 `used_percent`、`reset_at`（Unix epoch 秒→UTC）、`limit_window_seconds`；以多候選欄位名稱彈性解析，`secondary_window` 缺漏時只回報 `five-hour`
- [x] 2.4 將每個視窗映射為 `TokenUsage`：`Used = 0`、`Total = 0`、`UsedPercent` 限制在 0–100（小數以整數顯示）、`ResetAt` 由 `reset_at` 換算、`IsUnlimited = false`、`Unit = Request`、`Status = Ok`
- [x] 2.5 錯誤處理：401 →「ChatGPT 訂閱 access token 已過期或無效，請重新設定（含 account id 提示）」；403 →「無權限存取額度（可能為免費版）」；其他非 2xx → HTTP 錯誤；回應缺 `rate_limit` → 明確解析錯誤訊息；全部拋出 `InvalidOperationException` 讓 `UsageService` 保留上次資料
- [x] 2.6 方案名稱解析（best-effort）：呼叫 `https://chatgpt.com/backend-api/accounts/check/v4-2023-04-27` 讀 `consumer_plan.display_name`，失敗時以 `wham/usage` 的 `plan_type` 對映（free/plus/pro 等），最糟回退「ChatGPT 訂閱」；此錯誤不得中斷額度資料顯示

## 3. Composition & Wiring

- [x] 3.1 `UsageService.ApplyApiKeys`：新增 switch 分支，將 `settings.ChatGptConsumerToken`（與選填 account id）餵給 `ChatGptConsumerProvider`
- [x] 3.2 `App.xaml.cs`：組合根以 `KeyObfuscator.Deobfuscate` 解開設定值並建立 `ChatGptConsumerProvider`，加入 `UsageService` providers 清單

## 4. Settings UI

- [x] 4.1 `Views/SettingsWindow.xaml`：於 API Key 群組新增 ChatGPT 訂閱欄位（access token + 選填 account id），附說明文字（如何從瀏覽器 DevTools 或 `~/.codex/auth.json` 取得）
- [x] 4.2 `Views/SettingsWindow.xaml.cs`：載入與儲存 `ChatGptConsumerToken`/`ChatGptConsumerAccountId`（Obfuscate/Deobfuscate）

## 5. Verification

- [x] 5.1 `dotnet build TokenUsageMonitor.sln` 通過（Release 組態無錯誤無警告）
- [ ] 5.2 手動驗證：設定 token 後，懸浮窗/托盤可選取並顯示「ChatGPT 訂閱 · 5 小時滾動」與「每週」的百分比與重置時間
- [ ] 5.3 手動驗證錯誤路徑：未設定 → 顯示未設定狀態；無效 token → 顯示錯誤狀態並保留其他 provider 資料