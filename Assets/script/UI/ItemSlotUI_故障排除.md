# ItemSlotUI Icon 無法顯示 - 故障排除指南

## 問題描述
在遊戲運行時，ItemSlotUI 的物品圖示（item icon）無法正常顯示。

## ⚠️ 編輯器能顯示但實際運行不能顯示

**如果您的情況是：**
- ✅ 在 Unity Editor 的 Play Mode 中可以看到 icon
- ❌ 在 Game 視圖或 Build 後的遊戲中看不到 icon

**最常見的原因和解決方案：**

### 解決方案 1：檢查 Image 組件的 Raycast Target ✅

動態創建的 UI 元素有時會因為 Raycast 設置導致渲染問題。

**修復步驟：**
1. 打開 ItemSlot Prefab
2. 選中 **ItemIcon** Image 物件
3. 在 Inspector 的 Image 組件中：
   - ✓ 確認 **Color** 的 Alpha 值為 255（完全不透明）
   - ✓ 取消勾選 **Raycast Target**（不需要接收點擊）
   - ✓ 確認 **Raycast Padding** 為 (0,0,0,0)

### 解決方案 2：確保 Sprite 在 Build 中被包含 ⭐

**問題：** Sprite 可能沒有被 Unity 打包到 Build 中

**修復步驟：**
1. 選中武器 Prefab（例如 Gun, Knife）
2. 確認 **Item Icon** Sprite 已正確設置
3. 選中該 Sprite 資源（在 Project 視窗）
4. 檢查 Inspector：
   ```
   Texture Type: Sprite (2D and UI) ← 必須
   Sprite Mode: Single
   Pixels Per Unit: 100
   Filter Mode: Bilinear
   Compression: None (或 Low Quality)
   ```
5. 點擊 **Apply**

### 解決方案 3：添加調試代碼確認 Sprite 是否存在 🔍

在 ItemSlotUI.cs 中臨時添加：

```csharp
public void SetItem(Item item)
{
    if (item == null)
    {
        SetEmpty();
        return;
    }
    
    isEmpty = false;
    
    // 顯示物品圖示
    if (itemIcon != null)
    {
        if (item.ItemIcon != null)
        {
            itemIcon.sprite = item.ItemIcon;
            itemIcon.color = Color.white;
            itemIcon.enabled = true;
            
            // 🔍 添加調試輸出
            Debug.Log($"[ItemSlotUI] Icon設置成功: {item.ItemName}, Sprite: {item.ItemIcon.name}, 尺寸: {item.ItemIcon.rect.size}");
        }
        else
        {
            Debug.LogError($"[ItemSlotUI] 物品 '{item.ItemName}' 的 ItemIcon 為 null！");
            itemIcon.sprite = null;
            itemIcon.color = emptyIconColor;
            itemIcon.enabled = false;
        }
    }
    else
    {
        Debug.LogError("[ItemSlotUI] itemIcon Image 組件為 null！");
    }
    
    // ... 其餘代碼
}
```

運行遊戲後查看 Console 輸出。

### 解決方案 4：確保 Canvas 渲染正確 🎨

1. 選中 Canvas 物件
2. 確認設置：
   ```
   Canvas:
   - Render Mode: Screen Space - Overlay ✓
   - Pixel Perfect: ☑ (取消勾選試試)
   - Sort Order: 0
   
   Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Screen Match Mode: Match Width Or Height
   - Match: 0.5
   ```

3. 確認 Canvas 上沒有其他組件影響渲染（例如 Canvas Group）

### 解決方案 5：檢查 ItemIcon 的層級順序 📐

ItemIcon 必須在正確的渲染順序中：

**正確的 Prefab 結構：**
```
ItemSlot
├── Background (Image) ← 最底層
├── ItemIcon (Image) ← 中間層，顯示在背景上方
├── SelectedBorder (Image) ← 最上層
└── DurabilityPanel
    └── DurabilityBar (Image)
```

**檢查方法：**
1. 在 Hierarchy 中，從上到下的順序就是渲染順序（下面的在上層）
2. ItemIcon 應該在 Background 之後（顯示在上方）
3. 如果順序錯誤，直接拖動調整

### 解決方案 6：禁用 Sprite Packing（如果使用了） 📦

如果您啟用了 Sprite Atlas 或 Sprite Packing：

1. 選中 Sprite 資源
2. 在 Inspector 中找到 **Packing Tag**
3. 清空或設置為 "UI"
4. 重新 Build

### 解決方案 7：使用 Resources 資料夾（備用方案） 📂

如果 Sprite 仍然無法在 Build 中顯示：

