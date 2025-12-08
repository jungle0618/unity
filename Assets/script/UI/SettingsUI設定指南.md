# SettingsUI 設定指南

## 📋 概述

`SettingsUI` 是用於遊戲設定的使用者介面組件，可以在主選單或暫停選單中使用。本指南將幫助您改善 SettingsUI 的排版，使其更加美觀和易用。

---

## 🎨 排版改善建議

### 整體結構建議

建議的 UI 層級結構：

```
SettingsPanel (GameObject)
├── Background (Image) ← 背景圖片
├── TitleBar (GameObject) ← 標題欄（可選）
│   ├── Title (TextMeshProUGUI) ← "設定"
│   └── CloseButton (Button) ← 關閉按鈕
├── ContentArea (GameObject) ← 內容區域（使用 ScrollView 如果內容過多）
│   ├── ScrollView (ScrollRect) ← 可選，如果內容很多
│   │   └── Viewport
│   │       └── Content
│   │           ├── PlayerSettingsSection (GameObject)
│   │           ├── AudioSettingsSection (GameObject)
│   │           ├── GraphicsSettingsSection (GameObject)
│   │           └── GameplaySettingsSection (GameObject)
│   └── (或直接使用 VerticalLayoutGroup，不使用 ScrollView)
└── ButtonArea (GameObject) ← 按鈕區域
    ├── ResetButton (Button)
    └── ApplyButton (Button)
```

---

## 🔧 Unity 設定步驟

### 步驟 1：主容器設定

#### SettingsPanel (GameObject)
1. **添加 VerticalLayoutGroup 組件**：
   - Padding: Left: 30, Right: 30, Top: 30, Bottom: 30
   - Spacing: 25（區塊之間的間距）
   - Child Alignment: Upper Center
   - Child Force Expand: Width ✅, Height ❌
   - Child Control Size: Width ✅, Height ❌

2. **設定 RectTransform**：
   - Anchor: 居中 (0.5, 0.5)
   - Position: (0, 0, 0)
   - Size: 例如 (800, 900) 或根據螢幕大小調整

3. **添加 Image 組件**（背景）：
   - Color: 半透明黑色 (0, 0, 0, 200) 或使用背景圖片
   - 可選：添加圓角效果（使用 Mask 或自定義 Shader）

---

### 步驟 2：標題欄設定（可選）

#### TitleBar (GameObject)
1. **添加 HorizontalLayoutGroup 組件**：
   - Padding: Left: 0, Right: 0, Top: 0, Bottom: 0
   - Spacing: 0
   - Child Alignment: Middle
   - Child Force Expand: Width ✅, Height ❌

2. **Title (TextMeshProUGUI)**：
   - Text: "設定" 或 "Settings"
   - Font Size: 32-36
   - Alignment: 左對齊
   - Color: 白色或主題色

3. **CloseButton (Button)**：
   - 位置：右側
   - Text: "X" 或 "關閉"
   - Size: (40, 40) 或 (80, 40)

---

### 步驟 3：內容區域設定

#### ContentArea (GameObject)
**選項 A：使用 ScrollView（內容較多時）**

1. **添加 ScrollView (ScrollRect)**：
   - Content: 拖入 Content GameObject
   - Horizontal: ❌
   - Vertical: ✅
   - Movement Type: Clamped
   - Scroll Sensitivity: 15

2. **Content (GameObject)**：
   - 添加 VerticalLayoutGroup：
     - Padding: Left: 0, Right: 0, Top: 0, Bottom: 20
     - Spacing: 25
     - Child Alignment: Upper Center
   - 添加 Content Size Fitter：
     - Vertical Fit: Preferred Size

**選項 B：直接使用 VerticalLayoutGroup（內容較少時）**

1. **添加 VerticalLayoutGroup 組件**：
   - Padding: Left: 0, Right: 0, Top: 0, Bottom: 0
   - Spacing: 25
   - Child Alignment: Upper Center

---

### 步驟 4：各個設定區塊（Section）設定

每個區塊（PlayerSettingsSection、AudioSettingsSection 等）的設定：

#### Section GameObject
1. **添加 VerticalLayoutGroup 組件**：
   - Padding: Left: 20, Right: 20, Top: 15, Bottom: 15
   - Spacing: 15（區塊內項目間距）
   - Child Alignment: Upper Left
   - Child Force Expand: Width ✅, Height ❌

2. **添加 Image 組件**（背景，可選）：
   - Color: 半透明灰色 (50, 50, 50, 150)
   - 或使用帶圓角的背景圖片

3. **添加標題文字**（可選）：
   - 在 Section 的第一個子物件添加 TextMeshProUGUI
   - Text: "玩家設定"、"音效設定" 等
   - Font Size: 24-28
   - Font Style: Bold
   - Color: 主題色或白色

---

### 步驟 5：音量滑桿設定

每個音量滑桿（Master/Music/SFX Volume）的建議結構：

```
VolumeItem (GameObject) ← 使用 HorizontalLayoutGroup
├── Label (TextMeshProUGUI) ← "主音量："、"音樂音量：" 等
├── Slider (Slider)
└── ValueText (TextMeshProUGUI) ← "50%"
```

#### VolumeItem (GameObject)
1. **添加 HorizontalLayoutGroup 組件**：
   - Padding: Left: 0, Right: 0, Top: 0, Bottom: 0
   - Spacing: 15
   - Child Alignment: Middle
   - Child Force Expand: Width ❌, Height ❌

2. **Label (TextMeshProUGUI)**：
   - Width: 120-150（固定寬度）
   - Text: "主音量："、"音樂音量："、"音效音量："
   - Font Size: 18-20
   - Alignment: 左對齊

