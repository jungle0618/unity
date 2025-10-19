using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 簡化的門控制器，直接操作Tilemap
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("門設定")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase doorTile;
    
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
    /// 刪除指定世界位置的門（包括相鄰的門tile）
    /// </summary>
    /// <param name="worldPosition">世界位置</param>
    /// <returns>是否成功刪除</returns>
    public bool RemoveDoorAtWorldPosition(Vector3 worldPosition)
    {
        if (tilemap == null) return false;
        
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        
        // 檢查該位置是否有門
        if (tilemap.GetTile(cellPosition) == doorTile)
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
            
            // 檢查當前位置是否有門tile
            if (tilemap.GetTile(current) == doorTile)
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
                        
                        // 如果相鄰位置有門tile且未訪問過，加入搜索
                        if (!visited.Contains(neighbor) && tilemap.GetTile(neighbor) == doorTile)
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }
    }
}