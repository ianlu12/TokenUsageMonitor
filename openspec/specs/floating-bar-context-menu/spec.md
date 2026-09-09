# floating-bar-context-menu Capability Specification

> Status: Done

## Requirements

### Requirement: 懸浮條右鍵選單顯示項目多選
系統 SHALL 在懸浮條上提供右鍵選單（ContextMenu），選單內列出所有可用用量計數器（來自 UsageCatalog），每個計數器以 IsCheckable MenuItem 呈現，允許多選勾選要同時顯示在懸浮條上的項目。

#### Scenario: 開啟右鍵選單
- **WHEN** 使用者在懸浮條上按下滑鼠右鍵
- **THEN** 系統顯示 ContextMenu，選單內「顯示項目」子選單列出所有 UsageCatalog 計數器，已勾選者與 VisibleMeters 設定值相符

#### Scenario: 勾選項目即時顯示
- **WHEN** 使用者在右鍵選單中勾選一個目前未勾選的計數器
- **THEN** 系統立即將該計數器加入 VisibleMeters，並在懸浮條上新增對應的計數器顯示行（名稱＋百分比＋明細）

#### Scenario: 取消勾選即時隱藏
- **WHEN** 使用者在右鍵選單中取消勾選一個目前勾選的計數器
- **THEN** 系統立即將該計數器從 VisibleMeters 移除，懸浮條上對應的計數器顯示行消失

#### Scenario: 勾選變更持久化
- **WHEN** 使用者透過右鍵選單勾選或取消勾選項目
- **THEN** 系統將新的 VisibleMeters 清單寫入 settings.json，應用程式重啟後保留

#### Scenario: 所有項目取消勾選
- **WHEN** 使用者取消勾選所有計數器
- **THEN** 系統顯示「未設定」佔位提示，懸浮條不顯示任何計數器卡片

#### Scenario: 預設顯示項目
- **WHEN** 應用程式首次啟動且 VisibleMeters 尚無值
- **THEN** 系統預設勾選目前的 SelectedMeter（維持向後相容）

### Requirement: 懸浮條背景透明度調整
系統 SHALL 在懸浮條右鍵選單中提供「背景透明度」子選單，支援以預設檔位調整懸浮條背景透明度，並即時套用、持久化至 settings.json。

#### Scenario: 顯示透明度選項
- **WHEN** 使用者在懸浮條右鍵選單中展開「背景透明度」子選單
- **THEN** 系統顯示四個檔位選項（100% / 75% / 50% / 25%），目前作用中的檔位以勾選標示

#### Scenario: 選擇透明度檔位即時套用
- **WHEN** 使用者選擇一個透明度檔位
- **THEN** 系統立即以對應的 Opacity（1.0 / 0.75 / 0.5 / 0.25）套用於懸浮條背景，文字前景保持不透明

#### Scenario: 透明度持久化
- **WHEN** 使用者變更透明度
- **THEN** 系統將 OverlayOpacity 寫入 settings.json，應用程式重啟後保留

#### Scenario: 透明度預設值
- **WHEN** 應用程式首次啟動且 OverlayOpacity 尚無值
- **THEN** 系統預設使用 100%（1.0）透明度，與目前行為完全相容

### Requirement: 懸浮條快速操作
系統 SHALL 在右鍵選單底部提供「重新整理」與「隱藏懸浮窗」快捷操作，行為與托盤選單一致。

#### Scenario: 重新整理
- **WHEN** 使用者在右鍵選單點擊「重新整理」
- **THEN** 系統觸發重新整理流程，資料更新後懸浮條內容即時刷新

#### Scenario: 隱藏懸浮窗
- **WHEN** 使用者在右鍵選單點擊「隱藏懸浮窗」
- **THEN** 系統隱藏懸浮條，托盤選單中「顯示懸浮窗」同步更新為未勾選狀態

#### Scenario: 懸浮條保持拖曳功能
- **WHEN** 使用者在懸浮條上按下滑鼠左鍵並拖曳
- **THEN** 懸浮條正常拖曳移動，不受右鍵選單影響