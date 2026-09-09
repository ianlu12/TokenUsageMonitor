## Context

懸浮窗目前是純展示元件：只顯示單一 `Selected` 計數器（懸浮條固定 3 行：狀態點＋名稱、大字百分比、明細），僅支援左鍵拖曳移動，沒有右鍵選單。使用者若要查看其他計數器，只能在設定視窗或托盤選單間切換單一「選定計數器」。此外背景不透明度固定為十六進位顏色 `#E6252525`，無處調整。

托盤圖示（`TrayIconService`）同樣只顯示單一 `_state.Selected` 的百分比，並在工具提示顯示其名稱；該選擇固定在 AppState，無從托盤選單調整。此外應用程式目前沒有正式圖示（執行檔與視窗使用系統預設）。

本變更加入四組能力：(1) 懸浮條右鍵選單，多選要在懸浮條上顯示的計數器；(2) 調整懸浮條背景透明度；(3) 托盤圖示顯示項目改為可由托盤右鍵選單單選；(4) 應用程式正式圖示。皆需持久化到 `settings.json`。

## Goals / Non-Goals

**Goals:**
- 在懸浮條上提供右鍵 ContextMenu。
- 支援同時顯示多個已勾選的計數器（以多行/項列表呈現，每項顯示名稱＋百分比＋明細）。
- 提供背景透明度快速設定（含數個預設檔位）。
- 顯示項目與透明度寫入並讀回 `settings.json`，重啟後保留。
- 托盤圖示顯示的計數器可在托盤右鍵選單中單選，並持久化。
- 為應用程式提供正式圖示（.ico），應用於執行檔、視窗與托盤。
- 保留既有拖曳移動功能不受右鍵影響。

**Non-Goals:**
- 不新增新的計數器來源或 provider。
- 不重做設定視窗（SettingsWindow）既有功能；右鍵選單為獨立入口。
- 不引入第三方 UI 或彈出視窗主題。
- 不提供透明度滑桿微調（以預設檔位為主，介面簡潔）。
- 不改變托盤圖示的自我繪製機制（沿用 GDI+ 動態繪製百分比，僅改用正式圖示作為應用程式視窗/執行檔圖示、並保持托盤上的即時百分比呈現）。

## Decisions

### D1: 使用 WPF 原生 `ContextMenu` 而非自建快顯視窗
懸浮窗已使用 WPF 且 `AllowsTransparency=True`。WPF 內建 `ContextMenu` 不需額外相依、易於綁定與樣式化，且會自動在視窗外隨螢幕邊界定位。替代方案：自訂 `Popup` 需手動處理邊界與失焦關閉，成本高、收益低。
**決策**: 在 `OverlayWindow.xaml` 的 `RootBorder.ContextMenu` 上定義 MenuItem。

### D2: 以 `ObservableCollection<TokenUsage>` + `ItemsControl` 呈現多計數器
現行 `AppState.Selected`/`SelectedDisplay`/`SelectedPercent`/`SelectedDetail` 是單一值。改為由 `ItemsControl` 綁定到 `AppState` 提供的一個「可顯示計數器集合」，每個項目渲染名稱＋百分比＋明細（保留狀態點、大字百分比、明細的既有資訊密度）。
**決策**: 新增 `AppState.SelectedUsages`（`ObservableCollection<...>` 或 IReadOnlyList 於 `ApplyUsages` 重建並觸發 `OnPropertyChanged`），保留 `Selected` 供托盤/狀態點相容。懸浮窗尺寸由 `SizeToContent` 或 ItemsControl 調整。

### D3: 可見計數器以「meter key 集合」儲存
`AppSettings` 加入 `List<string> VisibleMeters`（如 `["github-copilot:premium_interactions", "opencode-go:monthly"]`）。`ApplyUsages` 依此過濾出可顯示項目；若集合為空，改用既有「未設定」佔位提示，不顯示空白條。
**決策**: 預設值清單僅含目前的 `SelectedMeter`，維持向後相容；沿用 `UsageCatalog` 的 `Meter.Key` 格式化。

### D4: 透明度以 `double OverlayOpacity`（0.25–1.0）與效率檔位
`AppSettings` 加入 `double OverlayOpacity = 1.0`。懸浮窗 `RootBorder` Background 以 `Opacity="Bind to OverlayOpacity"` 或背景色 alpha 動態組合套用。右鍵選單以 Radio 選項（100% / 75% / 50% / 25%）對應 1.0/0.75/0.5/0.25。
**決策**: 直接控制 `RootBorder.Opacity` 最簡單且視覺一致；設定經 `_settingsService.Save()` 持久化。

