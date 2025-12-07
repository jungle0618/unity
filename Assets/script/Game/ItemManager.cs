using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ItemManager 類別：管理場景中所有散落的物品
/// 職責：載入物品資料、生成物品、處理撿取邏輯
/// 參考 EnemyManager 的架構設計
/// </summary>
[DefaultExecutionOrder(200)] // 在 GameManager (150) 之後執行
public class ItemManager : MonoBehaviour
{
    [Header("物品資料設定")]
    [SerializeField] private TextAsset itemDataFile; // itemdata.txt 檔案
    [SerializeField] private GameObject worldItemPrefab; // WorldItem 的 Prefab
    
    [Header("物品映射來源")]
    [Tooltip("從 EntityManager 獲取物品映射（優先）")]
    [SerializeField] private bool useEntityManagerMapping = true;
    
    [Header("物品 Prefab 對應表（僅當 useEntityManagerMapping 為 false 時使用）")]
    [Tooltip("物品類型名稱與對應的 Item Prefab（必須與 itemdata.txt 中的 ItemType 一致）")]
    [SerializeField] private ItemPrefabMapping[] itemPrefabMappings;
    
    [Header("視覺設定")]
    [SerializeField] private Vector3 worldItemScale = Vector3.one; // 散落物品的固定大小
    
    [Header("撿取設定")]
    [SerializeField] private float defaultPickupRange = 2f; // 預設撿取範圍
    
