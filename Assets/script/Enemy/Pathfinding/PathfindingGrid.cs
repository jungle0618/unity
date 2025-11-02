using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 路徑規劃網格系統
/// 負責管理網格地圖和節點
/// </summary>
public class PathfindingGrid : MonoBehaviour
{
    [Header("網格設定")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private Vector2 gridOffset = Vector2.zero;

    [Header("Tilemap 設定")]
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap objectTilemap;
    [SerializeField] private LayerMask obstacleLayerMask = -1;

    private PathfindingNode[,] grid;
    private Vector2 gridWorldSize;

    public float CellSize => cellSize;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    private void Awake()
    {
        gridWorldSize = new Vector2(gridWidth * cellSize, gridHeight * cellSize);
        CreateGrid();
    }

    /// <summary>
    /// 創建網格
    /// </summary>
    private void CreateGrid()
    {
        grid = new PathfindingNode[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = GetWorldPosition(x, y);
                bool walkable = IsWalkable(worldPos);
                grid[x, y] = new PathfindingNode(new Vector2Int(x, y), worldPos, walkable);
            }
        }
    }

    /// <summary>
    /// 獲取世界位置
    /// </summary>
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(
            x * cellSize + gridOffset.x,
            y * cellSize + gridOffset.y,
            0
        );
    }

    /// <summary>
    /// 獲取網格座標
    /// </summary>
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOffset.x) / cellSize);
        int y = Mathf.RoundToInt((worldPosition.y - gridOffset.y) / cellSize);
        
        x = Mathf.Clamp(x, 0, gridWidth - 1);
        y = Mathf.Clamp(y, 0, gridHeight - 1);
        
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 獲取節點
    /// </summary>
    public PathfindingNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            return grid[x, y];
        }
        return null;
    }

    /// <summary>
    /// 獲取節點
    /// </summary>
    public PathfindingNode GetNode(Vector2Int gridPos)
    {
        return GetNode(gridPos.x, gridPos.y);
    }

    /// <summary>
    /// 獲取節點
    /// </summary>
    public PathfindingNode GetNode(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        return GetNode(gridPos);
    }

    /// <summary>
    /// 獲取鄰居節點
    /// </summary>
    public List<PathfindingNode> GetNeighbors(PathfindingNode node)
    {
        List<PathfindingNode> neighbors = new List<PathfindingNode>();
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // 跳過自己
                
                int checkX = node.gridPosition.x + x;
                int checkY = node.gridPosition.y + y;
                
                PathfindingNode neighbor = GetNode(checkX, checkY);
                if (neighbor != null && neighbor.isWalkable)
                {
                    neighbors.Add(neighbor);
                }
            }
        }
        
        return neighbors;
    }

    /// <summary>
    /// 檢查位置是否可行走
    /// </summary>
    private bool IsWalkable(Vector3 worldPosition)
    {
        // 檢查牆壁 tilemap
        if (wallTilemap != null)
        {
            Vector3Int cellPosition = wallTilemap.WorldToCell(worldPosition);
            if (wallTilemap.GetTile(cellPosition) != null)
            {
                return false; // 有牆壁，不可行走
            }
        }

        // 檢查物件 tilemap
        if (objectTilemap != null)
        {
            Vector3Int cellPosition = objectTilemap.WorldToCell(worldPosition);
            if (objectTilemap.GetTile(cellPosition) != null)
            {
                return false; // 有物件，不可行走
            }
        }

        // 檢查碰撞體（額外的障礙物檢測）
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, cellSize * 0.4f, obstacleLayerMask);
        if (hit != null)
        {
            return false; // 有碰撞體，不可行走
        }

        return true; // 沒有障礙物，可以行走
    }

    /// <summary>
    /// 檢查位置是否可行走（帶除錯信息）
    /// </summary>
    public bool IsWalkableWithDebug(Vector3 worldPosition)
    {
        bool hasWall = false;
        bool hasObject = false;
        bool hasCollider = false;

        // 檢查牆壁 tilemap
        if (wallTilemap != null)
        {
            Vector3Int cellPosition = wallTilemap.WorldToCell(worldPosition);
            if (wallTilemap.GetTile(cellPosition) != null)
            {
                hasWall = true;
            }
        }

        // 檢查物件 tilemap
        if (objectTilemap != null)
        {
            Vector3Int cellPosition = objectTilemap.WorldToCell(worldPosition);
            if (objectTilemap.GetTile(cellPosition) != null)
            {
                hasObject = true;
            }
        }

        // 檢查碰撞體
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, cellSize * 0.4f, obstacleLayerMask);
        if (hit != null)
        {
            hasCollider = true;
        }

        bool isWalkable = !hasWall && !hasObject && !hasCollider;
        
        // 除錯信息
        if (!isWalkable)
        {
            string obstacles = "";
            if (hasWall) obstacles += "牆壁 ";
            if (hasObject) obstacles += "物件 ";
            if (hasCollider) obstacles += "碰撞體 ";
            Debug.Log($"位置 {worldPosition} 不可行走，障礙物: {obstacles}");
        }

        return isWalkable;
    }

    /// <summary>
    /// 更新網格（當地形改變時調用）
    /// </summary>
    public void UpdateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = GetWorldPosition(x, y);
                grid[x, y].isWalkable = IsWalkable(worldPos);
            }
        }
    }

    /// <summary>
    /// 檢查網格設定是否正確
    /// </summary>
    [ContextMenu("檢查網格設定")]
    public void CheckGridSettings()
    {
        Debug.Log("=== 路徑規劃網格設定檢查 ===");
        Debug.Log($"網格大小: {gridWidth} x {gridHeight}");
        Debug.Log($"單元大小: {cellSize}");
        Debug.Log($"網格偏移: {gridOffset}");
        
        if (wallTilemap != null)
        {
            Debug.Log($"牆壁Tilemap: {wallTilemap.name}");
        }
        else
        {
            Debug.LogWarning("牆壁Tilemap未設定！");
        }
        
        if (objectTilemap != null)
        {
            Debug.Log($"物件Tilemap: {objectTilemap.name}");
        }
        else
        {
            Debug.LogWarning("物件Tilemap未設定！");
        }
        
        Debug.Log($"障礙物層遮罩: {obstacleLayerMask.value}");
        
        // 檢查一些樣本位置
        Vector3[] testPositions = {
            Vector3.zero,
            Vector3.one,
            Vector3.right * 5,
            Vector3.up * 5
        };
        
        foreach (Vector3 pos in testPositions)
        {
            PathfindingNode node = GetNode(pos);
            if (node != null)
            {
                Debug.Log($"位置 {pos}: 可行走 = {node.isWalkable}");
            }
        }
    }

    /// <summary>
    /// 重新創建網格（用於修復設定問題）
    /// </summary>
    [ContextMenu("重新創建網格")]
    public void RecreateGrid()
    {
        Debug.Log("重新創建路徑規劃網格...");
        CreateGrid();
        Debug.Log("網格重新創建完成！");
    }

    /// <summary>
    /// 在Scene視圖中繪製網格（除錯用）
    /// 已禁用：不顯示任何視覺化內容
    /// </summary>
    private void OnDrawGizmos()
    {
        // 已禁用視覺化顯示
        return;
    }
}
