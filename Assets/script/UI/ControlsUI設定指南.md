# ControlsUI 設定指南

## 📋 概述

`ControlsUI` 是一個用於顯示遊戲按鍵說明的 UI 組件，可以在主選單中通過按鈕打開，顯示所有正式版本中可使用的按鍵說明。

---

## 🎯 功能特點

- ✅ 顯示所有正式版本的按鍵說明
- ✅ 分類清晰（移動、戰鬥、互動、武器切換、鏡頭控制等）
- ✅ 支援滾動查看（內容較多時）
- ✅ 可通過按鈕打開/關閉
- ✅ 自動格式化文字（使用 TextMeshPro 的富文本標籤）
- ✅ 自動調整 Content 高度（根據文字內容）
- ✅ 可設定底部留白（讓文字下方有空間）

---

## 🔧 Unity 設定步驟

### 步驟 1：創建 ControlsUI GameObject

在 MainMenuScene 的 Canvas 下創建：

```
Canvas
└── ControlsPanel (GameObject)
    ├── ControlsUI (Component) ← 新增此組件
    ├── Background (Image) ← 背景圖片（可選）
    ├── Title (TextMeshProUGUI) ← 標題（可選，因為內容已包含標題）
    ├── ScrollView (ScrollRect)
    │   └── Viewport
    │       └── Content
    │           └── ControlsText (TextMeshProUGUI) ← 顯示按鍵說明
    └── CloseButton (Button) ← 關閉按鈕
```

### 步驟 2：設定 ControlsUI 組件

在 `ControlsPanel` 上添加 `ControlsUI` 組件，並設定以下欄位：

```
ControlsUI:
  UI References:
    Controls Panel: 拖入 ControlsPanel GameObject
    Close Button: 拖入 CloseButton
  Content References:
    Controls Text: 拖入 ControlsText (TextMeshProUGUI)
    Scroll Rect: 拖入 ScrollView 上的 ScrollRect 組件
    Content Rect Transform: 拖入 Content GameObject 的 RectTransform ← 重要！
    Min Content Height: 100（最小高度，可調整）
    Bottom Padding: 20（底部留白，可調整）
  Settings:
    Hide On Start: ✅ 勾選（初始隱藏）
```

**重要提示**：
- `Content Rect Transform` 必須連接，用於自動調整 Content 高度
- 如果未手動連接，腳本會嘗試從 `ScrollRect.content` 自動獲取，但建議手動連接更可靠

### 步驟 3：設定 UI 元素

#### ControlsPanel (GameObject)
- 添加 `Image` 組件作為背景（可選）
- 設定 RectTransform：
  - Anchor: 居中（0.5, 0.5）
  - Position: (0, 0, 0)
  - Size: 例如 (800, 600) 或全螢幕

#### ScrollView (ScrollRect)
- 設定 ScrollRect：
  - Content: 拖入 Content GameObject
  - Horizontal: ❌ 取消勾選（只垂直滾動）
  - Vertical: ✅ 勾選
  - Movement Type: `Clamped` 或 `Elastic`（建議使用 Clamped 避免超出邊界）
  - Elasticity: 10（如果使用 Elastic，可調整彈性效果）
  - Scroll Sensitivity: 10-20（滾動速度，可根據需求調整）

#### Content (GameObject)
- **重要**：Content 的高度會由 `ControlsUI` 腳本自動調整，不需要手動設定固定高度
- 可選：添加 `Content Size Fitter` 組件（如果使用 Layout Group）
  - Vertical Fit: `Preferred Size`
- 設定 RectTransform：
  - Anchor: 左上角 (0, 1)
  - Pivot: (0, 1)
  - Size: 初始高度可設為任意值（腳本會自動調整）

#### ControlsText (TextMeshProUGUI)
- 設定 TextMeshProUGUI：
  - Text: 留空（由腳本自動填充）
  - Font Size: 24（可根據需要調整）
  - Alignment: 左上對齊
  - Overflow: Vertical（垂直溢出時允許滾動）
  - Rich Text: ✅ 勾選（支援富文本格式）

#### CloseButton (Button)
- 設定 Button：
  - 位置：通常在面板右上角或底部
  - Text: "關閉" 或 "X"
  - 大小：例如 (100, 40)

### 步驟 4：連接到 MainMenuUI

在 MainMenuScene 中找到 `MainMenuUI` 組件（通常在 Canvas 或主選單物件上）：

```
MainMenuUI:
  Start Button: 拖入開始按鈕
  Quit Button: 拖入退出按鈕
  Controls Button: 拖入新增的控制說明按鈕 ← 新增
  Controls UI: 拖入 ControlsPanel 上的 ControlsUI 組件 ← 新增
  Auto Find Controls UI: ✅ 勾選（如果只有一個 ControlsUI）
```

### 步驟 5：創建控制說明按鈕

在主選單中創建一個新按鈕：

```
Canvas
└── MainMenuPanel (或類似名稱)
    ├── StartButton
    ├── ControlsButton ← 新增此按鈕
    └── QuitButton
```

設定按鈕：
- Text: "操作說明" 或 "Controls" 或 "按鍵說明"
- 位置：放在 Start 和 Quit 按鈕之間

---

## 📝 顯示的按鍵說明內容

ControlsUI 會自動顯示以下按鍵說明：

