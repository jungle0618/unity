# ItemHolder 方法使用說明

## 重要：EquipFromPrefab vs AddItemFromPrefab

在使用 ItemHolder 時，有兩個看起來相似但**功能完全不同**的方法。選擇錯誤的方法會導致物品掉落系統失效。

---

## 方法對比

### `AddItemFromPrefab(GameObject prefab)` ✅ 推薦用於動態添加物品

**用途：** 將物品加入到背包/物品欄

**功能：**
1. ✅ 實例化物品
2. ✅ 加入到 `availableItems` 列表
3. ✅ 建立 `itemToPrefabMap` 對應（用於掉落）
4. ✅ 如果是第一個物品，自動裝備
5. ✅ 如果已有物品，加入列表但不裝備

**適用場景：**
- ✅ 撿取物品
- ✅ 敵人生成時裝備物品（EnemyManager）
- ✅ 給予玩家新物品
- ✅ 任何需要"獲得"物品的情況

**ItemCount 影響：** 會增加 ItemCount

**死亡掉落：** ✅ 可以正確掉落

**範例：**
```csharp
// EnemyManager 中為敵人裝備物品
Item item = enemy.ItemHolder.AddItemFromPrefab(knifePrefa b);

// PlayerManager 中給玩家新物品
Item item = player.ItemHolder.AddItemFromPrefab(keyPrefab);
```

---

### `EquipFromPrefab(GameObject prefab)` ⚠️ 僅用於替換當前物品

**用途：** 替換當前裝備的物品（不保留在列表中）

**功能：**
1. ✅ 實例化物品
2. ❌ **不會**加入到 `availableItems` 列表
3. ❌ **不會**建立 `itemToPrefabMap` 對應
4. ✅ 設定為 `currentItem`
5. ✅ 銷毀舊的 `currentItem`

**適用場景：**
- ⚠️ 臨時替換武器（但會失去原武器）
- ⚠️ 強制替換當前物品
- ⚠️ **不推薦日常使用**

**ItemCount 影響：** **不會**增加 ItemCount

**死亡掉落：** ❌ **無法掉落**（因為不在列表中）

**範例：**
```csharp
// 不推薦：這樣敵人死亡時無法掉落物品
Weapon weapon = enemy.ItemHolder.EquipFromPrefab(swordPrefab); // ❌
```

---

## 常見錯誤案例

### ❌ 錯誤：使用 EquipFromPrefab 為敵人裝備物品

```csharp
// EnemyManager.cs - 錯誤做法
foreach (string itemName in enemyData.itemNames)
{
    GameObject itemPrefab = itemNameToPrefab[itemName];
    enemy.ItemHolder.EquipFromPrefab(itemPrefab); // ❌ 錯誤！
}
```

**問題：**
- Enemy 看起來有武器（可以攻擊）
- 但 `ItemHolder.ItemCount` 為 0
- 死亡時無法掉落物品
- `GetAllItemsWithPrefabs()` 返回空列表

---

### ✅ 正確：使用 AddItemFromPrefab 為敵人裝備物品

```csharp
// EnemyManager.cs - 正確做法
foreach (string itemName in enemyData.itemNames)
{
    GameObject itemPrefab = itemNameToPrefab[itemName];
    Item item = enemy.ItemHolder.AddItemFromPrefab(itemPrefab); // ✅ 正確！
}
```

**結果：**
- ✅ Enemy 有武器可以攻擊
- ✅ `ItemHolder.ItemCount` 正確反映物品數量
- ✅ 死亡時可以正確掉落物品
- ✅ `GetAllItemsWithPrefabs()` 返回正確的物品列表

---

## 方法功能詳細對比表

| 功能 | `AddItemFromPrefab` | `EquipFromPrefab` |
|------|---------------------|-------------------|
| 實例化物品 | ✅ | ✅ |
| 加入 availableItems 列表 | ✅ | ❌ |
| 建立 itemToPrefabMap | ✅ | ❌ |
| 設定為 currentItem | ✅ (如果是第一個) | ✅ |
| 可以切換到其他物品 | ✅ | ❌ |
| 影響 ItemCount | ✅ 增加 | ❌ 不變 |
| 死亡時可掉落 | ✅ | ❌ |
| 保留原有物品 | ✅ | ❌ 銷毀 |
| 觸發 OnItemChanged 事件 | ✅ | ❌ |
| 適用於撿取 | ✅ | ❌ |
| 適用於敵人生成 | ✅ | ❌ |
| 適用於切換物品 | ❌ | ❌ |