    [Header("除錯設定")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showGizmos = true;
    
    // 物品管理
    private List<WorldItemData> worldItemsData = new List<WorldItemData>(); // 從檔案載入的物品資料
    private List<WorldItem> spawnedItems = new List<WorldItem>(); // 已生成的物品
    private Dictionary<string, GameObject> itemPrefabDict = new Dictionary<string, GameObject>(); // 物品類型 -> Prefab
    
    // EntityManager 引用（用於獲取統一的 item mapping）
    private EntityManager entityManager;
    
    // 統計資訊
    public int TotalItemCount => worldItemsData.Count;
    public int RemainingItemCount => spawnedItems.Count;
    public int PickedUpItemCount => TotalItemCount - RemainingItemCount;
    
    #region Unity 生命週期
    
    private void Start()
    {
        InitializeManager();
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        DrawItemPositions();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化管理器
    /// </summary>
    private void InitializeManager()
    {
        // 獲取 EntityManager 引用
        if (useEntityManagerMapping)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
            if (entityManager == null)
            {
                Debug.LogWarning("ItemManager: EntityManager not found, falling back to local mappings");
                useEntityManagerMapping = false;
            }
        }
        
        // 建立物品類型對應表
        BuildItemPrefabDictionary();
        
        // 載入物品資料
        LoadItemData();
        
        // 生成所有物品
        SpawnAllItems();
        
        if (showDebugInfo)
        {
            //Debug.Log($"ItemManager: Initialized with {TotalItemCount} items");
        }
    }
    
    /// <summary>
    /// 建立物品類型到 Prefab 的對應字典
    /// </summary>
    private void BuildItemPrefabDictionary()
    {
        itemPrefabDict.Clear();
        
        // 優先使用 EntityManager 的映射
        if (useEntityManagerMapping && entityManager != null)
        {
            var entityMapping = entityManager.ItemMappingDict;
            if (entityMapping != null && entityMapping.Count > 0)
            {
                foreach (var kvp in entityMapping)
                {
                    itemPrefabDict[kvp.Key] = kvp.Value;
                }
                
                if (showDebugInfo)
                {
                    //Debug.Log($"ItemManager: Loaded {itemPrefabDict.Count} item mappings from EntityManager");
                }
                return;
            }
            else
            {
                Debug.LogWarning("ItemManager: EntityManager mapping is empty, falling back to local mappings");
            }
        }
        
        // 使用本地映射（向後兼容）
        if (itemPrefabMappings == null || itemPrefabMappings.Length == 0)
        {
            Debug.LogWarning("ItemManager: No item prefab mappings assigned!");
            return;
        }
        
        foreach (var mapping in itemPrefabMappings)
        {
            if (string.IsNullOrEmpty(mapping.itemType) || mapping.itemPrefab == null)
            {
                Debug.LogWarning($"ItemManager: Invalid mapping - Type: {mapping.itemType}, Prefab: {mapping.itemPrefab}");
                continue;
            }
            
            if (itemPrefabDict.ContainsKey(mapping.itemType))
            {
                Debug.LogWarning($"ItemManager: Duplicate item type '{mapping.itemType}' in mappings!");
                continue;
            }
            
            itemPrefabDict[mapping.itemType] = mapping.itemPrefab;
        }
        
        if (showDebugInfo)
        {
            //Debug.Log($"ItemManager: Built prefab dictionary with {itemPrefabDict.Count} item types");
        }
    }
    
    #endregion
    
    #region 物品資料載入
    
    /// <summary>
    /// 從 TextAsset 載入所有物品的資料
    /// </summary>
    private void LoadItemData()
    {
        worldItemsData.Clear();
        
        if (itemDataFile == null)
        {
            Debug.LogError("ItemManager: Item data file (TextAsset) is not assigned! Please assign the itemdata.txt file in the inspector.");
            CreateDefaultItemData();
            return;
        }
        
        try
        {
            string[] lines = itemDataFile.text.Split('\n');
            
            foreach (string line in lines)
            {
                // 跳過註釋行和空行
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                
                string[] parts = line.Split(',');
                if (parts.Length >= 5)
                {
                    int itemIndex = int.Parse(parts[0].Trim());
                    string itemType = parts[1].Trim();
                    float x = float.Parse(parts[2].Trim());
                    float y = float.Parse(parts[3].Trim());
                    float z = float.Parse(parts[4].Trim());
                    
                    WorldItemData itemData = new WorldItemData
                    {
                        index = itemIndex,
                        itemType = itemType,
                        position = new Vector3(x, y, z)
                    };
                    
                    worldItemsData.Add(itemData);
                    
                    if (showDebugInfo)
                    {
                        //Debug.Log($"ItemManager: Loaded item {itemIndex} - Type: {itemType}, Position: ({x}, {y}, {z})");
                    }
                }
            }
            
            //Debug.Log($"ItemManager: Loaded {worldItemsData.Count} items from {itemDataFile.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ItemManager: Failed to load item data: {e.Message}");
            CreateDefaultItemData();
        }
    }
    
    /// <summary>
    /// 創建預設的物品資料（當檔案載入失敗時）
    /// </summary>
    private void CreateDefaultItemData()
    {
        worldItemsData.Clear();
        
        // 創建一些預設物品
        worldItemsData.Add(new WorldItemData { index = 0, itemType = "Sword", position = new Vector3(10, 10, 0) });
        worldItemsData.Add(new WorldItemData { index = 1, itemType = "Bow", position = new Vector3(20, 10, 0) });
        worldItemsData.Add(new WorldItemData { index = 2, itemType = "Knife", position = new Vector3(30, 10, 0) });
        
        //Debug.Log($"ItemManager: Created {worldItemsData.Count} default items");
    }
    
    #endregion
    
    #region 物品生成
    
    /// <summary>
    /// 生成所有物品到場景中
    /// </summary>
    private void SpawnAllItems()
    {
        if (worldItemsData.Count == 0)
        {
            Debug.LogWarning("ItemManager: No item data to spawn!");
            return;
        }
        
        foreach (var itemData in worldItemsData)
        {
            SpawnItem(itemData);
        }
        
        //Debug.Log($"ItemManager: Spawned {spawnedItems.Count} items");
    }
    
    /// <summary>
    /// 生成單個物品
    /// </summary>
    private void SpawnItem(WorldItemData itemData)
    {
        // 檢查是否有對應的 Item Prefab
        if (!itemPrefabDict.ContainsKey(itemData.itemType))
        {
            Debug.LogWarning($"ItemManager: No prefab mapping found for item type '{itemData.itemType}'");
            return;
        }
        
        GameObject itemPrefab = itemPrefabDict[itemData.itemType];
        if (itemPrefab == null)
        {
            Debug.LogWarning($"ItemManager: Item prefab is null for type '{itemData.itemType}'");
            return;
        }
        
        WorldItem worldItem = CreateWorldItem(itemData.itemType, itemData.position, itemPrefab, $"WorldItem_{itemData.itemType}_{itemData.index}");
        if (worldItem != null)
        {
            spawnedItems.Add(worldItem);
            
            if (showDebugInfo)
            {
                //Debug.Log($"ItemManager: Spawned {itemData.itemType} at {itemData.position}");
            }
        }
    }
    
    /// <summary>
    /// 創建 WorldItem（共用方法）
    /// </summary>
    private WorldItem CreateWorldItem(string itemType, Vector3 position, GameObject itemPrefab, string defaultName = null)
    {
        // 生成 WorldItem
        GameObject worldItemGO;
        
        if (worldItemPrefab != null)
        {
            // 使用自訂的 WorldItem Prefab
            worldItemGO = Instantiate(worldItemPrefab, position, Quaternion.identity, transform);
        }
        else
        {
            // 創建基本的 WorldItem GameObject
            worldItemGO = new GameObject(defaultName ?? $"WorldItem_{itemType}");
            worldItemGO.transform.position = position;
            worldItemGO.transform.SetParent(transform);
            worldItemGO.AddComponent<WorldItem>();
        }
        
        // 設定 WorldItem 組件
        WorldItem worldItem = worldItemGO.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            worldItem = worldItemGO.AddComponent<WorldItem>();
        }
        
        worldItem.SetItemType(itemType);
        worldItem.SetItemPrefab(itemPrefab);
        worldItem.SetItemScale(worldItemScale);
        
        return worldItem;
    }
    
