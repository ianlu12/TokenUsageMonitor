## Requirements

### Requirement: 查詢 GitHub Copilot token 使用量
系統 SHALL 透過 GitHub Copilot API 查詢使用者的 token 使用額度，包括已使用量、總額度、剩餘量與重置時間。

#### Scenario: 有設定 GitHub Copilot API Key
- **WHEN** 使用者已設定 GitHub Copilot API Key 且應用程式啟動輪詢
- **THEN** 系統從 GitHub Copilot API 取得並顯示 token 使用量、總額度與重置時間

#### Scenario: GitHub Copilot API 請求失敗
- **WHEN** API 請求失敗（網路錯誤、逾時或限流）
- **THEN** 系統保留上次成功取得的資料並顯示錯誤狀態，且於下次輪詢時重試

#### Scenario: 未設定 GitHub Copilot API Key
- **WHEN** 使用者未設定 GitHub Copilot API Key
- **THEN** 系統對 GitHub Copilot 顯示未設定 API Key 的狀態，不嘗試發送請求

### Requirement: 查詢 OpenCode Go token 使用量
系統 SHALL 透過 OpenCode Go API 查詢使用者的 token 使用額度，包括已使用量、總額度、剩餘量與重置時間。

#### Scenario: 有設定 OpenCode Go API Key
- **WHEN** 使用者已設定 OpenCode Go API Key 且應用程式啟動輪詢
- **THEN** 系統從 OpenCode Go API 取得並顯示 token 使用量、總額度與重置時間

#### Scenario: OpenCode Go API 請求失敗
- **WHEN** API 請求失敗（網路錯誤、逾時或限流）
- **THEN** 系統保留上次成功取得的資料並顯示錯誤狀態，且於下次輪詢時重試

#### Scenario: 未設定 OpenCode Go API Key
- **WHEN** 使用者未設定 OpenCode Go API Key
- **THEN** 系統對 OpenCode Go 顯示未設定 API Key 的狀態，不嘗試發送請求

### Requirement: 解析與標準化使用量資料
系統 SHALL 將 GitHub Copilot 與 OpenCode Go 的 API 回應統一轉換為標準化的使用量模型，包含服務名稱、模型名稱、已使用量、總額度、剩餘百分比與重置時間。

#### Scenario: API 回應格式不同
- **WHEN** GitHub Copilot 與 OpenCode Go 回傳不同的 JSON 結構
- **THEN** 系統將兩者的資料轉換為相同的內部模型，供托盤與懸浮窗統一顯示

### Requirement: 定期自動重新整理使用量
系統 SHALL 依設定的輪詢間隔（預設 30 秒）自動重新查詢所有已設定 API Key 的服務使用量。

#### Scenario: 到達輪詢間隔
- **WHEN** 距上次成功查詢已超過設定的輪詢間隔
- **THEN** 系統重新向各服務查詢使用量並更新顯示

#### Scenario: 手動重新整理
- **WHEN** 使用者在懸浮窗或托盤選單觸發手動重新整理
- **THEN** 系統立即重新查詢所有服務的使用量，不等待下一個輪詢間隔