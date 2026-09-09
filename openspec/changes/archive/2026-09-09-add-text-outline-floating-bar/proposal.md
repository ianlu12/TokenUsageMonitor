## Why

懸浮窗的文字在淺色或高亮度背景上顯示時可讀性不足，且當懸浮窗背景透明度調低、底下有淺色視窗時，文字容易與背景混淆而難以閱讀。

## What Changes

- 為懸浮窗內所有文字（狀態點旁的服務/模型名稱、百分比數字、明細文字、以及「未設定」佔位文字）加上黑色描邊（outline/stroke）
- 描邊作為視覺優化，不影響既有文字內容、字型大小、顏色或佈局
- 保持既有設計：文字前景仍維持完全不透明（不受不透明度滑桿影響），描邊進一步強化淺色背景下的對比與可讀性

## Capabilities

### New Capabilities
<!-- 無新增能力 -->

### Modified Capabilities
- `floating-overlay`: 新增需求，懸浮窗內所有文字需具備黑色描邊以提升在任何背景下的可讀性

## Impact

- 受影響檔案：`TokenUsageMonitor/Views/OverlayWindow.xaml`
- 僅涉及 WPF XAML 文字樣式（TextBlock 的描邊效果），無 API、無資料結構、無相依套件變更
- 描邊效果透過 WPF 既有的 `DropShadowEffect`（或等價方法）達成，無需新增第三方依賴
