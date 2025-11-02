# 物品快捷欄 UI 系統使用說明

## 概述
類似 Minecraft 的 1x10 物品快捷欄 UI 系統，支援顯示物品圖示、選中高亮、武器耐久度顯示。
**物品欄功能已整合到 GameUIManager 中**，無需獨立的 ItemHotbarUI 組件。

## 功能特點
- ✅ 1x10 格子物品欄
- ✅ 選中格子高亮顯示
- ✅ 武器耐久度條顯示（只在選中武器時）
- ✅ 按 R 鍵切換物品（已由 ItemHolder 實現）
- ✅ 自動同步 ItemHolder 的物品變化
- ✅ 統一由 GameUIManager 管理

## 📐 Canvas 完整架構

```
Canvas (Screen Space - Overlay)
├── GameUIManager.cs ← 掛在 Canvas 根物件上
│
├── HealthUI (左上角)
│   └── PlayerHealthUI (PlayerHealthUI.cs)
│
├── DangerUI (右上角)
│   └── DangerousUI (DangerousUI.cs)
│
├── ItemHotbar (底部中央) ⭐
│   ├── Background (Image) [可選]
│   └── SlotsContainer (HorizontalLayoutGroup) ⭐⭐⭐
│       └── (由 GameUIManager 動態生成 10 個 ItemSlot)
│
└── OtherUI
    ├── PauseMenuUI
    ├── MainMenuUI
    └── LoadingProgressUI
```

## 🎨 視覺化效果預覽

```
┌─────────────────────────────────────────────────────┐
│ [❤️ 100/100]                    [⚠️ 危險等級: 安全] │
│                                                      │
│                                                      │
│                  遊戲畫面區域                         │
│                                                      │
│                                                      │
│                                                      │
│ ┌────────────────────────────────────────────────┐  │
│ │ [🗡️][🔫][🔪][  ][  ][  ][  ][  ][  ][  ]      │  │ ← 物品欄
│ │  ▓▓▓▓▓░░                                       │  │ ← 耐久度條
│ │  └─ 選中（高亮）                                │  │
│ └────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

## Unity 設置步驟

### 步驟 0：設定 Canvas

1. **創建或檢查 Canvas**
   - 如果沒有 Canvas：右鍵 Hierarchy > UI > Canvas
   
2. **Canvas 組件設定**
   ```
   Canvas:
   - Render Mode: Screen Space - Overlay
   - Pixel Perfect: ☑ (可選)
   
   Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Match: 0.5
   ```

3. **在 Canvas 根物件添加 GameUIManager**
   - 選中 Canvas
   - Add Component > GameUIManager

---

### 步驟 1：創建物品格子預製體 (ItemSlot Prefab)

#### 1.1 創建根物件
```
右鍵 Hierarchy > Create Empty
命名為：ItemSlot
```

#### 1.2 設定 ItemSlot 根物件
```
RectTransform:
- Width: 70
- Height: 70
- Anchors: Middle-Center
- Pivot: (0.5, 0.5)

Components:
- 添加：ItemSlotUI.cs 腳本
```

#### 1.3 創建 Background (背景)
```
右鍵 ItemSlot > UI > Image
命名為：Background

RectTransform:
- Anchors: Stretch-Stretch
- Left/Right/Top/Bottom: 0

Image:
- Sprite: UI-Default (Unity 內建) 或自定義
- Color: (0.2, 0.2, 0.2, 0.8) 深灰半透明
- Material: None
```

#### 1.4 創建 SelectedBorder (選中框)
```
右鍵 ItemSlot > UI > Image
命名為：SelectedBorder

RectTransform:
- Anchors: Stretch-Stretch
- Left/Right/Top/Bottom: -3 (向外擴展 3 像素)

Image:
- Sprite: UI-Default
- Color: (1, 1, 0, 1) 亮黃色
- Image Type: Sliced (如果使用自定義 Sprite)
- 預設設定：Enabled = false ☐
```

#### 1.5 創建 ItemIcon (物品圖示)
```
右鍵 ItemSlot > UI > Image
命名為：ItemIcon

RectTransform:
- Anchors: Stretch-Stretch
- Left/Right/Top/Bottom: 8 (內邊距)

Image:
- Source Image: None (會動態設定)
- Color: (1, 1, 1, 1) 白色
- Preserve Aspect: ☑ 勾選
- 預設設定：Enabled = false ☐
```

#### 1.6 創建 DurabilityPanel (耐久度面板)
```
右鍵 ItemSlot > Create Empty
命名為：DurabilityPanel

