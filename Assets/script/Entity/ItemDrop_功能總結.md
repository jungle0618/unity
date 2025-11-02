# 實體死亡物品掉落功能 - 實現總結

## 📋 功能描述

當敵人死亡時，會自動將 `ItemHolder` 中的所有物品掉落到場景中，這些物品會以圓形方式散落在死亡位置周圍，並可以被重新撿取。

**注意：目前只有 Enemy 會掉落物品，Player 死亡時不會掉落物品。**

## ✅ 已完成的修改

### 1. ItemHolder.cs
- ✅ 添加 `Dictionary<Item, GameObject> itemToPrefabMap` 追蹤每個物品對應的原始 Prefab
- ✅ 在 `Start()` 和 `AddItemFromPrefab()` 中記錄物品與 Prefab 的對應關係
- ✅ 新增 `GetItemPrefab(Item item)` 方法：獲取物品對應的 Prefab
- ✅ 新增 `GetAllItemsWithPrefabs()` 方法：獲取所有物品及其 Prefab 列表
- ✅ 新增 `ClearAllItems()` 方法：清空所有物品（用於死亡時）

### 2. ItemManager.cs
- ✅ 新增 `DropItemAtPosition()` 方法：在指定位置掉落單個物品
- ✅ 新增 `DropItemsAtPosition()` 方法：在指定位置掉落多個物品（圓形散落）
- ✅ 掉落的物品會自動加入到 `spawnedItems` 列表中
- ✅ 支援自訂散落半徑（預設 1.5 單位）

### 3. BaseEntity.cs
- ✅ `OnDeath()` 方法：保持為虛擬方法，供子類別覆寫
- ✅ 新增 `DropAllItems()` 方法（protected）：提供掉落物品的功能
  - 檢查是否有物品需要掉落
  - 尋找場景中的 `ItemManager`
  - 獲取所有物品的 Prefab
  - 調用 `ItemManager` 掉落物品
  - 清空 `ItemHolder` 的物品列表

### 4. Enemy.cs
- ✅ 覆寫 `OnDeath()` 方法：調用 `DropAllItems()` 來掉落物品
- ✅ Enemy 死亡時會自動掉落所有持有的物品

### 5. Player.cs
- ✅ 覆寫 `OnDeath()` 方法：目前為空實現
- ✅ Player 死亡時**不會**掉落物品（可以根據需求修改）

## 🎮 使用方式

### Enemy 自動掉落
Enemy 死亡時會自動掉落物品，無需額外配置：
1. 當 Enemy 死亡時，自動觸發掉落邏輯
2. 物品以圓形散落在死亡位置周圍
3. 掉落的物品可以被重新撿取

### 前置條件
1. 場景中必須有 `ItemManager` 組件
2. Enemy 必須有 `ItemHolder` 組件並配置了物品

### 讓 Player 也掉落物品（可選）
如果需要讓 Player 死亡時也掉落物品，在 `Player.cs` 的 `OnDeath()` 方法中添加：
```csharp
protected override void OnDeath()
{
    // 掉落所有物品
    DropAllItems();
    
    // Player 特定的死亡邏輯...
}
```

## 📊 技術細節

### 物品散落算法
```csharp
// 圓形散落：物品均勻分佈在圓周上
float angle = (360f / itemCount) * i * Mathf.Deg2Rad;
Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spreadRadius;
Vector3 dropPosition = centerPosition + offset;
```

### 流程圖（以 Enemy 為例）
```
Enemy 死亡 (Die)
    ↓
Enemy.OnDeath() 被調用
    ↓
調用 DropAllItems()（繼承自 BaseEntity）
    ↓
獲取 ItemHolder 中的所有物品和 Prefab
    ↓
調用 ItemManager.DropItemsAtPosition()
    ↓
在死亡位置生成 WorldItem（圓形散落）
    ↓
清空 ItemHolder 的物品列表
```

## 📝 相關文件

- `ItemDrop_README.md`：完整的使用說明和除錯指南
- `Todo.md`：已更新，記錄了新功能的完成狀態

## 🔍 測試建議

1. 創建一個有多個物品的實體
2. 讓實體進入死亡狀態
3. 觀察物品是否正確掉落
4. 確認掉落的物品可以被重新撿取
5. 檢查 Console 中的 Debug.Log 訊息

## ⚡ 性能優化建議

如果遊戲中有大量實體頻繁死亡，建議將 `ItemManager` 改為單例模式，避免每次都使用 `FindObjectOfType<ItemManager>()`。

## 📅 完成日期

2025年11月2日

---

**狀態：✅ 已完成並通過編譯檢查**

