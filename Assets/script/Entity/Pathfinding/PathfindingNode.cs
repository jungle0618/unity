using UnityEngine;

/// <summary>
/// 路徑規劃節點
/// 用於A*算法中的網格節點
/// </summary>
public class PathfindingNode
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public bool isWalkable;
    public float gCost; // 從起點到當前節點的實際成本
    public float hCost; // 從當前節點到終點的啟發式成本
    public float fCost => gCost + hCost; // 總成本
    public PathfindingNode parent;

    public PathfindingNode(Vector2Int gridPos, Vector3 worldPos, bool walkable)
    {
        gridPosition = gridPos;
        worldPosition = worldPos;
        isWalkable = walkable;
        gCost = 0;
        hCost = 0;
        parent = null;
    }

    public void Reset()
    {
        gCost = 0;
        hCost = 0;
        parent = null;
    }
}
