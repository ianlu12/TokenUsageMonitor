## ADDED Requirements

### Requirement: 儲存與載入設定
系統 SHALL 將設定資料持久化儲存於使用者專屬目錄（`%APPDATA%`）中的 JSON 檔案，並於應用程式啟動時載入。

#### Scenario: 應用程式啟動讀取設定
- **WHEN** 應用程式啟動
- **THEN** 系統從設定檔載入 API Key、選定模型、懸浮窗位置、顯示狀態與開機自動啟動設定

#### Scenario: 設定檔不存在
- **WHEN** 使用者首次執行應用程式且設定檔不存在
- **THEN** 系統使用預設設定（懸浮窗顯示於右下角、輪詢間隔 30 秒）並等待使用者設定 API Key

#### Scenario: 儲存設定
- **WHEN** 使用者在設定視窗或選單中變更設定
- **THEN** 系統立即將更新後的設定寫入設定檔

### Requirement: 設定 API Key
系統 SHALL 提供設定視窗讓使用者輸入並儲存 GitHub Copilot 與 OpenCode Go 的 API Key。

#### Scenario: 設定 GitHub Copilot API Key
- **WHEN** 使用者在設定視窗輸入 GitHub Copilot API Key 並按下儲存
- **THEN** 系統儲存 API Key 並立即查詢該服務的使用量

#### Scenario: 設定 OpenCode Go API Key
- **WHEN** 使用者在設定視窗輸入 OpenCode Go API Key 並按下儲存
- **THEN** 系統儲存 API Key 並立即查詢該服務的使用量

#### Scenario: 清空 API Key
- **WHEN** 使用者清空 API Key 欄位並儲存
- **THEN** 系統移除已儲存的 API Key，該服務停止被輪詢

### Requirement: 選擇顯示模型
系統 SHALL 允許使用者從已設定的服務中選擇目前要在懸浮窗與托盤圖示顯示的模型。

#### Scenario: 選擇模型
- **WHEN** 使用者在設定視窗中選擇特定的模型
- **THEN** 懸浮窗與托盤圖示即時切換顯示該模型的使用量

#### Scenario: 目前模型服務的 API Key 被移除
- **WHEN** 使用者移除目前選定模型所屬服務的 API Key
- **THEN** 系統自動切換至另一個已設定的服務，若無其他服務則顯示未設定的狀態

### Requirement: 管理開機自動啟動
系統 SHALL 允許使用者啟用或停用開機自動啟動，並在啟動資料夾中建立或移除快捷方式。

#### Scenario: 啟用開機自動啟動
- **WHEN** 使用者在托盤選單啟用開機自動啟動
- **THEN** 系統在 Windows 啟動資料夾建立應用程式快捷方式，並於下次登入時自動啟動

#### Scenario: 停用開機自動啟動
- **WHEN** 使用者在托盤選單停用開機自動啟動
- **THEN** 系統從啟動資料夾移除應用程式快捷方式

#### Scenario: API Key 未設定時自動啟動
- **WHEN** 應用程式於開機自動啟動且 API Key 未設定
- **THEN** 應用程式正常啟動但進入待設定狀態，不顯示錯誤對話框