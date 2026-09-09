## Why

開發者使用 GitHub Copilot 和 OpenCode Go 等 AI 程式碼助手時，難以即時掌握 token 使用額度與重置時間，容易超出限額導致服務中斷。目前缺乏一個輕量級的桌面工具來集中監控這些服務的使用狀況。

## What Changes

- 新增 C# Windows 桌面應用程式，支援系統托盤圖示與懸浮窗顯示
- 整合 GitHub Copilot 與 OpenCode Go 的 API，即時查詢 token 使用額度
- 提供使用者介面設定 API Key、選擇模型、控制顯示選項
- 支援開機自動啟動功能

## Capabilities

### New Capabilities

- `token-quota-monitoring`: 即時查詢並顯示 GitHub Copilot 與 OpenCode Go 的 token 使用額度與重置時間
- `system-tray-integration`: 系統托盤圖示顯示、右鍵選單操作、即時更新模型使用量
- `floating-overlay`: 懸浮窗顯示選定模型的使用量，支援拖曳移動與置頂顯示
- `settings-management`: API Key 設定、模型選擇、開機自動啟動等設定管理

### Modified Capabilities

（無既有功能需要修改）

## Impact

- 新增 C# 專案（WPF 或 WinForms）
- 依赖 GitHub Copilot API 與 OpenCode Go API
- 需要 Windows 系統托盤 API（NotifyIcon）
- 需要 Windows 登錄檔或啟動資料夾來實現開機自動啟動