RectTransform:
- Anchors: Bottom-Stretch
- Height: 8
- Left: 5, Right: -5
- Bottom: 5
- Pivot: (0.5, 0)

預設設定：Active = false ☐
```

#### 1.7 創建 DurabilityBar (耐久度條)
```
右鍵 DurabilityPanel > UI > Image
命名為：DurabilityBar

RectTransform:
- Anchors: Stretch-Stretch
- Left/Right/Top/Bottom: 0

Image:
- Sprite: UI-Default
- Color: (0, 1, 0, 1) 綠色（會動態變化）
- Image Type: Filled ⭐
- Fill Method: Horizontal
- Fill Origin: Left
- Fill Amount: 1
```

#### 1.8 連結 ItemSlotUI 腳本

選中 ItemSlot，在 Inspector 中連結：
```
ItemSlotUI 組件:
- Item Icon: 拖曳 ItemIcon
- Background: 拖曳 Background
- Selected Border: 拖曳 SelectedBorder
- Durability Bar: 拖曳 DurabilityBar
- Durability Panel: 拖曳 DurabilityPanel

Colors (顏色設定):
- Normal Color: (1, 1, 1, 1) 白色
- Selected Color: (1, 1, 0, 1) 黃色
- Empty Icon Color: (1, 1, 1, 0.2) 半透明白

Durability Colors:
- Durability High Color: (0, 1, 0, 1) 綠色
- Durability Medium Color: (1, 1, 0, 1) 黃色
- Durability Low Color: (1, 0, 0, 1) 紅色
```

#### 1.9 保存為預製體
```
1. 將 Hierarchy 中的 ItemSlot 拖曳到 Project 視窗
2. 建議路徑：Assets/Prefabs/UI/ItemSlot.prefab
3. 刪除 Hierarchy 中的 ItemSlot（已不需要）
```

**ItemSlot 預製體結構預覽：**
```
ItemSlot (70x70) [ItemSlotUI.cs]
├── Background (Image) - 深灰色背景
├── SelectedBorder (Image) - 黃色邊框 [預設隱藏]
├── ItemIcon (Image) - 物品圖示 [預設隱藏]
└── DurabilityPanel [預設隱藏]
    └── DurabilityBar (Image, Filled) - 耐久度條
```

---

### 步驟 2：創建物品快捷欄 (Item Hotbar)

#### 2.1 創建 ItemHotbar
```
右鍵 Canvas > Create Empty
命名為：ItemHotbar

RectTransform:
- Anchors: Bottom-Center
- Pivot: (0.5, 0)
- Pos X: 0
- Pos Y: 30 (距離底部 30 像素)
- Width: 760 (70 * 10 + 8 * 9 = 700 + 72 = 772)
- Height: 90
```

#### 2.2 創建 Background (可選)
```
右鍵 ItemHotbar > UI > Image
命名為：Background

RectTransform:
- Anchors: Stretch-Stretch
- Left/Right/Top/Bottom: 0

Image:
- Color: (0, 0, 0, 0.6) 半透明黑色
- Sprite: UI-Default
```

#### 2.3 創建 SlotsContainer ⭐⭐⭐
```
右鍵 ItemHotbar > Create Empty
命名為：SlotsContainer

RectTransform:
- Anchors: Stretch-Stretch
- Left: 10, Right: -10
- Top: -10, Bottom: 10
- Pivot: (0.5, 0.5)

Components:
- 添加：Horizontal Layout Group ⭐

Horizontal Layout Group 設定:
- Padding: Left/Right/Top/Bottom = 0
- Spacing: 8 (格子間距)
- Child Alignment: Middle Center
- Child Control Size:
  - Width: ☐ 不勾選
  - Height: ☐ 不勾選
- Child Force Expand:
  - Width: ☐ 不勾選
  - Height: ☐ 不勾選
```

**ItemHotbar 結構預覽：**
```
ItemHotbar (760x90)
├── Background (Image) [可選]
└── SlotsContainer [HorizontalLayoutGroup]
    └── (GameUIManager 會在這裡動態生成 10 個 ItemSlot)