### 基本移動控制
- W / A / S / D - 移動角色
- Shift - 快速奔跑
- Z - 蹲下
- 滑鼠點擊 - 移動到點擊位置

### 戰鬥操作
- Q - 攻擊

### 互動操作
- E - 互動（撿取物品、開門等）
- R - 切換物品

### 武器快速切換
- 1 / 小鍵盤1 - 切換到小刀
- 2 / 小鍵盤2 - 切換到槍
- 3 / 小鍵盤3 - 切換到空手

### 鏡頭控制
- Space（長按） - 移動鏡頭
- Y - 將相機拉回以玩家為中心

### 遊戲控制
- ESC - 暫停/恢復遊戲

### UI 功能
- M - 地圖縮放

---

## 🎨 自訂樣式建議

### 背景樣式
- 使用半透明黑色背景（RGBA: 0, 0, 0, 200）
- 添加邊框或陰影效果

### 文字樣式
- 標題使用較大字體（36）
- 分類標題使用中等字體（28）
- 內容使用正常字體（24）
- 使用不同顏色區分不同類別

### 按鈕樣式
- 關閉按鈕使用明顯的樣式（紅色或帶 X 圖標）
- 可以添加懸停效果

---

## 🔍 程式碼 API

### 公開方法

```csharp
// 顯示按鍵說明面板
controlsUI.Show();

// 隱藏按鍵說明面板
controlsUI.Hide();

// 切換顯示/隱藏
controlsUI.Toggle();

// 設定可見性
controlsUI.SetVisible(true);  // 顯示
controlsUI.SetVisible(false); // 隱藏

// 檢查是否可見
bool isVisible = controlsUI.IsVisible();
```

---

## ✅ 測試檢查清單

- [ ] ControlsPanel 初始狀態為隱藏
- [ ] 點擊主選單的「操作說明」按鈕後，ControlsPanel 顯示
- [ ] 按鍵說明文字正確顯示，格式正確
- [ ] 內容可以正常滾動（如果內容超出視窗）
- [ ] 滾動到底部時，不會超出文字邊界（使用 Clamped 模式）
- [ ] 滾動到底部時，文字下方有適當的留白
- [ ] Content 高度自動根據文字內容調整
- [ ] 點擊「關閉」按鈕後，ControlsPanel 隱藏
- [ ] 文字支援富文本格式（粗體、大小等）

---

## 🐛 常見問題

### 問題 1：點擊按鈕沒有反應

**解決方法**：
- 檢查 ControlsButton 是否正確連接到 MainMenuUI
- 檢查 ControlsUI 組件是否正確連接到 MainMenuUI
- 檢查 Console 是否有錯誤訊息

### 問題 2：文字顯示不正確

**解決方法**：
- 確認 ControlsText 的 Rich Text 選項已勾選
- 檢查 TextMeshProUGUI 的字體是否支援中文
- 確認 ControlsText 的 Overflow 設定為 Vertical

### 問題 3：無法滾動

**解決方法**：
- 檢查 ScrollRect 的 Content 是否正確設定
- 確認 `Content Rect Transform` 已連接到 ControlsUI 組件
- 確認 Content 的 RectTransform 高度大於 Viewport（腳本會自動調整）
- 檢查 ScrollRect 的 Vertical 選項是否勾選
- 如果使用 `Clamped` 模式無法滾動，檢查 Content 高度是否正確計算

### 問題 4：滾動超出文字邊界

**解決方法**：
- 確認 `Content Rect Transform` 已正確連接
- 檢查 `Movement Type` 是否設為 `Clamped`（避免超出邊界）
- 確認 `Bottom Padding` 設定合理（建議 20-40）
- 如果使用 `Elastic` 模式，將 `Elasticity` 調小（例如 0.01-0.05）以減少彈性效果

### 問題 5：底部沒有留白

**解決方法**：
- 在 ControlsUI 組件中調整 `Bottom Padding` 值（預設 20）
- 數值越大，底部留白越多（建議 20-50）

---

## 📚 相關文件

- `Architecture/KeyboardControls.md` - 鍵盤操作說明文檔
- `UI/MainMenuUI.cs` - 主選單 UI 腳本
- `UI/ControlsUI.cs` - 控制說明 UI 腳本

---

## 🎯 後續擴展建議

1. **多語言支援**：可以擴展支援多種語言
2. **按鍵自訂**：允許玩家自訂按鍵配置
3. **動畫效果**：添加打開/關閉的動畫效果
4. **分類標籤**：使用標籤頁切換不同類別的說明
5. **圖示顯示**：為每個按鍵添加圖示

---

## 📌 新增功能說明（v2.0）

### 自動調整 Content 高度

ControlsUI 現在會自動根據文字內容調整 Content 的高度，確保：
- 滾動邊界正確對齊文字內容
- 不會出現空白區域或內容被截斷
- 底部有適當的留白空間

### 設定參數

- **Content Rect Transform**：必須連接 Content GameObject 的 RectTransform
- **Min Content Height**：Content 的最小高度（預設 100），避免內容太少時顯示異常
- **Bottom Padding**：底部留白大小（預設 20），可調整讓文字下方有更多空間

### 使用建議

- 使用 `Clamped` 模式可以避免滾動超出邊界
- 根據需要調整 `Bottom Padding` 值（建議 20-50）
- 如果滾動速度不適合，調整 `Scroll Sensitivity`（建議 10-20）

