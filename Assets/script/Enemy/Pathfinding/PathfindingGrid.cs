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
                return false;
            }
        }

        // 檢查物件 tilemap
        if (objectTilemap != null)
        {
            Vector3Int cellPosition = objectTilemap.WorldToCell(worldPosition);
            if (objectTilemap.GetTile(cellPosition) != null)
            {
                return false;
            }
        }

        // 檢查碰撞體
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, cellSize * 0.4f, obstacleLayerMask);
        if (hit != null)
        {
            return false;
        }

        return true;
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
    /// 在Scene視圖中繪製網格（除錯用）
    /// </summary>
    private void OnDrawGizmos()
    {
        if (grid == null) return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position + (Vector3)gridOffset, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));

        if (grid != null)
        {
            foreach (PathfindingNode node in grid)
            {
                Gizmos.color = node.isWalkable ? Color.white : Color.red;
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (cellSize - 0.1f));
            }
        }
    }
}
