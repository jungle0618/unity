using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 簡化的門控制器，直接操作Tilemap
/// 支援鑰匙系統
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("門設定")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase doorTile;        // 普通門（不需要鑰匙）
    [SerializeField] private TileBase redDoorTile;     // 紅色門（需要紅色鑰匙）
    [SerializeField] private TileBase blueDoorTile;    // 藍色門（需要藍色鑰匙）
    
    [Header("觸發範圍")]
    [SerializeField] private float openDoorRange = 1.5f; // 多近可以開門
    
    // 單例模式
    public static DoorController Instance { get; private set; }
    
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
    
    /// <summary>
    /// 尋找範圍內最近的門
    /// </summary>
    /// <param name="position">玩家位置</param>
    /// <returns>最近門的 Cell Position，如果沒有則返回 null</returns>
    private Vector3Int? FindNearestDoorInRange(Vector3 position)
    {
        if (tilemap == null) return null;
        
        Vector3Int centerCell = tilemap.WorldToCell(position);
        
        // 計算要搜尋的範圍（以 cell 為單位）
        int searchRadius = Mathf.CeilToInt(openDoorRange);
        
        Vector3Int? nearestDoorCell = null;
        float nearestDistance = float.MaxValue;
        
        // 在範圍內搜尋門
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector3Int checkCell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);
                TileBase tile = tilemap.GetTile(checkCell);
                
                if (IsDoorTile(tile))
                {
                    // 計算世界座標距離
                    Vector3 doorWorldPos = tilemap.GetCellCenterWorld(checkCell);
                    float distance = Vector3.Distance(position, doorWorldPos);
                    
                    if (distance <= openDoorRange && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestDoorCell = checkCell;
                    }
                }
            }
        }
        
        return nearestDoorCell;
    }
    
    /// <summary>
    /// 根據 Tile 類型獲取需要的鑰匙類型
    /// </summary>
    private KeyType GetRequiredKeyTypeByTile(TileBase tile)
    {
        if (tile == redDoorTile)
            return KeyType.Red;
        if (tile == blueDoorTile)
            return KeyType.Blue;
        
        // doorTile 或其他不認識的 tile 都不需要鑰匙
        return KeyType.None;
    }
    
    /// <summary>
    /// 檢查指定位置是否有門
    /// </summary>
    private bool IsDoorTile(TileBase tile)
    {
        return tile == doorTile || tile == redDoorTile || tile == blueDoorTile;
    }
    
    /// <summary>
    /// 嘗試使用鑰匙開啟門（從背包中查找，不需要裝備）
    /// </summary>
    /// <param name="playerPosition">玩家位置</param>
    /// <param name="itemHolder">持有鑰匙的物品持有者</param>
    /// <returns>是否成功開啟門</returns>
    public bool TryOpenDoorWithKey(Vector3 playerPosition, ItemHolder itemHolder)
    {
        if (tilemap == null) return false;
        
        // 找到範圍內最近的門
        Vector3Int? nearestDoorCell = FindNearestDoorInRange(playerPosition);
        
        if (nearestDoorCell == null)
        {
            Debug.Log($"[DoorController] 在範圍 {openDoorRange} 內沒有找到門");
            return false;
        }
        
        Vector3Int cellPosition = nearestDoorCell.Value;
        TileBase tile = tilemap.GetTile(cellPosition);
        
        // 根據 tile 類型自動判斷需要的鑰匙類型
        KeyType requiredKeyType = GetRequiredKeyTypeByTile(tile);
        
        // 如果不需要鑰匙（普通門），直接開門
        if (requiredKeyType == KeyType.None)
        {
            Debug.Log($"[DoorController] 門在位置 {cellPosition} 不需要鑰匙");
            RemoveConnectedDoorTiles(cellPosition);
            return true;
        }
        
        // 需要鑰匙 - 從背包中查找（不需要裝備）
        if (itemHolder == null)
        {
            Debug.LogWarning($"[DoorController] 門在位置 {cellPosition} 需要 {requiredKeyType} 鑰匙，但沒有提供 ItemHolder");
            ShowNotification("The door is locked! ItemHolder is missing.");
            return false;
        }
        
        // 從背包中取得對應鑰匙（不需要裝備）
        Key matchingKey = itemHolder.GetKeyForDoor(requiredKeyType);
        
        if (matchingKey == null)
        {
            Debug.LogWarning($"[DoorController] 門需要 {requiredKeyType} 鑰匙，但背包中沒有找到");
            ShowNotification(GetKeyRequiredMessage(requiredKeyType));
            return false;
        }
        
        // 嘗試開門
        bool unlocked = matchingKey.TryUnlockDoor(requiredKeyType, out bool shouldRemove);
        
        if (!unlocked)
        {
            Debug.LogWarning($"[DoorController] 鑰匙 {matchingKey.KeyType} 無法開啟 {requiredKeyType} 門");
            return false;
        }
        
        // 成功開門記錄（不再顯示剩餘堆疊數，因為堆疊功能已取消）
        Debug.Log($"[DoorController] 使用 {matchingKey.KeyType} 鑰匙開啟門在位置 {cellPosition}{(shouldRemove ? "（鑰匙已使用完畢，已移除）" : "（鑰匙保留）")}");
        
        if (shouldRemove)
        {
            itemHolder.RemoveItem(matchingKey); // 會自動觸發 UI 更新事件
        }
        
        // 刪除門 tiles
        RemoveConnectedDoorTiles(cellPosition);
        
        return true;
    }
    
    /// <summary>
    /// 刪除指定世界位置的門（包括相鄰的門tile）
    /// 不檢查鑰匙，直接刪除
    /// </summary>
    /// <param name="worldPosition">世界位置</param>
    /// <returns>是否成功刪除</returns>
    public bool RemoveDoorAtWorldPosition(Vector3 worldPosition)
    {
        if (tilemap == null) return false;
        
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        TileBase tile = tilemap.GetTile(cellPosition);
        
        // 檢查該位置是否有門
        if (IsDoorTile(tile))
        {
            // 刪除相鄰的所有門tile
            RemoveConnectedDoorTiles(cellPosition);
            
            Debug.Log($"成功刪除門在位置: {cellPosition}");
            return true;
        }
        
        Debug.Log($"在位置 {cellPosition} 沒有找到門");
        return false;
    }
    
    /// <summary>
    /// 獲取門需要的鑰匙類型（根據 tile 類型自動判斷）
    /// </summary>
    /// <param name="worldPosition">世界位置</param>
    /// <returns>鑰匙類型</returns>
    public KeyType GetDoorKeyType(Vector3 worldPosition)
    {
        if (tilemap == null) return KeyType.None;
        
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        TileBase tile = tilemap.GetTile(cellPosition);
        
        return GetRequiredKeyTypeByTile(tile);
    }
    
    /// <summary>
    /// 刪除相連的門tile（使用深度優先搜索）
    /// </summary>
    /// <param name="startPosition">起始位置</param>
    private void RemoveConnectedDoorTiles(Vector3Int startPosition)
    {
        // 使用深度優先搜索找到所有相連的門tile
        var visited = new System.Collections.Generic.HashSet<Vector3Int>();
        var stack = new System.Collections.Generic.Stack<Vector3Int>();
        
        stack.Push(startPosition);
        
        while (stack.Count > 0)
        {
            Vector3Int current = stack.Pop();
            
            if (visited.Contains(current)) continue;
            visited.Add(current);
            
            TileBase currentTile = tilemap.GetTile(current);
            
            // 檢查當前位置是否有門tile
            if (IsDoorTile(currentTile))
            {
                // 刪除這個門tile
                tilemap.SetTile(current, null);
                
                // 檢查8個方向的相鄰位置
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue; // 跳過自己
                        
                        Vector3Int neighbor = new Vector3Int(current.x + x, current.y + y, current.z);
                        TileBase neighborTile = tilemap.GetTile(neighbor);
                        
                        // 如果相鄰位置有門tile且未訪問過，加入搜索
                        if (!visited.Contains(neighbor) && IsDoorTile(neighborTile))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }
    }
    
    #region Notification Helpers
    
    /// <summary>
    /// 顯示通知訊息給玩家
    /// </summary>
    private void ShowNotification(string message)
    {
        // 嘗試找到 GameUIManager
        GameUIManager gameUIManager = FindFirstObjectByType<GameUIManager>();
        if (gameUIManager != null)
        {
            NotificationUIManager notificationManager = gameUIManager.GetNotificationUIManager();
            if (notificationManager != null)
            {
                notificationManager.ShowNotification(message);
            }
            else
            {
                Debug.LogWarning($"[DoorController] NotificationUIManager 未找到，無法顯示訊息: {message}");
            }
        }
        else
        {
            Debug.LogWarning($"[DoorController] GameUIManager 未找到，無法顯示訊息: {message}");
        }
    }
    
    /// <summary>
    /// 根據鑰匙類型獲取通知訊息
    /// </summary>
    private string GetKeyRequiredMessage(KeyType keyType)
    {
        switch (keyType)
        {
            case KeyType.Red:
                return "The door is locked! Requires a RED key.";
            case KeyType.Blue:
                return "The door is locked! Requires a BLUE key.";
            case KeyType.None:
                return "The door is locked!";
            default:
                return $"The door is locked! Requires a {keyType} key.";
        }
    }
    
    #endregion
    
}