3. **Slider (Slider)**：
   - 使用 LayoutElement 組件：
     - Flexible Width: 1（佔用剩餘空間）
   - Min Value: 0
   - Max Value: 1
   - Whole Numbers: ❌

4. **ValueText (TextMeshProUGUI)**：
   - Width: 60（固定寬度）
   - Text: "50%"
   - Font Size: 18-20
   - Alignment: 右對齊

---

### 步驟 6：切換開關（Toggle）設定

每個 Toggle 的建議結構：

```
ToggleItem (GameObject) ← 使用 HorizontalLayoutGroup
├── Label (TextMeshProUGUI) ← "啟用跑步"、"全螢幕" 等
└── Toggle (Toggle)
```

#### ToggleItem (GameObject)
1. **添加 HorizontalLayoutGroup 組件**：
   - Padding: Left: 0, Right: 0, Top: 0, Bottom: 0
   - Spacing: 10
   - Child Alignment: Middle
   - Child Force Expand: Width ❌, Height ❌

2. **Label (TextMeshProUGUI)**：
   - 使用 LayoutElement：
     - Flexible Width: 1
   - Font Size: 18-20
   - Alignment: 左對齊

3. **Toggle (Toggle)**：
   - 使用 LayoutElement：
     - Preferred Width: 50（固定寬度）

---

### 步驟 7：下拉選單（Dropdown）設定

FPS Dropdown 的建議結構：

```
DropdownItem (GameObject) ← 使用 HorizontalLayoutGroup
├── Label (TextMeshProUGUI) ← "目標幀率："
└── Dropdown (TMP_Dropdown)
```

#### DropdownItem (GameObject)
1. **添加 HorizontalLayoutGroup 組件**：
   - Padding: Left: 0, Right: 0, Top: 0, Bottom: 0
   - Spacing: 15
   - Child Alignment: Middle

2. **Label (TextMeshProUGUI)**：
   - Width: 120-150（固定寬度）
   - Text: "目標幀率："
   - Font Size: 18-20

3. **Dropdown (TMP_Dropdown)**：
   - 使用 LayoutElement：
     - Preferred Width: 200
     - Preferred Height: 40

---

### 步驟 8：按鈕區域設定

#### ButtonArea (GameObject)
1. **添加 HorizontalLayoutGroup 組件**：
   - Padding: Left: 0, Right: 0, Top: 20, Bottom: 0
   - Spacing: 20（按鈕之間的間距）
   - Child Alignment: Middle Center
   - Child Force Expand: Width ❌, Height ❌

2. **按鈕設定**：
   - ResetButton 和 ApplyButton
   - 使用 LayoutElement：
     - Preferred Width: 150-200
     - Preferred Height: 50
   - 建議使用相同的樣式以保持一致性

---

## 🎯 視覺美化建議

### 顏色方案
- **背景色**：深色半透明 (0, 0, 0, 200-220)
- **區塊背景**：稍亮的灰色 (50, 50, 50, 150-180)
- **文字顏色**：白色 (#FFFFFF) 或淺灰色 (#E0E0E0)
- **標題顏色**：主題色或金色 (#FFD700)
- **按鈕顏色**：主題色，Hover 時稍亮

### 間距建議
- **主容器 Padding**：30（上下左右）
- **區塊間距 (Spacing)**：25-30
- **區塊內項目間距**：15
- **按鈕間距**：20

### 字體大小建議
- **標題**：32-36
- **區塊標題**：24-28
- **一般文字**：18-20
- **按鈕文字**：20-22

### 圓角效果（可選）
- 使用 Mask 組件配合 Image 實現圓角背景
- 或使用自定義 Shader 實現圓角效果

---

## ✅ 檢查清單

完成設定後，請確認：

- [ ] 所有區塊使用 VerticalLayoutGroup 整齊排列
- [ ] 音量滑桿使用 HorizontalLayoutGroup 水平排列
- [ ] 切換開關和下拉選單都有清晰的標籤
- [ ] 按鈕區域使用 HorizontalLayoutGroup 居中排列
- [ ] 所有間距一致且美觀
- [ ] 文字大小適中且易讀
- [ ] 顏色方案統一且符合遊戲風格
- [ ] 在不同解析度下測試，確保排版正常

---

## 🔍 常見問題

### Q: 內容太多，超出螢幕範圍怎麼辦？
A: 使用 ScrollView 包裹 ContentArea，並確保 Content 有 Content Size Fitter 組件。

### Q: 如何讓區塊之間有分隔線？
A: 在每個 Section 下方添加一個 Image 作為分隔線，高度設為 1-2，顏色設為半透明。

### Q: 如何讓設定面板居中顯示？
A: 確保 SettingsPanel 的 RectTransform Anchor 設為 (0.5, 0.5)，Pivot 也設為 (0.5, 0.5)。

### Q: 如何實現響應式設計？
A: 使用 Anchor Presets（例如：Stretch-Stretch）讓面板在不同解析度下自動調整大小。

---

## 📝 注意事項

1. **性能考量**：如果使用 ScrollView，確保只在需要時啟用，避免不必要的重繪。

2. **可訪問性**：確保文字大小足夠大，顏色對比度足夠高，方便所有玩家閱讀。

3. **一致性**：保持與遊戲其他 UI 元素的風格一致。

4. **測試**：在不同解析度（1920x1080, 1366x768, 2560x1440 等）下測試排版效果。

---

完成以上設定後，您的 SettingsUI 應該會有一個更加美觀和專業的排版！

