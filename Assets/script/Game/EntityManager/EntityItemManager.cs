using UnityEngine;
using System.Collections.Generic;

namespace Game.EntityManager
{
    /// <summary>
    /// 實體物品管理器
    /// 職責：管理物品名稱到 Prefab 的映射，為實體裝備物品
    /// </summary>
    [System.Serializable]
    public class ItemMapping
    {
        [Tooltip("物品名稱或類型（用於敵人生成和世界物品生成）")]
        public string itemName;

        [Tooltip("對應的 Item Prefab（必須包含 Item 組件）")]
        public GameObject itemPrefab;
    }

    public class EntityItemManager
    {
        private Dictionary<string, GameObject> itemNameToPrefab = new Dictionary<string, GameObject>();
        private bool showDebugInfo = false;

        // 公共 API：供 ItemManager 使用
        public Dictionary<string, GameObject> ItemMappingDict => itemNameToPrefab;

        public EntityItemManager(bool showDebugInfo = false)
        {
            this.showDebugInfo = showDebugInfo;
        }

        /// <summary>
        /// 初始化物品名稱到 Prefab 的映射
        /// </summary>
        public void InitializeItemMappings(ItemMapping[] itemMappings)
        {
            itemNameToPrefab.Clear();

            if (itemMappings == null || itemMappings.Length == 0)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("EntityItemManager: No item mappings provided!");
                }
                return;
            }

            foreach (var mapping in itemMappings)
            {
                if (mapping == null || string.IsNullOrEmpty(mapping.itemName) || mapping.itemPrefab == null)
                {
                    if (showDebugInfo)
                    {
                        Debug.LogWarning("EntityItemManager: Invalid item mapping (null itemName or itemPrefab)");
                    }
                    continue;
                }

                // 檢查 Prefab 是否有 Item 組件
                if (mapping.itemPrefab.GetComponent<Item>() == null)
                {
                    Debug.LogWarning($"EntityItemManager: Item prefab '{mapping.itemPrefab.name}' missing Item component!");
                    continue;
                }

                // 如果已經存在同名映射，覆蓋舊的
                if (itemNameToPrefab.ContainsKey(mapping.itemName))
                {
                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"EntityItemManager: Duplicate item name '{mapping.itemName}', overwriting previous mapping");
                    }
                    itemNameToPrefab[mapping.itemName] = mapping.itemPrefab;
                }
                else
                {
                    itemNameToPrefab.Add(mapping.itemName, mapping.itemPrefab);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"EntityItemManager: Initialized {itemNameToPrefab.Count} item mappings");
            }
        }

        /// <summary>
        /// 根據物品名稱/類型獲取 Prefab
        /// </summary>
        public GameObject GetItemPrefab(string itemName)
        {
            itemNameToPrefab.TryGetValue(itemName, out GameObject prefab);
            return prefab;
        }

        /// <summary>
        /// 為實體裝備物品列表
        /// </summary>
        public void EquipItemsToEntity(MonoBehaviour entity, List<string> itemNames)
        {
            if (entity == null || itemNames == null || itemNames.Count == 0)
            {
                return;
            }

            var itemHolder = entity.GetComponent<ItemHolder>();
            if (itemHolder == null)
            {
                Debug.LogError($"EntityItemManager: Entity {entity.name} missing ItemHolder component!");
                return;
            }

            // 清空現有物品
            itemHolder.ClearAllItems();

            // 裝備物品
            int equippedCount = 0;
            foreach (string itemName in itemNames)
            {
                if (string.IsNullOrEmpty(itemName))
                {
                    continue;
                }

                GameObject itemPrefab = GetItemPrefab(itemName);
                if (itemPrefab == null)
                {
                    Debug.LogWarning($"EntityItemManager: Item '{itemName}' not found in mappings! Entity: {entity.name}");
                    continue;
                }

                Item addedItem = itemHolder.AddItemFromPrefab(itemPrefab);
                if (addedItem != null)
                {
                    equippedCount++;
                    if (showDebugInfo)
                    {
                        Debug.Log($"EntityItemManager: Added '{itemName}' to {entity.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"EntityItemManager: Failed to add '{itemName}' to {entity.name}");
                }
            }

            // 添加完所有物品後，裝備第一個物品（優先裝備武器）
            if (equippedCount > 0)
            {
                // 優先尋找武器並裝備
                var weapons = itemHolder.GetItemsOfType<Weapon>();
                if (weapons.Count > 0)
                {
                    // 裝備第一個武器
                    var allItems = itemHolder.GetAllItems();
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        if (allItems[i] is Weapon)
                        {
                            itemHolder.SwitchToItem(i);
                            if (showDebugInfo)
                            {
                                Debug.Log($"EntityItemManager: Equipped first weapon '{weapons[0].ItemName}' to {entity.name}");
                            }
                            break;
                        }
                    }
                }
                else
                {
                    // 沒有武器，裝備第一個物品
                    itemHolder.SwitchToItem(0);
                    if (showDebugInfo)
                    {
                        Debug.Log($"EntityItemManager: Equipped first item to {entity.name}");
                    }
                }
            }

            if (showDebugInfo && equippedCount > 0)
            {
                Debug.Log($"EntityItemManager: Added {equippedCount} items to {entity.name}");
            }
        }

        /// <summary>
        /// 檢查物品映射是否包含指定物品
        /// </summary>
        public bool HasItem(string itemName)
        {
            return itemNameToPrefab.ContainsKey(itemName);
        }

        /// <summary>
        /// 獲取所有物品名稱
        /// </summary>
        public List<string> GetAllItemNames()
        {
            return new List<string>(itemNameToPrefab.Keys);
        }
    }
}


