## Context

懸浮窗 (`OverlayWindow.xaml`) 目前以四個 TextBlock 呈現文字：佔位「未設定」、服務/模型名稱 (`Display`)、百分比數字 (`Percent`)、明細 (`Detail`)。所有文字前景色使用完全不透明的顏色（`#FFD8D8D8`、`White`、`#FFAFAFAF`），不受背景透明度滑桿影響。但當懸浮窗背景透明度調低、底下為淺色視窗時，文字邊緣與淺色背景之間對比不足，可讀性下降。

## Goals / Non-Goals

**Goals:**
- 為懸浮窗內所有文字加上黑色描邊（outline/stroke），在任何背景上皆清晰可讀
- 保持既有文字顏色、字型、大小、佈局與內容不變
- 不影響既有行為：拖曳、置頂、透明度、位置記憶、內容更新

**Non-Goals:**
- 不新增任何使用者可見的設定項（描邊為固定視覺樣式）
- 不更改文字內容、顏色、字型或間距
- 不引入外部相依套件或自訂控制項

## Decisions

### Decision 1: 使用 `DropShadowEffect` 達成描邊效果
WPF 的 `TextBlock` 沒有內建文字描邊（stroke）屬性（該能力僅存在於 `FormattedText` 與 DrawingContext）。最簡潔的替代方式是在每個 TextBlock 上套用 `DropShadowEffect`：

```xml
<TextBlock.Effect>
    <DropShadowEffect Color="Black" BlurRadius="2" ShadowDepth="1" Direction="270" Opacity="1"/>
</TextBlock.Effect>
```

- **BlurRadius**: 建議 2，提供柔和但不失清晰的黑邊
- **ShadowDepth**: 1，僅向下偏移 1px，營造類似描邊的貼合感
- **Opacity**: 1，確保黑邊完全不透明、穩定可讀

**替代方案考量：**
- `DropShadowEffect` 的四方向疊加（上下左右）可形成完整描邊，但會增加運算成本。由於懸浮窗僅包含少量文字（4 個 TextBlock），單一方向陰影已足夠提供足夠對比，且效能可忽略。
- 自訂 `BitmapEffect`/自訂控制項可達成精確描邊，但過度工程化，不符本變更規模。
- 直接改文字顏色為深色會破壞既有視覺設計，故不採用。

### Decision 2: 以 `Window.Resources` 定義共用樣式避免重複
為避免在 4 個 TextBlock 上重複貼上相同的 `Effect` 屬性，可考慮在 `Window.Resources` 定義一個 `DropShadowEffect` 共用資源並以 `StaticResource` 引用。然而考量到各文字字型大小不同（10/11/20），單一效果參數未必適合所有字型。**決策**：直接在各 TextBlock 個別套用相同參數的 `DropShadowEffect`，因目前所有文字都需同等強度的黑邊，統一參數即可；若後續需要不同強度，個別設定更靈活。

### Decision 3: 不修改 `OpacityToBackgroundConverter`
該 converter 僅控制背景 Alpha 通道。本變更不動它；描邊效果獨立運作，與背景透明度無關。

## Risks / Trade-offs

- **描邊可讀性過於突兀** → 使用小 BlurRadius（2）與 ShadowDepth（1）的柔和效果，而非粗實邊框，保持視覺輕量。
- **`DropShadowEffect` 在大量元素時有效能成本** → 本變更僅套用於 4 個 TextBlock，運算成本可忽略。
- **不同字型大小的描邊厚度不均** → 統一參數在大字（20px 百分比）上邊框相對較細、在小字（10px 明細）上相對較粗；若不滿意可後續為個別 TextBlock 調整參數，此為可接受且易調整的取捨。