### D5: 右鍵選單項目結構與行為
選單包含：
- 「顯示項目」子選單：每個 `UsageCatalog.All` 計數器一個 `MenuItem`（IsCheckable），勾選寫入 `VisibleMeters`。
- 分隔線。
- 「背景透明度」子選單：4 個 Radio 選項。
- 分隔線。
- 「重新整理」：觸發 `ManualRefresh` 回呼（由 App 注入，與托盤一致）。
- 「隱藏懸浮窗」：觸發 `ToggleOverlay` 回呼（與托盤一致）。

事件在 code-behind `OverlayWindow.xaml.cs` 處理，透過委派注入（類似現有 `_persistPosition`），避免懸浮窗直接依賴組合根。拖曳保持綁定在左鍵；右鍵由 ContextMenu 接手不會觸發 DragMove。

### D6: 托盤顯示項目以 `TrayMeter`（單一 key）儲存並於托盤選單單選
托盤圖示一次只能呈現一個計數器（16x16 空間有限）。`AppSettings` 加入 `string TrayMeter`，預設值沿用目前的 `SelectedMeter`（向後相容）。托盤右鍵選單新增「托盤顯示項目」子選單，列出全部 `UsageCatalog.All` 計數器的 Radio（單選）選項；選擇後寫入 `TrayMeter`、儲存設定並重建選單、重繪圖示。
**決策**: 托盤繼續使用 GDI+ 動態繪製百分比（即時資料），而非固定圖示；`TrayMeter` 決定要呈現哪一個計數器。`UpdateIcon`/`UpdateTooltip` 由讀取 `_state.Selected` 改為依 `TrayMeter` 解析對應 `TokenUsage`。

### D7: 應用程式正式圖示（.ico）
新增一個多尺寸 `.ico`（例如 16/32/48/256）作為應用程式圖示，內容呼應既有托盤視覺（深色圓形 + 使用量百分比風格），由 AI 生成並放置於 `TokenUsageMonitor/Assets/App.ico`。引用方式：
- `.csproj`：加入 `<ApplicationIcon>Assets/App.ico</ApplicationIcon>`（執行檔圖示）與 `Resource`（內嵌資源）。
- 視窗 `Icon`：設定於 `OverlayWindow` 與 `SettingsWindow` 的 `Icon` 屬性。
- 托盤：保留即時百分比動態圖示（D6），但可用正式圖示色彩/風格統一視覺。
**決策**: 使用 256 支援高 DPI 的 ICO；托盤的即時百分比繪製沿用現有邏輯，僅風格上與正式圖示一致。

### D8: 托盤圖示顯示項目解析（AppState 共用）
在 `AppState` 新增輔助方法 `ResolveMeter(string key)`：依 `AllUsages` 與 key 解析對應 `TokenUsage?`（找不到時退回與 `ApplyUsages` 相同的選取邏輯，確保指標不為空）。`TrayIconService.UpdateIcon` 改用此解析結果；`Settings.TrayMeter` 由托盤選單變更，並於變更後觸發 `TrayDisplayChanged` 事件讓服務重繪。
**決策**: 解析集中於 AppState，避免 UI 層重複「key → usage」邏輯。

## Risks / Trade-offs

- **懸浮窗高度伸縮**：多計數器下視窗變高。→ 使用 `SizeToContent="Height"` 或動態調整 `Height`，並限制 `MaxHeight`；狀態點顏色改為依「整體最差狀態」或保留首項目。
- **透明度影響文字可讀性**：過低透明度使文字難讀。→ 檔位下限 25%，文字前景色保持不透明（僅背景透明）；需實測微調。
- **拖曳與右鍵衝突**：`MouseLeftButtonDown` 只觸發拖曳，右鍵預設不觸發；ContextMenu 設於 Border 上不會干擾 DragMove。→ 保留 `Focusable=False`，確保 ContextMenu 能開啟。
- **`VisibleMeters` 指向不存在的 key**（例如舊設定）→ `ApplyUsages` 過濾時忽略未知 key；空集合退回佔位。
- **`TrayMeter` 指向不存在的 key**（例如舊設定或 key 變更）→ `ResolveMeter` 退回既有選取邏輯，仍顯示一個有效計數器，不顯示空白。
- **透明度是非破壞性設定**（無遷移），`OverlayOpacity` 預設 1.0 對既有使用者無感。→ 無需資料遷移。
- **圖示生成品質**：AI 生成的 ICO 解析度不足或風格不符。→ 提供多尺寸；若不符可再迭代重新生成。

## Open Questions

- 多項與單項的視覺排版是否需截然不同的佈局，或共用每項的「名稱／百分比／明細」模板？（預設採共用模板、垂直堆疊）
- 托盤圖示在「未設定/錯誤」狀態下是否仍顯示代號（`?`）而非正式圖示？→ 預設保留現有行為（狀態代號），維持即時可用性。