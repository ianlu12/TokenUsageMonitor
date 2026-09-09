## MODIFIED Requirements

### Requirement: 解析與標準化使用量資料

系統 SHALL 將 GitHub Copilot、OpenCode Go、ChatGPT / OpenAI API 與 ChatGPT 消費版訂閱的 API 回應統一轉換為標準化的使用量模型，包含服務名稱、模型名稱、已使用量、總額度、剩餘百分比、重置時間與用量單位（請求數或 USD 金額）；對僅提供百分比與重置時間的訂閱視窗額度（如 ChatGPT 消費版的 5 小時 / 每週視窗），以百分比與重置時間標準化而不推算絕對數字；USD 金額以「分」為整數單位儲存，顯示時格式化為 `US$`。

#### Scenario: API 回應格式不同

- **WHEN** GitHub Copilot、OpenCode Go、ChatGPT API 與 ChatGPT 消費版訂閱回傳不同的 JSON 結構
- **THEN** 系統將四者的資料轉換為相同的內部模型，供托盤與懸浮窗統一顯示

#### Scenario: 用量單位為金額

- **WHEN** 標準化資料的用量單位為 USD（如 ChatGPT 額度）
- **THEN** 系統依金額格式顯示 `US$` 兩位小數，而非整數計數，且百分比計算與金額上限一致

#### Scenario: 用量單位為請求數

- **WHEN** 標準化資料的用量單位為請求數（如 GitHub Copilot 與 OpenCode Go）
- **THEN** 系統維持現有的整數「已使用 / 總額度（剩餘 X）」顯示格式

#### Scenario: 訂閱視窗額度僅含百分比與重置時間

- **WHEN** 標準化資料來自 ChatGPT 消費版訂閱視窗（如 5 小時或每週視窗），只提供已用百分比與重置時間而沒有絕對的已使用 / 總額度
- **THEN** 系統以「使用量 X%（時間 重置）」格式顯示，不推算虛假的絕對數字