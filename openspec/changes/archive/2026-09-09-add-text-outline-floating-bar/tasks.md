## 1. 為懸浮窗文字套用黑色描邊

- [x] 1.1 在 `TokenUsageMonitor/Views/OverlayWindow.xaml` 中，為「未設定」佔位 TextBlock（第 48 行）套用 `DropShadowEffect`（Color=Black、BlurRadius=2、ShadowDepth=1、Direction=270、Opacity=1）
- [x] 1.2 為使用量卡片內的服務/模型名稱 TextBlock（`Display`，第 58 行）套用相同參數的 `DropShadowEffect`
- [x] 1.3 為使用量卡片內的百分比數字 TextBlock（`Percent`，第 61 行）套用相同參數的 `DropShadowEffect`
- [x] 1.4 為使用量卡片內的明細文字 TextBlock（`Detail`，第 63 行）套用相同參數的 `DropShadowEffect`
- [x] 1.5 確認四個 TextBlock 的 `Foreground`、`FontSize`、`FontWeight` 及 `Padding/Margin` 均未被改動，文字內容與呈現與套用前一致

## 2. 驗證

- [ ] 2.1 建置專案（`dotnet build`）確認無錯誤
- [ ] 2.2 啟動應用程式，將「未設定」佔位文字顯示於淺色背景上，確認文字帶有黑色描邊且可清晰讀取
- [ ] 2.3 對任一計數器卡片，調整背景透明度（100% → 25%），確認描邊保持完全不透明、可讀性不受影響
- [ ] 2.4 確認拖曳移動、置頂顯示、位置記憶、右鍵選單等既有功能仍正常運作