# 3D Door Visual System - Setup Guide

這個系統允許你為 2D Tilemap 門添加 3D 視覺效果和動畫。所有的門邏輯（檢測、鑰匙檢查等）仍然由 `DoorController` 處理，3D 門只負責視覺動畫。

## 工作原理

1. 玩家按下互動鍵（E）
2. `DoorController` 檢查附近是否有 Tilemap 門
3. 檢查鑰匙（如果需要）
4. 刪除 Tilemap 上的門 tiles（門保持開啟）
5. **自動觸發對應位置的 3D 門動畫**

## 腳本概述

### 1. Door3DVisual.cs
- 連接 Tilemap 門位置和 3D 門模型
- 當 DoorController 開啟門時，自動播放 3D 動畫
- 門開啟後保持開啟狀態（與 2D 邏輯一致）

### 2. SlidingDoor3DAnimation.cs
- 專門用於滑動門（側向移動）
- 適合你的 vitrocsa3001 滑動門模型
- 使用位置插值實現平滑滑動

### 3. SimpleDoor3DAnimation.cs
- 用於旋轉門（例如普通房門）
- 使用旋轉插值實現開門效果

## 設定步驟

### 步驟 1：準備 3D 門模型

1. 在場景中放置你的 3D 門模型（例如 vitrocsa3001）
2. 確保門的結構正確：
   ```
   DoorParent (空物件，用於定位)
   └─ DoorMesh (實際的門模型，會移動/旋轉的部分)
   ```

### 步驟 2：添加腳本到門

#### 方法 A：使用 SlidingDoor3DAnimation（滑動門）

```
DoorParent
├─ Door3DVisual (腳本)
│   ├─ Tilemap World Position: 設定為對應的 2D 門位置
│   └─ Sliding Door Animation: 拖入下面的組件
└─ SlidingDoor3DAnimation (腳本)
    ├─ Door Transform: 拖入 DoorMesh
    ├─ Slide Distance: 2.0 (滑動距離)
    ├─ Slide Direction: (1, 0, 0) 向右或 (-1, 0, 0) 向左
    └─ Animation Duration: 1.0 (動畫時長)
```

#### 方法 B：使用 SimpleDoor3DAnimation（旋轉門）

```
DoorParent
├─ Door3DVisual (腳本)
│   ├─ Tilemap World Position: 設定為對應的 2D 門位置
│   └─ Simple Door Animation: 拖入下面的組件
└─ SimpleDoor3DAnimation (腳本)
    ├─ Door Transform: 拖入 DoorMesh
    ├─ Open Angle: 90 (開門角度)
    ├─ Rotation Axis: (0, 1, 0) Y 軸向上
    └─ Animation Duration: 1.0 (動畫時長)
```

#### 方法 C：使用 Animator（複雜動畫）

如果你已經有 Animator Controller 設定好的動畫：

```
DoorParent
├─ Door3DVisual (腳本)
│   ├─ Tilemap World Position: 設定為對應的 2D 門位置
│   └─ Door Animator: 拖入 Animator 組件
└─ Animator (組件)
    └─ Controller: 你的 Animator Controller
```

你的 Animator Controller 需要有一個 **Trigger 參數叫 "Open"**。

### 步驟 3：設定 Tilemap 位置

這是**最重要的一步**！

1. 在場景中找到對應的 2D Tilemap 門
2. 記錄它的世界座標位置
3. 在 `Door3DVisual` 的 `Tilemap World Position` 中設定這個位置

**如何找到 Tilemap 門的位置：**
- 在 Scene 視圖中選擇 Tilemap
- 開啟 Tile Palette
- 找到門的位置
- 或者在遊戲中嘗試開門，查看 Console 的日誌輸出座標

**小技巧：** 選擇 3D 門物件後，在 Scene 視圖中會顯示一條青色線連接到 Tilemap 位置，方便你確認對齊。

## 滑動門專用設定（vitrocsa3001）

對於你的滑動門模型：

```
SlidingDoor3DAnimation 設定：
- Door Transform: 門的網格物件
- Slide Distance: 2.0 到 3.0（根據門的大小調整）
- Slide Direction: 
  * (1, 0, 0) = 向右滑動
  * (-1, 0, 0) = 向左滑動
  * (0, 0, 1) = 向前滑動
  * (0, 0, -1) = 向後滑動
- Animation Duration: 1.0 到 1.5 秒
- Slide Curve: EaseInOut（預設）
```