1. 在 Assets 下創建 `Resources/UI/Icons` 資料夾
2. 將所有物品圖示 Sprite 放入此資料夾
3. 在武器 Prefab 中重新設置 Item Icon 引用

Unity 會自動將 Resources 資料夾中的所有資源打包到 Build 中。

## 常見原因和解決方案

### 1. ✅ Inspector 設定檢查（最常見）

#### 檢查 ItemSlotUI Prefab
1. 在 Project 視窗找到 ItemSlot Prefab
2. 選中後在 Inspector 檢查 `ItemSlotUI` 組件
3. 確認以下欄位是否已正確賦值：
   - **Item Icon** → 應該指向一個 Image 組件
   - **Background** → 背景 Image
   - **Selected Border** → 選中框 Image
   - **Durability Bar** → 耐久度條 Image（fillAmount 類型）
   - **Durability Panel** → 耐久度面板 GameObject

#### 修復方法
如果 `Item Icon` 欄位為空：
1. 在 ItemSlot Prefab 的 Hierarchy 中找到顯示圖示的 Image 物件
2. 將這個 Image 拖拽到 ItemSlotUI 組件的 `Item Icon` 欄位

### 2. ✅ Image 組件設定檢查

#### 檢查 Item Icon Image 的設定
選中 ItemSlot Prefab 中的 Item Icon Image 物件，確認：

**必須設置：**
- ✓ **Source Image**: 可以留空（會在運行時動態設置）
- ✓ **Color**: 白色 `(255, 255, 255, 255)`
- ✓ **Material**: None (預設)
- ✓ **Raycast Target**: 可以取消勾選（不需要接收點擊）

**Image Type 設定：**
- **Image Type**: Simple
- **Preserve Aspect**: 建議勾選（保持圖示比例）

**RectTransform 設定：**
- 確保大小適當（例如：80x80 或 100x100）
- 確保 Anchors 設置正確
- 確保 Scale 為 (1, 1, 1)

### 3. ✅ Item 物品設定檢查

#### 檢查武器/物品的 Sprite 設定
1. 在 Scene 中找到玩家持有的武器物件（例如 Gun）
2. 選中後在 Inspector 檢查 `Item` 或 `Weapon` 組件
3. 確認 **Item Icon** 欄位是否已設置 Sprite

**如何設置：**
1. 準備一個武器圖示的 Sprite（PNG 圖片）
2. 確保 Sprite 的 Texture Type 設為 `Sprite (2D and UI)`
3. 將 Sprite 拖拽到武器物件的 `Item Icon` 欄位

### 4. ✅ Canvas 設定檢查

#### 檢查 UI Canvas
1. 找到包含 ItemSlotUI 的 Canvas
2. 確認 Canvas 設定：
   - **Render Mode**: Screen Space - Overlay（推薦）或 Screen Space - Camera
   - **Canvas Scaler**: 建議設置 Scale With Screen Size
   - **Reference Resolution**: 1920x1080（或您的目標解析度）

### 5. ✅ 層級順序檢查

#### 確認 UI 層級結構
正確的層級應該是：
```
Canvas
├── HotbarPanel
│   └── SlotsContainer
│       ├── ItemSlot (Clone)
│       │   ├── Background (Image)
│       │   ├── ItemIcon (Image) ← 這個應該在前面
│       │   ├── SelectedBorder (Image)
│       │   └── DurabilityPanel
│       │       └── DurabilityBar (Image)
│       ├── ItemSlot (Clone)
│       └── ...
```

**Sibling Index（同層順序）：**
- ItemIcon 應該在 Background 之後（顯示在背景上方）
- SelectedBorder 應該在 ItemIcon 之後（顯示在圖示上方）

### 6. ✅ 運行時調試

#### 使用新增的 Debug Log
我已經在 `ItemSlotUI.SetItem()` 方法中添加了調試訊息：

**運行遊戲後檢查 Console：**

✅ **正常情況**（圖示應該顯示）：
```
[ItemSlotUI] 設置物品圖示：手槍, Sprite: gun_icon
```

❌ **錯誤情況 1**（Item Icon Image 未設置）：
```
[ItemSlotUI] itemIcon Image 組件未設置！請在 Inspector 中檢查。
```
→ 解決：回到 Prefab，設置 Item Icon 欄位

❌ **錯誤情況 2**（物品沒有 Sprite）：
```
[ItemSlotUI] 物品 手槍 沒有設置圖示！
```
→ 解決：在武器物件的 Item 組件中設置 Item Icon Sprite

### 7. ✅ 材質和 Shader 檢查

