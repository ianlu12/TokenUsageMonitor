## Why

懸浮條目前只能顯示單一選定的用量計數器，且沒有右鍵選單，使用者無法在桌面上直接調整顯示內容或外觀。托盤圖示顯示的項目也同樣固定為單一選定計數器，使用者希望能在托盤右鍵選單中自由選擇。此外應用程式目前沒有正式圖示。加入右鍵選單與圖示後，使用者可快速切換顯示項目、調整透明度並呈現一致的品牌形象。

## What Changes

- 在懸浮條上加入右鍵選單（Context Menu），取代目前唯一且容易誤觸的拖曳交互入口以外的空白區。
- 右鍵選單提供「顯示項目」區塊：列出所有可用用量計數器（GitHub Copilot 三個 + OpenCode Go 三個），以複選框勾選要同時顯示在懸浮條上的項目。
- 懸浮條從單一計數器顯示改為可同時顯示多個已勾選的計數器；未勾選任何項目時不予顯示或顯示佔位提示（避免空白懸浮條）。
- 右鍵選單提供「背景透明度」調整選項（例如 100% / 75% / 50% / 25%），即時套用於懸浮條背景。
- 顯示項目與透明度設定寫入 `settings.json`，應用程式重啟後保留。
- 選單中提供「重新整理」與「隱藏懸浮窗」快捷項目（保持與托盤選單行為一致）。
- 托盤圖示顯示的項目改為可在托盤圖示右鍵選單中選擇：新增「托盤顯示項目」子選單，列出所有計數器，以單選方式選擇托盤圖示與提示文字要呈現哪一個計數器。
- 為應用程式加入正式程式圖示（.ico），用於執行檔、視窗與托盤圖示。

## Capabilities

### New Capabilities
- `floating-bar-context-menu`: 懸浮條右鍵選單，提供顯示項目多選與背景透明度調整。
- `tray-display-selection`: 托盤圖示顯示項目選擇，於托盤右鍵選單中單選托盤圖示要呈現的計數器。
- `app-icon`: 應用程式正式圖示，應用於執行檔、視窗與托盤。

### Modified Capabilities
- `floating-overlay`: 懸浮窗由「單一模型」改為「可同時顯示多個勾選計數器」，並支援背景透明度設定。
- `system-tray-integration`: 托盤圖示顯示的計數器改為可由托盤右鍵選單選擇（替代原本固定的單一選定計數器）。

## Impact

- `TokenUsageMonitor/Views/OverlayWindow.xaml`：加入 `ContextMenu`、多計數器佈局（ItemsControl / StackPanel）、透明度樣式綁定。
- `TokenUsageMonitor/Views/OverlayWindow.xaml.cs`：右鍵選單事件處理、透明度套用、多項目明細刷新。
- `TokenUsageMonitor/State/AppState.cs`：由單一 `Selected` 改為支援多個可見計數器集合 + 托盤顯示計數器選擇，並暴露透明度屬性。
- `TokenUsageMonitor/Models/AppSettings.cs`：新增 `VisibleMeters`、`OverlayOpacity`、`TrayMeter`（托盤顯示計數器 key）欄位及預設值。
- `TokenUsageMonitor/Services/TrayIconService.cs`：新增「托盤顯示項目」子選單，依照所選計數器繪製托盤圖示；改用正式圖示資源。
- `TokenUsageMonitor/App.xaml.cs`：設定載入/儲存流程連接到新的選單、托盤顯示選擇與透明度設定。
- 新增 `.ico` 圖示資源檔（應用程式圖示），並在 .csproj / Window Icon / NotifyIcon 中引用。
- 沿用既有 `UsageCatalog`（6 個計數器）與 `ObservableObject` 通知機制，無新增外部相依。