    #endregion
    
    #region 撿取系統
    
    /// <summary>
    /// 嘗試撿取物品（根據距離檢查）
    /// </summary>
    /// <param name="callerPosition">呼叫者的位置</param>
    /// <param name="targetHolder">目標 ItemHolder（物品會被加入到這裡）</param>
    /// <param name="pickupRange">撿取範圍（預設使用 defaultPickupRange）</param>
    /// <returns>是否成功撿取物品</returns>
    public bool TryPickupItem(Vector3 callerPosition, ItemHolder targetHolder, float pickupRange = -1f)
    {
        if (targetHolder == null)
        {
            Debug.LogWarning("ItemManager: Target ItemHolder is null!");
            return false;
        }
        
        if (spawnedItems.Count == 0)
        {
            if (showDebugInfo)
            {
                //Debug.Log("ItemManager: No items available to pick up");
            }
            return false;
        }
        
        // 使用預設撿取範圍
        if (pickupRange < 0)
        {
            pickupRange = defaultPickupRange;
        }
        
        // 找到最近的物品
        WorldItem closestItem = FindClosestItem(callerPosition, pickupRange);
        
        if (closestItem == null)
        {
            if (showDebugInfo)
            {
                //Debug.Log($"ItemManager: No items within pickup range ({pickupRange})");
            }
            return false;
        }
        
        // 撿取物品
        return PickupItem(closestItem, targetHolder);
    }
    
    /// <summary>
    /// 找到最近的物品
    /// </summary>
    private WorldItem FindClosestItem(Vector3 position, float maxRange)
    {
        WorldItem closestItem = null;
        float closestDistanceSqr = maxRange * maxRange;
        
        // 清理已銷毀的物品
        spawnedItems.RemoveAll(item => item == null || item.gameObject == null);
        
        foreach (var item in spawnedItems)
        {
            float distanceSqr = (item.Position - position).sqrMagnitude;
            
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestItem = item;
            }
        }
        