## 範例配置

### 範例 1：右側滑動的玻璃門
```
Door3DVisual:
  - Tilemap World Position: (10.5, 5.5, 0)

SlidingDoor3DAnimation:
  - Door Transform: GlassDoorMesh
  - Slide Distance: 2.5
  - Slide Direction: (1, 0, 0)
  - Animation Duration: 1.2
```

### 範例 2：左側滑動的門（需要紅色鑰匙）
```
Door3DVisual:
  - Tilemap World Position: (15.5, 8.5, 0)

SlidingDoor3DAnimation:
  - Door Transform: DoorMesh
  - Slide Distance: 2.0
  - Slide Direction: (-1, 0, 0)
  - Animation Duration: 1.0
```

注意：鑰匙邏輯在 `DoorController` 中的 Tilemap 門設定，不需要在 3D 門上設定！

## 調試技巧

### 1. 檢查位置對齊

選擇 3D 門物件，在 Scene 視圖中會看到：
- 青色球體：表示 Tilemap 門的位置
- 青色線：連接 3D 門和 Tilemap 門位置

如果位置不對齊，調整 `Tilemap World Position`。

### 2. 測試動畫

如果想測試動畫效果（不需要開門）：
```csharp
// 在 Inspector 中找到 Door3DVisual 組件
// 在運行時調用 PlayOpenAnimation() 方法
GetComponent<Door3DVisual>().PlayOpenAnimation();
```

或者在 SlidingDoor3DAnimation 組件上調用 `Open()`。

### 3. 檢查 Console 日誌

成功開門時會顯示：
```
[DoorController] 使用 xxx 鑰匙開啟門在位置 (x, y)
[Door3DVisual] 門 DoorName 開啟（使用 SlidingDoor3DAnimation）
```

### 4. 常見問題

**問題：門沒有動畫**
- 檢查 `Tilemap World Position` 是否正確
- 檢查 `Door Transform` 是否有指定
- 檢查是否有動畫組件（SlidingDoor3DAnimation 或其他）

**問題：門滑動方向錯誤**
- 調整 `Slide Direction` 的正負號
- 例如改 (1, 0, 0) 為 (-1, 0, 0)

**問題：門滑動距離太短/太長**
- 調整 `Slide Distance` 數值
- 建議從 2.0 開始，根據實際效果調整

**問題：3D 門和 2D 門位置不匹配**
- 使用 Scene 視圖中的青色線確認位置
- 容差範圍是 1.0 單位，如果超過這個距離不會觸發

## 進階技巧

### 同時開啟多個 3D 門

如果一個 2D 門對應多個 3D 門（例如雙開門）：
- 創建兩個 3D 門物件
- 都添加 `Door3DVisual` 組件
- 設定相同的 `Tilemap World Position`
- 當玩家開門時，兩個 3D 門都會播放動畫

### 調整動畫曲線

在 `SlidingDoor3DAnimation` 或 `SimpleDoor3DAnimation` 中：
- `Slide Curve` / `Open Curve` 可以自訂動畫曲線
- 預設是 EaseInOut（先加速後減速）
- 可以改成 Linear（等速）或自訂曲線

### 添加音效

在動畫組件的 `Update()` 方法中添加：
```csharp
if (animationProgress == 0f && isAnimating)
{
    // 播放開門音效
    AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
}
```

## 工作流程總結

1. 在 Scene 中放置 3D 門模型
2. 添加 `Door3DVisual` 和動畫組件（SlidingDoor3DAnimation）
3. 設定 Tilemap 門的世界位置
4. 配置滑動參數（距離、方向、速度）
5. 測試遊戲，按 E 開門

**就是這樣！** 所有的門邏輯都由現有的 `DoorController` 處理，3D 門只是視覺效果。

## 系統架構

```
Player 按 E
    ↓
DoorController (檢查附近的門)
    ↓
檢查鑰匙（如果需要）
    ↓
刪除 Tilemap 門 tiles
    ↓
觸發 Door3DVisual.PlayOpenAnimation()
    ↓
播放 SlidingDoor3DAnimation 或其他動畫
    ↓
門保持開啟狀態 ✓
```

這種設計的好處：
- ✅ 所有邏輯集中在 DoorController
- ✅ 3D 門純粹是視覺效果
- ✅ 與現有系統完全相容
- ✅ 容易維護和調試
