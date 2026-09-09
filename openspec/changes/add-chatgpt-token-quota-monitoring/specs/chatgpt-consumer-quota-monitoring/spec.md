## ADDED Requirements

### Requirement: 查詢 ChatGPT 消費版訂閱額度

系統 SHALL 透過 ChatGPT 消費版 backend-api（`https://chatgpt.com/backend-api/wham/usage`）查詢使用者的訂閱額度，包括 5 小時滾動視窗（primary）與每週視窗（secondary）的已用百分比與重置時間；查詢時以 access token 作為 `Authorization: Bearer`，若設定 account id 則一併帶上 `ChatGPT-Account-Id` 標頭。

#### Scenario: 已設定 ChatGPT 訂閱 access token

- **WHEN** 使用者已設定 ChatGPT 訂閱 access token 且應用程式啟動輪詢
- **THEN** 系統取得並顯示 5 小時與每週視窗的已用百分比與重置時間

#### Scenario: 每週視窗缺漏

- **WHEN** `wham/usage` 回應只包含 primary 視窗而沒有 secondary 視窗
- **THEN** 系統只回報 5 小時視窗，不將每週視窗視為錯誤

#### Scenario: 未設定 ChatGPT 訂閱 access token

- **WHEN** 使用者未設定 ChatGPT 訂閱 access token
- **THEN** 系統對該 provider 顯示未設定狀態，不嘗試發送請求

#### Scenario: ChatGPT 訂閱 API 請求失敗

- **WHEN** API 請求失敗（401、403、網路錯誤、逾時或回應無法解析）
- **THEN** 系統保留上次成功取得的資料並顯示錯誤狀態與原因（401 提示 token 過期、403 提示無權限），且於下次輪詢時重試

### Requirement: 以百分比與重置時間標準化訂閱額度資料

系統 SHALL 將 ChatGPT 消費版的視窗額度轉換為標準化使用量模型，以已用百分比（0–100）與重置時間表示；此資料源不提供絕對的「已使用 / 總額度」數字，系統不得推算虛假的絕對數字。

#### Scenario: 標準化 5 小時視窗

- **WHEN** `rate_limit.primary_window` 回傳 `used_percent` 與 `reset_at`（Unix epoch 秒）
- **THEN** 系統產生對應 meter，`UsedPercent` 設為 `used_percent`、`ResetAt` 設為 `reset_at` 換算的 UTC 時間，並以「使用量 X% · 時間 重置」格式顯示

#### Scenario: 標準化每週視窗

- **WHEN** `rate_limit.secondary_window` 回傳 `used_percent` 與 `reset_at`
- **THEN** 系統產生對應 meter，比照 5 小時視窗的規則顯示百分比與重置時間

#### Scenario: 百分比為小數或超出範圍

- **WHEN** `used_percent` 為小數、超過 100 或低於 0
- **THEN** 系統將百分比限制在 0–100 之間並以整數百分比顯示

### Requirement: 解析訂閱方案作為顯示名稱

系統 SHALL 嘗試解析 ChatGPT 消費版訂閱方案（如 Plus / Pro）並反映在顯示的服務名稱上；方案解析失敗時回退為通用名稱「ChatGPT 訂閱」，不得影響額度資料的取得與顯示。

#### Scenario: 成功解析方案名稱

- **WHEN** `accounts/check` 或 `wham/usage` 回應提供可辨識的方案（如 `plus`、`pro`、`free`）
- **THEN** 系統以「ChatGPT Plus」這類帶方案的服務名稱顯示額度 meter

#### Scenario: 方案解析失敗

- **WHEN** 方案查詢失敗或回應缺少方案資訊
- **THEN** 系統仍取得並顯示額度資料，以「ChatGPT 訂閱」作為服務名稱

### Requirement: ChatGPT 訂閱額度可於介面選取顯示

系統 SHALL 將 ChatGPT 消費版的額度 meter（5 小時滾動、每週）註冊到使用量目錄（UsageCatalog），使使用者能在懸浮窗與托盤圖示中選取顯示任一項目，且以與其他服務一致的輪詢與錯誤處理機制運作。

#### Scenario: 選取 ChatGPT 訂閱額度 meter

- **WHEN** 使用者在設定視窗或懸浮窗選單中選取「ChatGPT 訂閱 · 5 小時滾動」或「ChatGPT 訂閱 · 每週」項目
- **THEN** 懸浮窗與托盤圖示顯示該項目的已用百分比與重置時間