## Context

使用者需要在 Windows 桌面環境下即時監控 GitHub Copilot 與 OpenCode Go 的 token 使用額度。目前沒有針對這兩種服務的整合式桌面監控工具。應用程式需要常駐背景、即時更新資料、並以兩種視覺方式（系統托盤圖示與懸浮窗）呈現使用量。

## Goals / Non-Goals

**Goals:**
- 輕量常駐的 Windows 桌面應用程式
- 同時支援 GitHub Copilot 與 OpenCode Go 的 API
- 即時顯示 token 使用量與重置時間
- 使用者可設定 API Key、選擇要顯示的模型
- 開機自動啟動

**Non-Goals:**
- 不包含 token 消耗歷史圖表分析
- 不支援非 Windows 平台
- 不實作多帳號切換
- 不做自動充值或購買額度

## Decisions

### 1. 使用 WPF（Windows Presentation Foundation）框架

**選擇**: WPF
**理由**: WPF 提供完善的托盤圖示（NotifyIcon）、無邊框透明視窗、拖曳事件處理，且對動態更新 UI 有良好的資料綁定支援。相較 WinForms 提供更佳的視覺客製化能力。
**替代方案**: WinForms（較簡單但視覺彈性差）、MAUI（跨平台但 Windows 桌面支援不完整）

### 2. 使用 NotifyIcon 搭配自繪圖示

**選擇**: 使用 `System.Windows.Forms.NotifyIcon` 並自訂 icon 繪製
**理由**: 托盤圖示需要即時顯示使用量，透過產生包含百分比數字的位圖（Bitmap）動態更新 icon。NotifyIcon 支援從 .NET 直接操作。
**替代方案**: 使用 WPF 社群套件（如 Hardcodet.NotifyIcon.Wpf），提供更完整的 WPF 整合。

### 3. 無邊框透明懸浮窗搭配手動拖曳

**選擇**: 繼承 `Window`，設定 `WindowStyle=None`、`AllowsTransparency=True`、`Topmost=True`，透過覆寫 `WM_MOUSEMOVE` 或監聽滑鼠事件實作拖曳
**理由**: 提供完全客製化的視覺外觀。拖曳透過欄位記錄起始位置與滑鼠位移來計算新位置。
**替代方案**: 使用 `System.Windows.Forms.Timer` 定期更新位置（不適合，因為需要真實滑鼠互動）

### 4. 使用 HttpClient 定時輪詢 API

**選擇**: 使用 `System.Net.Http.HttpClient` 搭配 `System.Threading.Timer` 或 `DispatcherTimer` 定期（如每 30 秒）查詢使用量
**理由**: API 沒有推送機制，輪詢是最簡單可靠的方式。設定合理的輪詢間隔避免 API 限流。
**替代方案**: SignalR / WebSocket 推送（兩家 API 皆不提供）

### 5. 設定儲存於 `%APPDATA%` 的 JSON 檔案

**選擇**: 使用 `System.Text.Json` 序列化設定資料到 `%APPDATA%\TokenUsageMonitor\settings.json`
**理由**: 輕量、無需額外資料庫依賴。API Key 與設定欄位簡單。
**替代方案**: Windows Registry（較難維護結構）、.NET User Secrets（僅供開發環境）

### 6. 開機自動啟動使用啟動資料夾快捷方式

**選擇**: 在 `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup` 建立 `.lnk` 快捷方式
**理由**: 用戶可在工作管理員中直接停用，容易管理，無需管理員權限。
**替代方案**: Registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`（需要安裝程式）。

## Risks / Trade-offs

- [API 限流或變更] → 設定合理的輪詢間隔、處理 HTTP 錯誤的回退邏輯
- [API Key 安全] → 儲存於使用者專屬目錄，避免明文儲存於程式目錄
- [托盤圖示更新效能] → 控制 icon 更新頻率，避免過度重繪
- [懸浮窗遮擋內容] → 提供使用者顯示/隱藏的開關
- [首版設定無加密] → 記錄於設計：API Key 以可逆混淆存放，明文標示是已知限制