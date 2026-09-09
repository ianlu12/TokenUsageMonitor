# tray-display-selection Capability Specification

> Status: Done

## Requirements

### Requirement: 托盤圖示顯示項目選擇
系統 SHALL 在托盤圖示右鍵選單中提供「托盤顯示項目」子選單，列出所有可用用量計數器（來自 UsageCatalog），以單選（Radio）方式選擇托盤圖示與工具提示要呈現哪一個計數器。

#### Scenario: 顯示托盤顯示項目選單
- **WHEN** 使用者對托盤圖示按滑鼠右鍵並展開「托盤顯示項目」子選單
- **THEN** 系統列出所有 UsageCatalog 計數器，目前作用中的計數器（TrayMeter）以勾選標示

#### Scenario: 選擇托盤顯示項目即時套用
- **WHEN** 使用者在托盤選單中選擇一個新的計數器
- **THEN** 系統立即將該計數器 key 寫入 TrayMeter，重繪托盤圖示以顯示新計數器的使用量百分比，並更新工具提示內容

#### Scenario: 托盤顯示項目持久化
- **WHEN** 使用者變更托盤顯示項目
- **THEN** 系統將 TrayMeter 寫入 settings.json，應用程式重啟後保留

#### Scenario: 托盤顯示項目預設值
- **WHEN** 應用程式首次啟動且 TrayMeter 尚無值
- **THEN** 系統預設使用目前的 SelectedMeter（與目前行為一致）