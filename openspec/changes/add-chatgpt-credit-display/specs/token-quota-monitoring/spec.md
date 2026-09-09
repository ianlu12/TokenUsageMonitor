## MODIFIED Requirements

### Requirement: 解析與標準化使用量資料

系統 SHALL 將 GitHub Copilot、OpenCode Go 與 ChatGPT / OpenAI 的 API 回應統一轉換為標準化的使用量模型，包含服務名稱、模型名稱、已使用量、總額度、剩餘百分比、重置時間與用量單位（請求數或 USD 金額）；USD 金額以「分」為整數單位儲存，顯示時格式化為 `US$`。

#### Scenario: API 回應格式不同

- **WHEN** GitHub Copilot、OpenCode Go 與 ChatGPT 回傳不同的 JSON 結構
- **THEN** 系統將三者的資料轉換為相同的內部模型，供托盤與懸浮窗統一顯示

#### Scenario: 用量單位為金額

- **WHEN** 標準化資料的用量單位為 USD（如 ChatGPT 額度）
- **THEN** 系統依金額格式顯示 `US$` 兩位小數，而非整數計數，且百分比計算與金額上限一致

#### Scenario: 用量單位為請求數

- **WHEN** 標準化資料的用量單位為請求數（如 GitHub Copilot 與 OpenCode Go）
- **THEN** 系統維持現有的整數「已使用 / 總額度（剩餘 X）」顯示格式