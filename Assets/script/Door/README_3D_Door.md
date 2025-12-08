# 3D Door Animation System

這個系統允許你為 3D 門物件添加開啟/關閉動畫，支援鑰匙系統和玩家互動。

## Tilemap settings
### Vertical door
- Offset
   x 1.5 y 0 z 0.5
- Scale
   x 0.6 y 0.5 z 1
- Orientation
   x 0 y 270 z 90
### Horizontal door
I forgot but I think Orientation was 270, 0, 0
And +-0.5 for y for different facing doors.

## 腳本概述

### 1. Door3DInteractable.cs
主要的門互動控制器，負責處理：
- 玩家與門的互動
- 鑰匙檢查和驗證
- 門的開啟/關閉邏輯
- 支援兩種動畫方式：Animator 或 SimpleDoor3DAnimation

### 2. SimpleDoor3DAnimation.cs
簡單的門動畫控制器，使用旋轉來實現門的開啟/關閉：
- 不需要 Animator Controller
- 使用程式碼控制門的旋轉
- 支援自訂動畫曲線和速度

## 使用方法

### 方法 A：使用 Animator（推薦用於複雜動畫）

1. **準備門的 3D 模型**
   - 確保門的鉸鏈點（Pivot）設定正確
   - 門應該能夠圍繞鉸鏈旋轉

2. **設定 Animator Controller**
   - 在 Unity 中創建一個 Animator Controller
   - 添加以下參數：
     - `Open` (Trigger)：觸發開門動畫
     - `Close` (Trigger)：觸發關門動畫
     - `IsOpen` (Bool)：門是否開啟（可選）
   - 創建開門和關門的動畫狀態
   - 設定狀態之間的轉換

3. **添加腳本到門物件**
   ```
   GameObject（門）
   ├─ Door3DInteractable（腳本）
   │   ├─ Door Animator：拖入門的 Animator 組件
   │   ├─ Requires Key：是否需要鑰匙
   │   ├─ Required Key Type：需要的鑰匙類型
   │   └─ Interaction Range：互動範圍
   └─ Animator（組件）
       └─ Controller：你創建的 Animator Controller
   ```

### 方法 B：使用 SimpleDoor3DAnimation（簡單快速）

1. **準備門的 3D 模型**
   - 門的結構應該像這樣：
     ```
     GameObject（門的父物件）
     └─ DoorMesh（門的網格物件）
     ```
   - 確保門的鉸鏈點（Pivot）設定正確

2. **添加腳本**
   ```
   GameObject（門）
   ├─ Door3DInteractable（腳本）
   │   ├─ Simple Door Animation：拖入 SimpleDoor3DAnimation 組件
   │   ├─ Requires Key：是否需要鑰匙
   │   ├─ Required Key Type：需要的鑰匙類型
   │   └─ Interaction Range：互動範圍
   └─ SimpleDoor3DAnimation（腳本）
       ├─ Door Transform：拖入門的網格物件（DoorMesh）
       ├─ Open Angle：開門角度（例如 90）
       ├─ Animation Duration：動畫時長（秒）
       ├─ Open Curve：動畫曲線
       └─ Rotation Axis：旋轉軸（預設 Y 軸向上）
   ```

3. **調整參數**
   - `Open Angle`：門開啟的角度（90 度表示完全打開）
   - `Animation Duration`：動畫持續時間（例如 1 秒）
   - `Rotation Axis`：門的旋轉軸
     - `(0, 1, 0)` = Y 軸（向上）- 適用於大多數門
     - `(1, 0, 0)` = X 軸（向右）
     - `(0, 0, 1)` = Z 軸（向前）

## 設定範例

### 範例 1：普通門（不需要鑰匙）
```
Door3DInteractable:
  - Requires Key: false
  - Interaction Range: 2.0

SimpleDoor3DAnimation:
  - Open Angle: 90
  - Animation Duration: 1.0
  - Rotation Axis: (0, 1, 0)
```

### 範例 2：需要紅色鑰匙的門
```
Door3DInteractable:
  - Requires Key: true
  - Required Key Type: Red
  - Interaction Range: 2.0

SimpleDoor3DAnimation:
  - Open Angle: 90
  - Animation Duration: 1.0
  - Rotation Axis: (0, 1, 0)
```

### 範例 3：滑動門（使用 X 軸旋轉）
```
SimpleDoor3DAnimation:
  - Open Angle: 90
  - Animation Duration: 1.5
  - Rotation Axis: (1, 0, 0)
```

## 玩家互動

玩家腳本（Player.cs）已經自動整合了 3D 門系統：
- 按下互動鍵（預設是 E 鍵）
- 系統會優先檢查附近的 3D 門
- 如果沒有 3D 門，會檢查 Tilemap 門
- 如果門需要鑰匙，會自動從背包中查找對應的鑰匙

## 調試技巧

1. **在 Scene 視圖中可視化互動範圍**
   - 選擇門物件
   - 在 Scene 視圖中會顯示黃色的互動範圍球體

2. **檢查日誌輸出**
   - 開啟 Console 視窗查看調試信息
   - 成功開門會顯示：`[Door3DInteractable] 門 XXX 開啟`
   - 如果需要鑰匙會顯示提示訊息

3. **常見問題**
   - 門沒有旋轉？檢查 `Door Transform` 是否正確設定
   - 門旋轉方向錯誤？調整 `Rotation Axis` 或改變 `Open Angle` 的正負號
   - 玩家無法互動？檢查 `Interaction Range` 是否足夠大

## Animator 參數說明

如果使用 Animator，你的 Animator Controller 需要以下參數：

| 參數名稱 | 類型 | 用途 |
|---------|------|------|
| `Open` | Trigger | 觸發開門動畫 |
| `Close` | Trigger | 觸發關門動畫 |
| `IsOpen` | Bool | 表示門是否開啟（可選） |

## 進階功能

### 自訂開門邏輯
你可以繼承 `Door3DInteractable` 來實現自訂的開門邏輯：

```csharp
public class CustomDoor : Door3DInteractable
{
    protected override bool TryUnlockDoor(ItemHolder itemHolder)
    {
        // 自訂解鎖邏輯
        return base.TryUnlockDoor(itemHolder);
    }
}
```

### 觸發事件
你可以在門開啟/關閉時觸發其他事件：

```csharp
// 在 Door3DInteractable 中添加事件
public event System.Action OnDoorOpened;
public event System.Action OnDoorClosed;

// 在 OpenDoor() 和 CloseDoor() 中調用
OnDoorOpened?.Invoke();
OnDoorClosed?.Invoke();
```

## 效能建議

- 如果場景中有很多門，考慮使用 Object Pooling
- SimpleDoor3DAnimation 比 Animator 更輕量，適合簡單場景
- 對於複雜的門動畫（多個部件），建議使用 Animator

## 相容性

- Unity 2022.3 或更高版本
- 與現有的 Tilemap 門系統完全相容
- 支援所有鑰匙類型（Red, Blue, Green, Yellow, Purple, Orange, White, Black）
