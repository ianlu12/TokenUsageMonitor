## 1. AppSettings — 新增持久化欄位

- [x] 1.1 在 `AppSettings.cs` 新增 `List<string> VisibleMeters` 欄位，預設值為空清單
- [x] 1.2 在 `AppSettings.cs` 新增 `double OverlayOpacity` 欄位，預設值為 `1.0`
- [x] 1.3 在 `AppSettings.cs` 新增 `string TrayMeter` 欄位，預設值為目前的 `SelectedMeter` 預設值
- [x] 1.4 確認 `SettingsService` 的 JSON 序列化/反序列化能正確處理新欄位（空清單向後相容）

## 2. AppState — 多計數器可見性狀態

- [x] 2.1 在 `AppState` 新增 `ObservableCollection<UsageItem> VisibleUsages` 屬性（公開唯讀，包裝 TokenUsage 以提供格式化綁定），作為懸浮窗的綁定來源
- [x] 2.2 在 `AppState` 新增 `Opacity` 屬性（double，ObservableObject 腳本）鏡像 `Settings.OverlayOpacity`，暴露給 OverlayWindow
- [x] 2.3 修改 `ApplyUsages` 方法：以 `Settings.VisibleMeters` 過濾 `AllUsages` 後重建 `VisibleUsages`；若清單為空則顯示預設（若 `VisibleMeters` 尚無值則預設含 `Settings.SelectedMeter`）
- [x] 2.4 在 `ApplyUsages` 中計算「最差狀態」供 `RefreshStatusColor` 使用，新增 `WorstStatus` 屬性（Ok / >=80% Orange / Error OrangeRed）
- [x] 2.5 新增 `RefreshVisibility()` 公開方法，允許外部位元使用者在 VisibleMeters 變更後呼叫（不觸發完整 ApplyUsages，僅重新過濾 VisibleUsages）
- [x] 2.6 新增 `event Action? VisibilityChanged` 事件，在 `VisibleUsages` 內容變更時觸發（供 OverlayWindow 重新調整高度）
- [x] 2.7 新增 `TrayDisplay` 屬性（`TokenUsage?`）鏡像 `Settings.TrayMeter`，並在 `ApplyUsages` 及鍵變更時重算
- [x] 2.8 新增 `ResolveMeter(string key)` 方法：依 `AllUsages` 與 key 解析對應 `TokenUsage?`，找不到時退回既有選取邏輯
- [x] 2.9 新增 `SetTrayMeter(string key)` 方法：更新 `Settings.TrayMeter`、重算 `TrayDisplay`、觸發 `TrayDisplayChanged` 事件

## 3. OverlayWindow — XAML 佈局與 ContextMenu

- [x] 3.1 將 `OverlayWindow.xaml` 原本的固定尺寸（Width/Height）改為 `SizeToContent="Height"` 並設定 `MinWidth="270"`，支援內容動態伸縮
- [x] 3.2 將既有的內層 `StackPanel` 改為 `ItemsControl`，`ItemsSource` 綁定 `VisibleUsages`，每個項目使用 DataTemplate 渲染：狀態點＋名稱、大字百分比、明細（沿用現有字型樣式）
- [x] 3.3 新增「無項目」佔位 `DataTrigger`：當 `VisibleUsages` 為空時顯示「未設定」樣式區塊
- [x] 3.4 在 `RootBorder.ContextMenu` 定義 ContextMenu 結構：
  - 「顯示項目」子選單（MenuItem）：以 `UsageCatalog.All` 動態產生 IsCheckable MenuItem
  - 分隔線
  - 「背景透明度」子選單（MenuItem）：4 個 Radio 選項（100%/75%/50%/25%），綁定 IsChecked 到 `OverlayOpacity`
  - 分隔線
  - 「重新整理」MenuItem（將 Click 繫接到 `ManualRefresh` 回呼）
  - 「隱藏懸浮窗」MenuItem（將 Click 繫接到 `ToggleOverlay` 回呼）
- [x] 3.5 設定 `RootBorder.Opacity` 綁定到 `AppState.Opacity`，實現透明度即時套用（以 `OpacityToBackgroundConverter` 僅調整背景 alpha，文字保持不透明）

## 4. OverlayWindow.xaml.cs — 右鍵選單事件處理