---

## 其他相關方法

### `SwitchToItem(int index)` - 切換到指定索引的物品
```csharp
// 切換到第二個物品
itemHolder.SwitchToItem(1);
```

### `SwitchToNextItem()` - 切換到下一個物品
```csharp
// 循環切換到下一個
itemHolder.SwitchToNextItem();
```

### `RemoveItem(Item item)` - 移除指定物品
```csharp
// 移除當前物品
itemHolder.RemoveItem(itemHolder.CurrentItem);
```

### `ClearAllItems()` - 清空所有物品
```csharp
// 清空背包（在重新裝備前使用）
itemHolder.ClearAllItems();
```

---

## 最佳實踐

### 1. 敵人生成時裝備物品

```csharp
// ✅ 正確做法
public void SpawnEnemy(EnemyData data)
{
    Enemy enemy = GetPooledEnemy();
    
    // 先清空舊物品
    enemy.ItemHolder.ClearAllItems();
    
    // 添加新物品
    foreach (string itemName in data.itemNames)
    {
        GameObject prefab = GetItemPrefab(itemName);
        enemy.ItemHolder.AddItemFromPrefab(prefab); // ✅
    }
}
```

### 2. 玩家撿取物品

```csharp
// ✅ 正確做法
public void PickupItem(GameObject itemPrefab)
{
    Item item = player.ItemHolder.AddItemFromPrefab(itemPrefab);
    
    if (item != null)
    {
        Debug.Log($"撿取了 {item.ItemName}");
    }
}
```

### 3. 測試用途（臨時裝備）

```csharp
// ⚠️ 僅用於測試，不要在正式代碼中使用
[ContextMenu("Test: Equip Temporary Weapon")]
private void TestEquipTemporaryWeapon()
{
    // 這個武器不會被保存，死亡時也不會掉落
    itemHolder.EquipFromPrefab(testWeaponPrefab);
}
```

---

## 檢查清單

在使用 ItemHolder 時，請確保：

- [ ] 所有動態添加物品的地方都使用 `AddItemFromPrefab()`
- [ ] EnemyManager 使用 `AddItemFromPrefab()` 為敵人裝備物品
- [ ] 撿取系統使用 `AddItemFromPrefab()`
- [ ] 沒有錯誤使用 `EquipFromPrefab()` 的地方
- [ ] 需要掉落的物品都在 `availableItems` 列表中

---

## 故障排除

### 問題：敵人死亡時沒有掉落物品

**檢查步驟：**
1. 在 Inspector 中選擇敵人
2. 查看 ItemHolder 組件
3. 檢查 ItemCount 是否為 0

**如果 ItemCount = 0：**
- ❌ 使用了 `EquipFromPrefab()`
- ✅ 應該使用 `AddItemFromPrefab()`

**如果 ItemCount > 0：**
- 檢查是否有 ItemManager
- 檢查 ItemManager 的設定

### 問題：物品添加後無法切換

**原因：** 使用了 `EquipFromPrefab()`
- `EquipFromPrefab()` 不會將物品加入列表
- 無法使用 `SwitchToNextItem()` 切換

**解決：** 使用 `AddItemFromPrefab()` 重新添加物品

---

## 總結

| 使用場景 | 推薦方法 |
|---------|---------|
| 敵人生成時裝備物品 | `AddItemFromPrefab()` ✅ |
| 玩家撿取物品 | `AddItemFromPrefab()` ✅ |
| 給予獎勵物品 | `AddItemFromPrefab()` ✅ |
| 切換到其他物品 | `SwitchToItem()` / `SwitchToNextItem()` ✅ |
| 測試臨時物品 | `EquipFromPrefab()` ⚠️ |
| 清空背包 | `ClearAllItems()` ✅ |
| 移除單個物品 | `RemoveItem()` ✅ |

**記住：** 
- 🎯 需要保留物品 → 使用 `AddItemFromPrefab()`
- 🎯 需要掉落物品 → 使用 `AddItemFromPrefab()`
- 🎯 任何正式用途 → 使用 `AddItemFromPrefab()`
- ⚠️ 臨時測試 → 可以使用 `EquipFromPrefab()`