```

---

### 步驟 3：設定 GameUIManager

選中 **Canvas** 物件，在 Inspector 中找到 **GameUIManager** 組件：

#### 3.1 UI Panels 設定
```
- Health Panel: 拖曳 HealthUI (如果有)
- Danger Panel: 拖曳 DangerUI (如果有)
- Hotbar Panel: 拖曳 ItemHotbar ⭐
```

#### 3.2 Item Hotbar Settings 設定 ⭐⭐⭐
```
- Item Slot Prefab: 拖曳 ItemSlot 預製體 ⭐
- Slots Container: 拖曳 ItemHotbar/SlotsContainer ⭐
- Max Slots: 10
- Auto Find Player: ☑ 勾選
```

#### 3.3 Settings 設定
```
- Show Health UI: ☑
- Show Danger UI: ☑
- Show Hotbar UI: ☑
```

---

## 🎯 物品設定

確保你的武器/物品 Prefab 有正確設定：

### 在武器 Prefab 上：
```
Weapon 組件 (或 Item 組件):
- Item Name: "刀" / "槍" 等
- Item Icon: 拖曳對應的 Sprite ⭐⭐⭐
  (建議大小：64x64 或 128x128)
```

### 在 Player 上：
```
ItemHolder 組件:
- Item Prefabs: 添加你的武器 Prefab ⭐
  (例如：Knife, Gun 等)
- Equip On Start: ☑ 勾選
```

---

## ✅ 完整檢查清單

使用前請確認以下所有項目：

### Canvas 設定
- [ ] Canvas 有 Canvas Scaler 組件
- [ ] Canvas 根物件有 GameUIManager 組件

### ItemSlot 預製體
- [ ] ItemSlot 預製體已創建且包含 ItemSlotUI 組件
- [ ] 有 Background、SelectedBorder、ItemIcon、DurabilityPanel 子物件
- [ ] DurabilityBar 的 Image Type 設定為 Filled
- [ ] ItemSlotUI 所有欄位都已正確連結

### ItemHotbar 設定
- [ ] ItemHotbar 已創建在 Canvas 下
- [ ] SlotsContainer 有 HorizontalLayoutGroup 組件
- [ ] SlotsContainer 的 Spacing 設定為 5-10

### GameUIManager 設定
- [ ] Hotbar Panel 已連結到 ItemHotbar
- [ ] Item Slot Prefab 已設定 ⭐
- [ ] Slots Container 已連結到 SlotsContainer ⭐
- [ ] Auto Find Player 已勾選
- [ ] Show Hotbar UI 已勾選

### 物品設定
- [ ] 武器/物品 Prefab 的 Item Icon 已設定
- [ ] Player 的 ItemHolder 有設定 Item Prefabs
- [ ] ItemHolder 的 Equip On Start 已勾選

---

## 🧪 測試步驟

### 1. 基本顯示測試
1. 進入 Play Mode
2. **預期結果**：
   - 螢幕底部應該看到 10 個灰色格子
   - 第一個格子應該有高亮邊框（黃色）
   - 如果有物品，應該看到物品圖示

### 2. 物品切換測試
1. 按 **R** 鍵切換物品
2. **預期結果**：
   - 高亮邊框移動到下一個格子
   - 物品圖示正確顯示
   - Console 沒有錯誤訊息

### 3. 耐久度顯示測試
1. 確保選中的是武器
2. 攻擊幾次（降低耐久度）
3. **預期結果**：
   - 選中格子底部應該看到耐久度條
   - 耐久度條隨攻擊減少
   - 顏色變化：綠 → 黃 → 紅

### 4. 空格子測試
如果物品少於 10 個：
- **預期結果**：空格子應該是半透明的

---

## 🔧 常見問題與解決方案

### Q1: 物品欄完全沒有顯示？

**檢查項目：**
1. Canvas 是否有 GameUIManager 組件？
2. GameUIManager 的 `Show Hotbar UI` 是否勾選？
3. ItemHotbar 物件是否啟用（Active）？
4. Console 是否有錯誤訊息？

**解決方法：**
- 確認 Hotbar Panel 已連結
- 檢查 ItemHotbar 的 RectTransform 位置是否在螢幕內

---

### Q2: 格子顯示了但都是空的？

**可能原因：**
1. Player 沒有 ItemHolder 組件
2. ItemHolder 的 Item Prefabs 沒有設定
3. 物品的 Item Icon 沒有設定

**解決方法：**
```
1. 檢查 Player > ItemHolder > Item Prefabs 是否有內容
2. 檢查每個武器 Prefab 的 Item Icon 欄位
3. 確認 GameUIManager 的 Auto Find Player 已勾選
```

**Debug 訊息：**
- 如果 Console 顯示 "物品欄設定不完整"
  → 檢查 Item Slot Prefab 和 Slots Container

---

### Q3: 格子有顯示，但沒有高亮效果？

**檢查項目：**
1. ItemSlotUI 的 Selected Border 是否連結？
2. SelectedBorder 是否設定正確的顏色？
3. GameUIManager 是否正確訂閱 ItemHolder 事件？

**解決方法：**
- 打開 ItemSlot 預製體檢查 SelectedBorder
- 確認 Selected Color 不是透明色
- 檢查 Player 是否有 ItemHolder 組件

---

### Q4: 耐久度條不顯示？

**這是正常的，因為：**
- 只有**武器**才顯示耐久度
- 只有**選中的格子**才顯示耐久度

**檢查項目：**
1. 當前選中的物品是否是武器？
2. DurabilityPanel 是否連結到 ItemSlotUI？
3. DurabilityBar 的 Fill Amount 是否 > 0？

---

### Q5: 按 R 切換沒反應？

**檢查項目：**
1. Player 的輸入處理是否正常？
2. ItemHolder 的 SwitchToNextItem 是否被調用？
3. Console 是否有錯誤？

**Debug 方法：**
```csharp
// 在 Player 的 Update 中添加：
if (Input.GetKeyDown(KeyCode.R))
{
    Debug.Log("R 鍵被按下");
}
```

---

### Q6: Console 出現錯誤

**常見錯誤：**

1. **"NullReferenceException: ItemHolder"**
   - 原因：Player 沒有 ItemHolder 組件
   - 解決：在 Player 上添加 ItemHolder

2. **"物品欄設定不完整"**
   - 原因：Item Slot Prefab 或 Slots Container 未設定
   - 解決：在 GameUIManager 中設定這兩個欄位

3. **"格子預製體缺少 ItemSlotUI 組件"**
   - 原因：Item Slot Prefab 沒有 ItemSlotUI 腳本
   - 解決：在預製體上添加 ItemSlotUI 組件

---

## 🎨 顏色配置建議

### ItemSlotUI 組件顏色設定：

**基本顏色：**
```
Normal Color: (1, 1, 1, 1) - 白色
Selected Color: (1, 1, 0, 1) - 亮黃色
或: (1, 0.84, 0, 1) - 金色 #FFD700

