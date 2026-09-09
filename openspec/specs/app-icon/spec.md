# app-icon Capability Specification

> Status: Done

## Requirements

### Requirement: 應用程式正式圖示
系統 SHALL 提供一個正式應用程式圖示（.ico 多尺寸），應用於應用程式執行檔、視窗標題列與視覺識別，並與現有托盤的深色圓形百分比視覺風格一致。

#### Scenario: 執行檔圖示
- **WHEN** 使用者檢視應用程式執行檔（.exe）的圖示
- **THEN** 系統顯示正式的應用程式圖示，而非系統預設圖示

#### Scenario: 視窗圖示
- **WHEN** 懸浮窗或設定視窗顯示
- **THEN** 系統在視窗標題列（若有顯示）顯示應用程式圖示，作為統一識別

#### Scenario: 一致性視覺
- **WHEN** 應用程式圖示與托盤動態圖示同時出現
- **THEN** 兩者採用一致的色彩與圓形風格，呈現統一的品牌識別