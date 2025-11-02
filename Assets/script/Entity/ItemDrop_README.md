# 實體死亡物品掉落系統

## 功能概述

當敵人（Enemy）死亡時，會自動將 `ItemHolder` 中的所有物品掉落到場景中，並由 `ItemManager` 接管這些物品。

**注意：目前只有 Enemy 會掉落物品，Player 死亡時不會掉落物品（可根據需求修改）。**

## 系統架構

### 1. ItemHolder 增強功能

**新增功能：**
- 追蹤每個物品對應的原始 Prefab（使用 `Dictionary<Item, GameObject>`）
- 提供獲取所有物品及其 Prefab 的方法
- 提供清空所有物品的方法

**主要新增方法：**

```csharp
// 獲取物品對應的原始 Prefab
public GameObject GetItemPrefab(Item item)

// 獲取所有物品及其對應的 Prefab（用於掉落）
public List<KeyValuePair<Item, GameObject>> GetAllItemsWithPrefabs()

// 清空所有物品（用於死亡時掉落）
public void ClearAllItems()
```

### 2. ItemManager 掉落功能

**新增功能：**
- 在指定位置生成掉落物品
- 支援單個物品掉落
- 支援多個物品圓形散落

**主要新增方法：**

```csharp
// 在指定位置掉落單個物品
public WorldItem DropItemAtPosition(GameObject itemPrefab, Vector3 position)

// 在指定位置掉落多個物品（圓形散落）
public List<WorldItem> DropItemsAtPosition(List<GameObject> itemPrefabs, Vector3 position, float spreadRadius = 1.5f)
```

**掉落邏輯：**
- 物品會以圓形方式散落在死亡位置周圍
- 預設散落半徑為 1.5 單位
- 每個物品均勻分佈在圓周上

### 3. BaseEntity 死亡處理

**修改內容：**
- `OnDeath()` 方法保持為虛擬方法，供子類別覆寫
- 新增 `DropAllItems()` 方法（protected），提供掉落物品的功能
- 子類別（如 Enemy）可以在 `OnDeath()` 中調用 `DropAllItems()`

**死亡流程（以 Enemy 為例）：**

1. Enemy 死亡時調用 `Die()` 方法
2. `Die()` 方法調用 `OnDeath()`
3. `Enemy.OnDeath()` 調用 `DropAllItems()`
4. `DropAllItems()` 執行以下步驟：
   - 檢查是否有 `ItemHolder` 和物品
   - 尋找場景中的 `ItemManager`
   - 獲取所有物品的 Prefab
   - 調用 `ItemManager` 掉落物品
   - 清空 `ItemHolder` 的物品列表

### 4. Enemy 死亡處理

**實現：**
- 覆寫 `OnDeath()` 方法
- 在 `OnDeath()` 中調用 `DropAllItems()` 來掉落物品

**代碼示例：**
```csharp
protected override void OnDeath()
{
    // 掉落所有物品
    DropAllItems();
    
    // Enemy 特定的死亡邏輯...
}
```

### 5. Player 死亡處理

**實現：**
- 覆寫 `OnDeath()` 方法
- 目前為空實現，Player 死亡時**不會**掉落物品

**如需讓 Player 掉落物品：**
```csharp
protected override void OnDeath()
{
    // 掉落所有物品
    DropAllItems();
    
    // Player 特定的死亡邏輯...
}
```

## 使用方式

### 基本使用

系統會自動運作，不需要額外配置。當 Enemy 死亡時：

1. Enemy 會自動掉落所有持有的物品
2. 物品會以圓形散落在死亡位置周圍
3. 掉落的物品可以被重新撿取

**Player 死亡時不會掉落物品**（除非手動在 `Player.OnDeath()` 中添加 `DropAllItems()` 調用）

### 自訂死亡行為

如果子類別需要自訂死亡行為，可以覆寫 `OnDeath()` 方法：