        return closestItem;
    }
    
    /// <summary>
    /// 撿取指定的物品
    /// </summary>
    private bool PickupItem(WorldItem worldItem, ItemHolder targetHolder)
    {
        if (worldItem == null || worldItem.ItemPrefab == null)
        {
            Debug.LogWarning("ItemManager: Invalid world item or item prefab!");
            return false;
        }
        
        if (showDebugInfo)
        {
            //Debug.Log($"[ItemManager] Attempting to pickup {worldItem.ItemType}. Current items in holder: {targetHolder.ItemCount}");
        }
        
        // 將物品 Prefab 加入 ItemHolder（不裝備，只加到列表尾端）
        Item addedItem = targetHolder.AddItemFromPrefab(worldItem.ItemPrefab);
        
        if (addedItem == null)
        {
            Debug.LogWarning($"ItemManager: Failed to add item {worldItem.ItemType}");
            return false;
        }
        
        // 從場景中移除物品（先從列表移除，再銷毀）
        spawnedItems.Remove(worldItem);
        
        // 銷毀 GameObject
        worldItem.OnPickedUp(); // 會銷毀 GameObject
        
        if (showDebugInfo)
        {
            //Debug.Log($"ItemManager: Picked up {worldItem.ItemType}. Remaining items: {spawnedItems.Count}");
        }
        
        return true;
    }
    
    /// <summary>
    /// 嘗試撿取指定類型的物品
    /// </summary>
    public bool TryPickupItemByType(string itemType, Vector3 callerPosition, ItemHolder targetHolder, float pickupRange = -1f)
    {
        if (targetHolder == null || string.IsNullOrEmpty(itemType))
        {
            Debug.LogWarning("ItemManager: Invalid parameters for pickup by type!");
            return false;
        }
        
        if (pickupRange < 0)
        {
            pickupRange = defaultPickupRange;
        }
        
        // 找到指定類型且在範圍內的最近物品
        WorldItem targetItem = null;
        float closestDistanceSqr = pickupRange * pickupRange;
        
        foreach (var item in spawnedItems)
        {
            if (item == null || item.ItemType != itemType) continue;
            
            float distanceSqr = (item.Position - callerPosition).sqrMagnitude;
            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                targetItem = item;
            }
        }
        
        if (targetItem == null)
        {
            if (showDebugInfo)
            {
                //Debug.Log($"ItemManager: No '{itemType}' items found within range");
            }
            return false;
        }
        
        return PickupItem(targetItem, targetHolder);
    }
    
    #endregion
    
    #region 視覺化
    
    /// <summary>
    /// 在 Scene 視圖中顯示物品位置
    /// </summary>
    private void DrawItemPositions()
    {
        if (worldItemsData == null || worldItemsData.Count == 0) return;
        
        foreach (var itemData in worldItemsData)
        {
            // 檢查物品是否已被撿取
            bool isPickedUp = !spawnedItems.Any(item => item != null && item.ItemType == itemData.itemType && item.Position == itemData.position);
            
            // 已撿取的物品用灰色，未撿取的用黃色
            Gizmos.color = isPickedUp ? Color.gray : Color.yellow;
            
            // 繪製物品位置
            Gizmos.DrawWireSphere(itemData.position, 0.5f);
            
            // 繪製撿取範圍
            if (!isPickedUp)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(itemData.position, defaultPickupRange);
            }
            
#if UNITY_EDITOR
            // 顯示物品類型標籤
            Handles.color = isPickedUp ? Color.gray : Color.white;
            Handles.Label(itemData.position + Vector3.up * 0.8f, $"{itemData.itemType} ({itemData.index})");
#endif
        }
    }
    
    #endregion
    
    #region 公共 API
    
    /// <summary>
    /// 獲取所有已生成的物品
    /// </summary>
    public List<WorldItem> GetAllSpawnedItems()
    {
        return new List<WorldItem>(spawnedItems);
    }
    
    /// <summary>
    /// 獲取指定類型的所有物品
    /// </summary>
    public List<WorldItem> GetItemsByType(string itemType)
    {
        return spawnedItems.Where(item => item != null && item.ItemType == itemType).ToList();
    }
    
    /// <summary>
    /// 清除所有已生成的物品
    /// </summary>
    private void ClearSpawnedItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        spawnedItems.Clear();
    }
    
    /// <summary>
    /// 重新生成所有物品
    /// </summary>
    public void RespawnAllItems()
    {
        ClearSpawnedItems();
        SpawnAllItems();
        
        if (showDebugInfo)
        {
            //Debug.Log($"ItemManager: Respawned {spawnedItems.Count} items");
        }
    }
    
    /// <summary>
    /// 清除所有物品
    /// </summary>
    public void ClearAllItems()
    {
        ClearSpawnedItems();
        
        if (showDebugInfo)
        {
            //Debug.Log("ItemManager: Cleared all items");
        }
    }
    
    /// <summary>
    /// 在指定位置掉落物品（用於實體死亡時）
    /// </summary>
    /// <param name="itemPrefab">物品的 Prefab</param>
    /// <param name="position">掉落位置</param>
    /// <returns>生成的 WorldItem，失敗則返回 null</returns>
    public WorldItem DropItemAtPosition(GameObject itemPrefab, Vector3 position)
    {
        if (itemPrefab == null)
        {
            Debug.LogWarning("ItemManager: Cannot drop item - prefab is null");
            return null;
        }
        
        // 獲取物品類型
        Item itemComponent = itemPrefab.GetComponent<Item>();
        if (itemComponent == null)
        {
            Debug.LogWarning($"ItemManager: Prefab {itemPrefab.name} does not have an Item component");
            return null;
        }
        
        string itemType = itemComponent.ItemName;
        
        WorldItem worldItem = CreateWorldItem(itemType, position, itemPrefab, $"DroppedItem_{itemType}");
        if (worldItem != null)
        {
            spawnedItems.Add(worldItem);
            
            if (showDebugInfo)
            {
                //Debug.Log($"ItemManager: Dropped {itemType} at {position}");
            }
        }
        
        return worldItem;
    }
    
    /// <summary>
    /// 在指定位置掉落多個物品（用於實體死亡時）
    /// </summary>
    /// <param name="itemPrefabs">物品 Prefab 列表</param>
    /// <param name="position">掉落位置中心</param>
    /// <param name="spreadRadius">散落半徑</param>
    /// <returns>生成的 WorldItem 列表</returns>
    public List<WorldItem> DropItemsAtPosition(List<GameObject> itemPrefabs, Vector3 position, float spreadRadius = 1.5f)
    {
        List<WorldItem> droppedItems = new List<WorldItem>();
        
        if (itemPrefabs == null || itemPrefabs.Count == 0)
        {
            return droppedItems;
        }
        
        // 計算每個物品的掉落位置（圓形散落）
        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            if (itemPrefabs[i] == null) continue;
            
            // 計算散落位置（圓形分佈）
            float angle = (360f / itemPrefabs.Count) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spreadRadius;
            Vector3 dropPosition = position + offset;
            
            WorldItem worldItem = DropItemAtPosition(itemPrefabs[i], dropPosition);
            if (worldItem != null)
            {
                droppedItems.Add(worldItem);
            }
        }
        
        if (showDebugInfo)
        {
            //Debug.Log($"ItemManager: Dropped {droppedItems.Count} items at {position}");
        }
        
        return droppedItems;
    }
    
    #endregion
    
    #region 資料結構
    
    /// <summary>
    /// 物品資料結構
    /// </summary>
    [System.Serializable]
    private class WorldItemData
    {
        public int index;
        public string itemType;
        public Vector3 position;
    }
    
    /// <summary>
    /// 物品類型與 Prefab 的對應
    /// </summary>
    [System.Serializable]
    public class ItemPrefabMapping
    {
        [Tooltip("物品類型名稱（必須與 itemdata.txt 中的 ItemType 一致）")]
        public string itemType;
        
        [Tooltip("對應的 Item Prefab（必須包含 Item 組件）")]
        public GameObject itemPrefab;
    }
    
    #endregion
}

