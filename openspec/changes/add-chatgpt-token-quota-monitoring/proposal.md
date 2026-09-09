# Add ChatGPT Consumer Subscription Quota Monitoring

## Why

現有的 `ChatGptProvider` 只監控 OpenAI API 的**美元帳單 / 預付餘額**，無法反映 ChatGPT 消費版（Plus / Pro 訂閱）的實際使用額度。訂閱使用者在 5 小時與每週的滾動視窗額度用盡時，常被打斷工作流程；應用程式若能即時顯示這些額度，就能像監控 Copilot / OpenCode Go 一樣在額度將盡前提醒。

## What Changes

- 新增 provider **ChatGPT 訂閱（Plus/Pro）**（`chatgpt-consumer`），與既有 providers 並行輪詢。
- 使用 ChatGPT 消費版的 backend-api 資料源：
  - `GET https://chatgpt.com/backend-api/wham/usage` — 回傳 5 小時（primary）與每週（secondary）滾動視窗的 `used_percent`、`reset_at` 等額度資料。
  - `GET https://chatgpt.com/backend-api/accounts/check/v4-2023-04-27`（best-effort）— 解析訂閱方案名稱供顯示。
- 新增兩個額度 meter：
  - `five-hour`（5 小時滾動額度）
  - `weekly`（每週額度）
  - 各自以百分比 + 重置時間呈現（本資料源無絕對「已使用/總額度」數字）。
- 新增設定欄位：`ChatGptConsumerToken`（ChatGPT session 的 access token，與其他 key 一樣 XOR 混淆儲存）與選填的 `ChatGptConsumerAccountId`，並在設定視窗提供輸入欄位與取得方式說明。
- 在 `UsageCatalog` 註冊新 meters，使托盤與懸浮窗選單自動包含這些項目（不需逐檔案 UI 修改）。
- 沿用既有的未設定 / 錯誤處理機制：401 視為 token 過期、403 視為無權限、其餘失敗保留上次資料並標記錯誤，下次輪詢自動重試。

## Capabilities

### New Capabilities

- `chatgpt-consumer-quota-monitoring`: 以 ChatGPT 消費版 backend-api 查詢訂閱（Plus/Pro）的 5 小時與每週滾動額度、解析訂閱方案，並將資料標準化為共享使用量模型供托盤與懸浮窗顯示。

### Modified Capabilities

- `token-quota-monitoring`: 「解析與標準化使用量資料」需求延伸為也涵蓋「僅有百分比與重置時間、無絕對已用/總額度」的訂閱視窗額度（如 ChatGPT 消費版），以及依方案名稱顯示服務名稱。

## Impact

- **Services**: 新增 `ChatGptConsumerProvider`（實作 `IGptProvider`）；擴充 `UsageService.ApplyApiKeys` 的 switch、`App` 組合根與 `SettingsWindow` 欄位。
- **Models**: `AppSettings` 新增 `ChatGptConsumerToken` 與 `ChatGptConsumerAccountId`；`UsageCatalog` 新增兩個 meter。
- **State/UI**: 無共用模型欄位變動（百分比-only 的顯示路徑已存在）；設定視窗新增一組輸入欄位。
- **NuGet / dependencies**: 無新增（沿用 `HttpClient` + `System.Text.Json`）。