- [x] 4.1 在建構子增加回呼參數：`Func<Task> manualRefresh` 與 `Action toggleOverlay`
- [x] 4.2 新增 `OnDisplayItemChecked` 事件處理：將勾選的計數器 Key 加入 `Settings.VisibleMeters`，呼叫 `RefreshVisibility()`，儲存設定
- [x] 4.3 新增 `OnDisplayItemUnchecked` 事件處理：將取消勾選的計數器 Key 從 `Settings.VisibleMeters` 移除，呼叫 `RefreshVisibility()`，儲存設定
- [x] 4.4 新增 `OnOpacityChanged` 事件處理：將對應的透明度值寫入 `Settings.OverlayOpacity`，呼叫 `Settings.Save()` 並觸發 `OnPropertyChanged(nameof(Opacity))`
- [x] 4.5 修改 `RefreshStatusColor`：讀取 `AppState.WorstStatus` 而非單一 `Selected`，更新 StatusDot 顏色
- [x] 4.6 為 ContextMenu 的「顯示項目」MenuItem 加入初始化邏輯：啟動時根據 `Settings.VisibleMeters` 設定各項 IsChecked 狀態

## 5. TrayIconService — 托盤顯示項目選擇

- [x] 5.1 修改 `UpdateIcon`/`UpdateTooltip`：由讀取 `_state.Selected` 改為讀取 `_state.TrayDisplay`（依 TrayMeter 解析）
- [x] 5.2 在 `RebuildMenu` 中新增「托盤顯示項目」子選單（ToolStripMenuItem DropDown）：列出 `UsageCatalog.All` 全部計數器，以 Radio（Checked 單選）呈現，目前被選中者 Checked
- [x] 5.3 新增托盤選單選擇事件處理：呼叫 `_state.SetTrayMeter(key)`、儲存設定、重建選單並重繪圖示
- [x] 5.4 訂閱 `_state.TrayDisplayChanged` 事件：觸發 `UpdateIcon` 與 `UpdateTooltip`
- [x] 5.5 確認托盤選單在功能區上方（詳細使用量之後）插入「托盤顯示項目」子選單位置

## 6. 應用程式圖示（App Icon）

- [x] 6.1 生成多尺寸 `.ico` 圖示檔（16/32/48/256），深色圓形百分比風格，存放至 `TokenUsageMonitor/Assets/App.ico`
- [x] 6.2 在 `.csproj` 加入 `<ApplicationIcon>Assets/App.ico</ApplicationIcon>` 並以 Resource 內嵌
- [x] 6.3 設定 `OverlayWindow` 與 `SettingsWindow` 的 `Icon` 屬性為新圖示
- [x] 6.4 調整托盤動態圖示色彩/風格與新圖示一致（保留即時百分比繪製邏輯）

## 7. App.xaml.cs — 組合根連接

- [x] 7.1 修改 `OverlayWindow` 建構式呼叫，注入 `manualRefresh: SafeRefreshAsync` 與 `toggleOverlay: ToggleOverlay` 回呼
- [x] 7.2 在 `_state.UsagesChanged` 事件中除了呼叫 `RefreshStatusColor` 也呼叫 `RefreshVisibility`，確保用量更新時 VisibleUsages 同步（並以 `_tray.RefreshData()` 輕量更新托盤圖示/工具提示，避免每輪重整開啟中的選單）

## 8. 測試與驗證

- [x] 8.1 編譯通過：執行 `dotnet build` 無錯誤無警告（於 Linux 交叉編譯 net8.0-windows 驗證，0 Warning / 0 Error）
- [ ] 8.2 手動測試：懸浮條右鍵選單能正確開啟，顯示項目子選單列舉全部 6 個計數器
- [ ] 8.3 手動測試：勾選多個計數器後懸浮條高度動態伸縮，顯示對應卡片
- [ ] 8.4 手動測試：取消全部計數器後懸浮條顯示「未設定」佔位
- [ ] 8.5 手動測試：透明度各檔位切換即時生效，重啟後保留
- [ ] 8.6 手動測試：「重新整理」與「隱藏懸浮窗」功能正常，托盤同步
- [ ] 8.7 手動測試：托盤右鍵選單「托盤顯示項目」能單選，選擇後托盤圖示與工具提示立即更新
- [ ] 8.8 手動測試：托盤顯示項目選擇重啟後保留
- [ ] 8.9 手動測試：執行檔與視窗顯示正式圖示