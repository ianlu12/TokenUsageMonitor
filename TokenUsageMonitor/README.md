# Token Usage Monitor

Windows 桌面工具，監控 **GitHub Copilot**、**OpenCode Go** 與 **ChatGPT**（Plus/Pro 訂閱）的 token 使用額度。

## 功能

- **系統托盤圖示**：自繪 icon 即時顯示選定項目的使用量百分比；滑鼠懸停顯示提示文字
- **右鍵選單**：顯示各家服務的全部使用額度與重置時間、切換托盤顯示項目，並可設定 API Key、切換懸浮窗、開機自啟動、手動重整、退出
- **懸浮窗**：無邊框、置頂顯示選定項目的使用量，可滑鼠拖曳移動（位置會被記憶）
- **設定視窗**：設定各家認證資訊、是否顯示懸浮窗、調整輪詢間隔

## 建置

需要 .NET 8 SDK，並在 Windows 上建置。

```bash
dotnet build TokenUsageMonitor/ -c Release
```

或直接開啟 `TokenUsageMonitor.sln` 於 Visual Studio 建置並執行。

## 認證資訊取得

- **GitHub Copilot**：`gh auth login` 後執行 `gh auth token`（需有 Copilot 訂閱）。也可用 VS Code Copilot 的 `gho_*` 工作階段 token。內部端點 `GET https://api.github.com/copilot_internal/user`。
- **OpenCode Go**：`~/.local/share/opencode/auth.json` 中的 `opencode-go` 金鑰，端點 `GET https://opencode.ai/zen/go/v1/usage`。
- **ChatGPT（Plus/Pro 訂閱額度）**：需要 chatgpt.com 的 **access token**（**不是** `sk-...` API key）。可從 `~/.codex/auth.json` 的 `accessToken` 欄位，或瀏覽器 DevTools 任一 `backend-api` 請求的 `Authorization` 標頭取得；貼上時可含或不含 `Bearer ` 前綴。多帳號或遇 401 時，建議一併設定 `ChatGPT Account Id`（對應 `~/.codex/auth.json` 的 `account_id`）。端點 `GET https://chatgpt.com/backend-api/wham/usage`。

## 用量項目（model）

- GitHub Copilot：`premium_interactions`（Premium 請求）、`chat`（Chat 請求）、`completions`（程式碼補全）
- OpenCode Go：`rolling`（5 小時滾動）、`weekly`（每週）、`monthly`（每月）
- ChatGPT：`five-hour`（5 小時滾動）、`weekly`（每週）

## 設定檔與已知限制

- 設定儲存在 `%APPDATA%\TokenUsageMonitor\settings.json`（右鍵選單「設定 API Key…」開啟視窗修改）
- API Key / access token 以可逆 **混淆**（XOR）存放，**不是加密**；這是首版的已知限制
- 設定「開機自動啟動」會在 `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup` 建立 `.lnk` 捷徑
- ChatGPT 訂閱額度需在擁有 Plus/Pro 訂閱的帳號才可取得（免費版帳號會回傳 403 無權限）