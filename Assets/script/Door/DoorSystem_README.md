# 簡化門系統使用說明

## 概述
這是一個簡化的門系統，允許玩家通過按鍵來開啟/刪除Tilemap中的門。系統直接操作Tilemap，無需複雜的門物件管理。

## 設置步驟

### 1. 設置DoorController
1. 在場景中創建一個空的GameObject，命名為"DoorController"
2. 添加 `DoorController` 腳本
3. 在Inspector中設置以下參數：
   - **Tilemap**: 包含門的Tilemap
   - **Door Tile**: 門的Tile

### 2. 設置Tilemap
1. 確保您的Tilemap中有門的Tile
2. 門的Tile應該放在指定的Tilemap中

### 3. 輸入設置
系統使用Input System，確保在Input Actions中有一個"Open"動作。

## 使用方法

### 玩家操作
- 玩家面向門的方向
- 按下設定的"Open"按鍵
- 系統會自動檢測玩家前方1.5單位距離的門並刪除它
- **智能刪除**：會同時刪除所有相鄰的門tile（支持多tile門）

### 程式化控制
```csharp
// 刪除指定世界位置的門
DoorController.Instance.RemoveDoorAtWorldPosition(new Vector3(10.5f, 6.2f, 0f));
```

## API 參考

### DoorController 主要方法
- `RemoveDoorAtWorldPosition(Vector3 worldPosition)` - 刪除指定世界位置的門

## 特點
- **極簡設計**: 只需要2個參數設置
- **高效能**: 直接操作Tilemap，無額外開銷
- **易於使用**: 只有1個主要方法
- **智能刪除**: 使用深度優先搜索算法，自動刪除所有相連的門tile
- **多tile支持**: 完美支持由多個tile組成的大型門

## 注意事項
1. 確保DoorController在場景中只有一個實例
2. 門的Tile必須正確設置在指定的Tilemap中
3. 門的刪除是永久性的，請謹慎使用

## 調試
- 查看Console輸出以了解門的操作狀態
- 使用Debug.Log來追蹤門的位置和狀態
- 確保所有必要的組件都已正確設置
