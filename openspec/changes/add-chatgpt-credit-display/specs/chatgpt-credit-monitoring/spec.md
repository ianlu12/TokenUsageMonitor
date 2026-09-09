## ADDED Requirements

### Requirement: 查詢 ChatGPT / OpenAI 帳單額度

系統 SHALL 透過 OpenAI 的 dashboard/billing API 查詢使用者的 ChatGPT / OpenAI 帳單額度，包括每月訂閱的已使用金額與上限（hard limit），以及預付額度（credit grants）的已授與、已使用與可用金額。

#### Scenario: 有設定 ChatGPT API Key

- **WHEN** 使用者已設定 ChatGPT API Key（`sk-...`）且應用程式啟動輪詢
- **THEN** 系統從 OpenAI billing API 取得並顯示每月已使用金額、上限與預付額度可用餘額

#### Scenario: ChatGPT API 請求失敗

- **WHEN** API 請求失敗（網路錯誤、逾時、限流、401 或 403）
- **THEN** 系統保留上次成功取得的資料並顯示錯誤狀態，且於下次輪詢時重試

#### Scenario: 未設定 ChatGPT API Key

- **WHEN** 使用者未設定 ChatGPT API Key
- **THEN** 系統對 ChatGPT 顯示未設定 API Key 的狀態，不嘗試發送請求

### Requirement: 以 USD 金額標準化額度資料

系統 SHALL 將 OpenAI billing API 回傳的金額（以美元計）統一轉換為標準化使用量模型，並以金額單位呈現；金額以「分」（cents）作為整數儲存與計算，顯示時換算為 `US$` 兩位小數格式。

#### Scenario: API 回傳金額

- **WHEN** OpenAI billing API 回傳 subscription 的 `hard_limit_usd` 與 usage 的 `total_usage`
- **THEN** 系統將兩者轉為以分為單位的 `Used`/`Total`，以 `UsedPercent` 計算已用百分比，並於介面以 `US$` 格式顯示

#### Scenario: 預付額度回應結構

- **WHEN** billing API 回傳 `credit_grants` 的 `total_granted` 與 `total_used`（或 `total_available`）
- **THEN** 系統顯示已用 / 授與金額與可用餘額，並以 `US$` 格式呈現

### Requirement: ChatGPT 額度可於介面選取顯示

系統 SHALL 將 ChatGPT 的額度 meter 註冊到使用量目錄（UsageCatalog），使使用者能在懸浮窗與托盤圖示中選擇顯示每月訂閱帳單與預付餘額任一項目，且以與其他服務一致的輪詢與錯誤處理機制運作。

#### Scenario: 選取 ChatGPT 額度 meter

- **WHEN** 使用者在設定視窗或托盤選單中選取 ChatGPT 額度項目
- **THEN** 懸浮窗與托盤圖示顯示該項目的已用/上限/剩餘 US$ 金額（依資料來源而定）與合適的百分比

#### Scenario: 金額為無限或無上限

- **WHEN** billing API 未回傳有效上限（如 subscription 無 hard limit）
- **THEN** 系統顯示已使用金額而不顯示百分比，並以「無限額度」或僅金額的方式呈現