Empty Icon Color: (1, 1, 1, 0.2) - 半透明白色
```

**耐久度顏色：**
```
Durability High Color: (0, 1, 0, 1) - 綠色 (>50%)
Durability Medium Color: (1, 1, 0, 1) - 黃色 (25-50%)
Durability Low Color: (1, 0, 0, 1) - 紅色 (<25%)
```

**背景顏色建議：**
```
ItemSlot Background: (0.2, 0.2, 0.2, 0.8) - 深灰半透明
ItemHotbar Background: (0, 0, 0, 0.6) - 黑色半透明
```

---

## 📊 性能優化建議

1. **物品圖示優化**
   - 使用 Sprite Atlas 合併圖示
   - 圖示大小建議：64x64 或 128x128（不要太大）

2. **UI 更新優化**
   - GameUIManager 只在物品變更時更新
   - 耐久度條只在選中格子更新

3. **Layout Group 優化**
   - 使用固定大小的格子（避免動態計算）
   - 不勾選 Child Force Expand

---

## 🚀 擴展功能建議

未來可以添加的功能：

### 1. 物品 Tooltip
- 滑鼠懸停顯示物品名稱和詳細資訊

### 2. 數字快捷鍵
- 按 1-9, 0 直接切換到對應格子

### 3. 拖曳排序
- 滑鼠拖曳重新排列物品順序

### 4. 物品數量顯示
- 如果支援堆疊，顯示數量（如 x64）

### 5. 快捷鍵提示
- 在格子上方顯示對應數字（1-9, 0）

### 6. 動畫效果
- 切換時的過渡動畫
- 耐久度低時的閃爍警告
- 獲得新物品的高亮效果

---

## 📚 相關文件

- `ItemSlotUI.cs` - 單個物品格子的顯示邏輯
- `GameUIManager.cs` - UI 管理器（包含物品欄邏輯）
- `ItemHolder.cs` - 物品管理系統
- `Item.cs` - 物品基類
- `Weapon.cs` - 武器類別

---

## 📝 更新日誌

**2025-11-02**
- 整合 ItemHotbarUI 到 GameUIManager
- 簡化架構，減少獨立組件
- 添加完整的設置步驟和檢查清單
- 增強故障排除指南

---

## 💡 小提示

1. **創建預製體時記得保存場景**
2. **測試前確保所有欄位都已連結**
3. **使用 Console 查看 Debug 訊息**
4. **物品圖示建議使用 PNG 格式，背景透明**
5. **顏色可以根據遊戲風格自行調整**

---

如有任何問題，請檢查：
1. Console 錯誤訊息
2. GameUIManager Inspector 中的設定
3. ItemSlot 預製體的結構
4. Player 的 ItemHolder 設定

祝開發順利！🎮