```csharp
protected override void OnDeath()
{
    // 先執行基類的物品掉落邏輯
    base.OnDeath();
    
    // 添加自訂的死亡行為
    PlayDeathAnimation();
    SpawnDeathEffect();
}
```

### 自訂掉落邏輯

如果需要自訂物品掉落邏輯，可以覆寫 `DropAllItems()` 方法：

```csharp
protected override void DropAllItems()
{
    // 自訂掉落邏輯
    // 例如：只掉落某些類型的物品
    // 或者改變掉落位置和散落方式
}
```

## 範例場景設定

### 必要組件：

1. **場景中必須有 ItemManager：**
   - 在場景中創建一個空物件並添加 `ItemManager` 組件
   - 配置 `worldItemPrefab`（WorldItem 的 Prefab）
   - 配置物品類型對應表（Item Prefab Mappings）

2. **實體必須有 ItemHolder：**
   - Player 和 Enemy 物件上必須有 `ItemHolder` 組件
   - 在 Inspector 中配置初始物品 Prefabs

### 測試步驟：

1. 確保場景中有 `ItemManager`
2. 確保 Enemy 有 `ItemHolder` 並配置了物品
3. 讓 Enemy 進入死亡狀態（例如生命值歸零）
4. 觀察物品是否正確掉落在死亡位置周圍
5. 嘗試撿取掉落的物品

## 注意事項

### 1. ItemManager 必須存在

如果場景中沒有 `ItemManager`，會顯示警告訊息：
```
[Entity Name]: Cannot drop items - ItemManager not found in scene!
```

解決方法：在場景中添加包含 `ItemManager` 組件的 GameObject。

### 2. 物品 Prefab 追蹤

系統會自動追蹤每個物品對應的原始 Prefab。這在以下情況下發生：
- 在 `Start()` 時從 `itemPrefabs` 陣列實例化物品
- 使用 `AddItemFromPrefab()` 添加物品（例如撿取物品時）

### 3. 掉落位置

物品會以圓形方式散落在實體的 `transform.position` 周圍：
- 預設散落半徑：1.5 單位
- 物品均勻分佈在圓周上
- 可以通過修改 `DropAllItems()` 方法來自訂散落範圍

### 4. 性能考量

- 使用 `FindObjectOfType<ItemManager>()` 會在每次死亡時搜索場景
- 如果有大量實體頻繁死亡，建議將 `ItemManager` 快取為靜態引用或單例

**優化建議：**

可以在 `ItemManager` 中添加單例模式：

```csharp
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

然後在 `BaseEntity.DropAllItems()` 中使用：

```csharp
ItemManager itemManager = ItemManager.Instance;
```

## 除錯

### 啟用除錯訊息

在 `ItemManager` 的 Inspector 中：
- 勾選 "Show Debug Info" 可以看到掉落相關的 Debug.Log 訊息

### 常見問題

**Q: 物品沒有掉落？**
A: 檢查以下項目：
1. 場景中是否有 `ItemManager`
2. 實體是否有 `ItemHolder` 組件
3. `ItemHolder` 中是否有物品
4. Console 中是否有錯誤訊息

**Q: 掉落的物品無法撿取？**
A: 確認：
1. `ItemManager` 中配置了正確的 `worldItemPrefab`
2. `WorldItem` 有 `CircleCollider2D` 組件
3. 撿取系統正常運作

**Q: 物品掉落位置不正確？**
A: 可以修改 `DropItemsAtPosition()` 方法中的 `spreadRadius` 參數

## 未來擴展

可能的擴展方向：

1. **掉落機率系統：** 每個物品有一定機率掉落
2. **掉落動畫：** 添加物品彈跳、飛濺效果
3. **稀有度系統：** 根據物品稀有度決定掉落特效
4. **掉落音效：** 為不同類型的物品添加掉落音效
5. **自動拾取：** 掉落物品自動飛向附近的玩家

## 版本記錄

### v1.0 (2025-11-02)
- 初始版本
- 實現基本的死亡掉落功能
- 支援圓形散落
- 整合 ItemHolder 和 ItemManager