#### 確認 Image 使用正確的材質
1. 選中 ItemIcon Image
2. 確認 Material 為 None（使用預設 UI 材質）
3. 如果使用了自訂材質，確保 Shader 為 `UI/Default`

### 8. ✅ Canvas Group 檢查

#### 檢查是否有 Canvas Group 影響
如果 ItemSlot 或其父物件有 `CanvasGroup` 組件：
- **Alpha**: 應該為 1（完全不透明）
- **Interactable**: 可以關閉
- **Block Raycasts**: 可以關閉
- **Ignore Parent Groups**: 視需求

## 快速診斷步驟

### 步驟 1：執行遊戲並檢查 Console
運行遊戲，查看是否有以下錯誤訊息：
- `[ItemSlotUI] itemIcon Image 組件未設置！` → 去步驟 2
- `[ItemSlotUI] 物品 XXX 沒有設置圖示！` → 去步驟 3
- 沒有錯誤但看不到圖示 → 去步驟 4

### 步驟 2：設置 Item Icon Image 引用
1. 打開 ItemSlot Prefab
2. 選中 ItemSlotUI 組件
3. 將 Hierarchy 中的 ItemIcon Image 拖到 `Item Icon` 欄位
4. 保存 Prefab

### 步驟 3：設置武器圖示 Sprite
1. 選中場景中的武器物件（Gun、Sword 等）
2. 在 Inspector 找到 Item/Weapon 組件
3. 設置 `Item Icon` Sprite
4. 保存場景

### 步驟 4：檢查 Image 可見性
在運行時：
1. 暫停遊戲
2. 在 Hierarchy 找到 ItemIcon Image
3. 檢查 Inspector：
   - Sprite 是否已設置？
   - Color 的 Alpha 是否為 255？
   - Enabled 是否勾選？
   - RectTransform 的 Scale 是否為 (1,1,1)？

### 步驟 5：檢查 Sprite 本身
1. 在 Project 視窗找到武器圖示的 Sprite
2. 確認 Texture Type 為 `Sprite (2D and UI)`
3. 確認 Sprite Mode 為 `Single` 或 `Multiple`
4. 點擊 Apply

## 建議的 Prefab 結構

### ItemSlot Prefab 完整結構
```
ItemSlot (GameObject)
├── ItemSlotUI (Component) ← 腳本組件
├── RectTransform
│   └── Size: 100x100
│
├── Background (Image)
│   ├── Color: 深灰色
│   └── Anchor: Stretch all
│
├── ItemIcon (Image) ← 重要！
│   ├── Color: 白色 (255,255,255,255)
│   ├── Preserve Aspect: ✓
│   ├── Size: 80x80
│   └── Anchor: Center
│
├── SelectedBorder (Image)
│   ├── Color: 黃色
│   ├── Enabled: 預設關閉
│   └── Anchor: Stretch all
│
└── DurabilityPanel (GameObject)
    ├── Canvas Group (可選)
    └── DurabilityBar (Image - Fill)
        ├── Image Type: Filled
        ├── Fill Method: Horizontal
        └── Color: 綠色
```

## 如果以上都無效

### 最後的檢查清單
- [ ] 確認 Canvas 的 Render Mode 正確
- [ ] 確認 Camera 設置正確（如果使用 Screen Space - Camera）
- [ ] 檢查是否有其他 UI 元素遮擋
- [ ] 嘗試重新創建 ItemSlot Prefab
- [ ] 檢查是否有自訂的 Layout Component 影響
- [ ] 確認沒有腳本在運行時修改 enabled 或 alpha

### 簡單測試
創建一個測試場景：
1. 創建新的 Canvas
2. 添加一個 Image
3. 手動設置 Sprite
4. 如果能顯示，說明問題在 ItemSlotUI 的設定或引用
5. 如果不能顯示，說明問題在 Unity 設置或 Sprite 本身

## 移除調試訊息

當問題解決後，可以將 `ItemSlotUI.cs` 中的 Debug.Log 語句刪除或註解掉：

```csharp
// Debug.Log($"[ItemSlotUI] 設置物品圖示：{item.ItemName}, Sprite: {item.ItemIcon.name}");
// Debug.LogWarning($"[ItemSlotUI] 物品 {item.ItemName} 沒有設置圖示！");
// Debug.LogError("[ItemSlotUI] itemIcon Image 組件未設置！請在 Inspector 中檢查。");
```

## 總結

**90% 的問題源於：**
1. ItemSlotUI 的 Item Icon 欄位沒有設置（Inspector）
2. 武器物件的 Item Icon Sprite 沒有設置
3. Image 組件的 Color Alpha 為 0 或 enabled 為 false

**確保這三點設置正確，問題通常就能解決！**

