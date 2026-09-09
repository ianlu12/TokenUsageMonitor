## 1. 專案初始化

- [x] 1.1 建立 WPF 專案結構（.NET 8），設定解決方案與專案名稱（如 TokenUsageMonitor）
- [x] 1.2 新增專案相依性（如 Newtonsoft.Json 或啟用 System.Text.Json、WPF 相關套件）
- [x] 1.3 設定單一執行個體限制（使用 Mutex 避免多開）

## 2. 設定管理

- [x] 2.1 定義 AppSettings 資料模型（API Key、選定模型、輪詢間隔、懸浮窗位置與顯示狀態、開機自動啟動）
- [x] 2.2 實作 SettingsService：讀取/寫入 `%APPDATA%\TokenUsageMonitor\settings.json`
- [x] 2.3 實作預設設定初始化（懸浮窗顯示於右下角、輪詢間隔 30 秒）
- [x] 2.4 實作設定視窗（SettingsWindow）：API Key 輸入欄位、模型選擇下拉清單、儲存與取消按鈕
- [x] 2.5 API Key 以可逆混淆方式存入設定檔（不採明文，並標示此為已知限制）

## 3. API 資料查詢層

- [x] 3.1 定義標準化 TokenUsage 資料模型（服務名稱、模型名稱、已使用量、總額度、剩餘百分比、重置時間、狀態）
- [x] 3.2 實作 IGptProvider 介面（查詢使用量與重置時間、設定向每項服務的 API Key）
- [x] 3.3 實作 GitHub Copilot API 查詢服務（包含 endpoint、錯誤處理、API Key 驗證）
- [x] 3.4 實作 OpenCode Go API 查詢服務（包含 endpoint、錯誤處理、API Key 驗證）
- [x] 3.5 組建 UsageService：管理所有 Provider 的輪詢、將各 Provider 資料轉換為標準化 TokenUsage
- [x] 3.6 實作以 `DispatcherTimer` 為基礎的輪詢機制（預設間隔 30 秒，失敗時自動重試）
- [x] 3.7 處理 HTTP 錯誤：保留上次成功資料、標示錯誤狀態、於下次輪詢時重試

## 4. 系統托盤整合

- [x] 4.1 建立托盤圖示（NotifyIcon）並設定基本屬性
- [x] 4.2 實作自繪 Icon 產生器：依選定模型的使用量百分比生成顯示數字的 Bitmap 圖示
- [x] 4.3 實作托盤圖示更新機制（使用量更新時重新繪製 icon 並更新 tooltip）
- [x] 4.4 實作托盤右鍵選單：顯示兩服務的完整使用額度與重置時間資訊（標題為唯讀文字項目）
- [x] 4.5 右鍵選單加入「設定 API Key」選項（開啟設定視窗）
- [x] 4.6 右鍵選單加入「顯示懸浮窗」選項（勾選切換懸浮窗顯示狀態）
- [x] 4.7 右鍵選單加入「開機自動啟動」選項（勾選切換自動啟動狀態）
- [x] 4.8 右鍵選單加入「手動重新整理」與「退出」選項

## 5. 懸浮窗

- [x] 5.1 建立 OverlayWindow：無邊框、透明背景、置頂（WindowStyle=None、AllowsTransparency=True、Topmost=True）
- [x] 5.2 實作懸浮窗 UI：顯示服務名稱、模型名稱、使用量百分比與剩餘額度
- [x] 5.3 設定懸浮窗預設位置在桌面右下角（計算工作區尺寸並保留邊距）
- [x] 5.4 實作滑鼠拖曳移動（追蹤 MouseDown/MouseMove/MouseUp 事件計算位移）
- [x] 5.5 儲存拖曳後的位置至設定檔，並於下次啟動時恢復

## 6. 開機自動啟動

- [x] 6.1 實作 StartupManager：在 `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup` 建立快捷方式
- [x] 6.2 實作檢查與移除快捷方式功能（停用自動啟動時移除）
- [x] 6.3 啟動時讀取自動啟動設定並同步選單勾選狀態

## 7. 系統整合與驗證

- [x] 7.1 建立單一 ViewModel（或 StateManager）統一管理選定模型、使用量資料與 UI 通知
- [x] 7.2 實作應用程式退出流程（停止計時器、清理托盤圖示、儲存設定）
- [x] 7.3 啟動時若未設定 API Key，進入待設定狀態而不彈出錯誤對話框
- [x] 7.4 建立單一執行個體（Mutex）限制，防止重複開啟
- [ ] 7.5 手動測試：設定 API Key、切換模型、拖曳懸浮窗、切換開機自動啟動、退出應用程式等場景