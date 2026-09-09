# system-tray-integration Capability Specification

> Status: Done

## Requirement: 顯示系統托盤圖示
系統 SHALL 在 Windows 系統托盤（通知區域）顯示應用程式圖示，並在圖示中即時顯示「托盤顯示項目」所選定計數器的 token 使用量百分比（替代原本固定的選定模型）。

#### Scenario: 應用程式啟動時
- **WHEN** 應用程式啟動完成
- **THEN** 系統在系統托盤建立圖示，並顯示 TrayMeter 指定計數器的使用量百分比

#### Scenario: 托盤圖示更新
- **WHEN** 使用量資料更新
- **THEN** 系統重新繪製托盤圖示以顯示 TrayMeter 指定計數器最新的使用量百分比

#### Scenario: 圖示提示文字
- **WHEN** 滑鼠懸停在托盤圖示上
- **THEN** 系統顯示包含 TrayMeter 指定計數器的服務名稱、模型名稱與使用量百分比的提示文字

## Requirement: 托盤右鍵選單
系統 SHALL 在托盤圖示的右鍵選單中顯示各服務的完整使用額度資訊、重置時間、設定 API Key、控制懸浮窗與開機自動啟動、以及托盤顯示項目選擇等選項。

#### Scenario: 檢視詳細使用量
- **WHEN** 使用者對托盤圖示按滑鼠右鍵
- **THEN** 系統在選單上方顯示 GitHub Copilot 與 OpenCode Go 的已使用額度、總額度與重置時間

#### Scenario: 選擇托盤顯示項目
- **WHEN** 使用者展開「托盤顯示項目」子選單並選擇一個計數器
- **THEN** 系統更新 TrayMeter 設定，重繪托盤圖示並更新工具提示

#### Scenario: 開啟設定 API Key
- **WHEN** 使用者點選「設定 API Key」選單項目
- **THEN** 系統開啟設定視窗，提供輸入兩個服務的 API Key 欄位

#### Scenario: 切換懸浮窗顯示
- **WHEN** 使用者點選「顯示懸浮窗」選單項目
- **THEN** 系統切換懸浮窗的顯示/隱藏狀態，並更新選單項目以反映目前狀態

#### Scenario: 切換開機自動啟動
- **WHEN** 使用者點選「開機自動啟動」選單項目
- **THEN** 系統啟用或停用開機自動啟動功能，並更新選單項目的勾選狀態

#### Scenario: 退出應用程式
- **WHEN** 使用者點選「退出」選單項目
- **THEN** 系統停止所有計時器與輪詢並安全關閉應用程式