using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 貪心路徑規劃算法
/// 用於計算從起點到終點的路徑（優先選擇最接近目標的方向）
/// </summary>
public class GreedyPathfinding : MonoBehaviour
{
    [Header("路徑規劃設定")]
    [SerializeField] private PathfindingGrid pathfindingGrid;
    [SerializeField] private bool showDebugPath = false;
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private Color exploredColor = Color.yellow;

    private List<PathfindingNode> currentPath = new List<PathfindingNode>();
    private List<PathfindingNode> exploredNodes = new List<PathfindingNode>();

    private void Awake()
    {
        if (pathfindingGrid == null)
        {
            pathfindingGrid = FindObjectOfType<PathfindingGrid>();
        }
    }

    /// <summary>
    /// 計算從起點到終點的路徑
    /// </summary>
    /// <param name="startPos">起點世界位置</param>
    /// <param name="targetPos">終點世界位置</param>
    /// <returns>路徑節點列表，如果找不到路徑則返回null</returns>
    public List<PathfindingNode> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        if (pathfindingGrid == null)
        {
            Debug.LogError("PathfindingGrid 未設定！");
            return null;
        }

        PathfindingNode startNode = pathfindingGrid.GetNode(startPos);
        PathfindingNode targetNode = pathfindingGrid.GetNode(targetPos);

        if (startNode == null || targetNode == null)
        {
            Debug.LogWarning($"無法找到起點或終點節點: {startPos} -> {targetPos}");
            return null;
        }

        if (!startNode.isWalkable || !targetNode.isWalkable)
        {
            Debug.LogWarning("起點或終點不可行走！");
            return null;
        }

        return FindPath(startNode, targetNode);
    }

    /// <summary>
    /// 計算從起點到終點的路徑（貪心算法）
    /// </summary>
    /// <param name="startNode">起點節點</param>
    /// <param name="targetNode">終點節點</param>
    /// <returns>路徑節點列表，如果找不到路徑則返回null</returns>
    public List<PathfindingNode> FindPath(PathfindingNode startNode, PathfindingNode targetNode)
    {
        List<PathfindingNode> path = new List<PathfindingNode>();
        HashSet<PathfindingNode> visited = new HashSet<PathfindingNode>();
        exploredNodes.Clear();

        // 重置所有節點
        ResetAllNodes();

        PathfindingNode currentNode = startNode;
        path.Add(currentNode);
        visited.Add(currentNode);
        exploredNodes.Add(currentNode);

        int maxIterations = 1000; // 防止無限循環
        int iterations = 0;

        while (currentNode != targetNode && iterations < maxIterations)
        {
            iterations++;
            
            // 獲取鄰居節點
            List<PathfindingNode> neighbors = pathfindingGrid.GetNeighbors(currentNode);
            
            // 過濾掉已訪問和不可行走的節點
            List<PathfindingNode> validNeighbors = new List<PathfindingNode>();
            foreach (PathfindingNode neighbor in neighbors)
            {
                if (neighbor.isWalkable && !visited.Contains(neighbor))
                {
                    validNeighbors.Add(neighbor);
                }
            }

            if (validNeighbors.Count == 0)
            {
                // 沒有可用的鄰居，嘗試回溯
                if (path.Count > 1)
                {
                    path.RemoveAt(path.Count - 1);
                    currentNode = path[path.Count - 1];
                    continue;
                }
                else
                {
                    Debug.LogWarning("貪心算法找不到路徑！");
                    return null;
                }
            }

            // 貪心選擇：選擇最接近目標的鄰居
            PathfindingNode bestNeighbor = validNeighbors[0];
            float bestDistance = GetDistance(bestNeighbor, targetNode);

            foreach (PathfindingNode neighbor in validNeighbors)
            {
                float distance = GetDistance(neighbor, targetNode);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNeighbor = neighbor;
                }
            }

            // 移動到下一個節點
            currentNode = bestNeighbor;
            path.Add(currentNode);
            visited.Add(currentNode);
            exploredNodes.Add(currentNode);
        }

        if (iterations >= maxIterations)
        {
            Debug.LogWarning("貪心算法達到最大迭代次數！");
            return null;
        }

        currentPath = path;
        return currentPath;
    }

    /// <summary>
    /// 計算兩個節點之間的距離
    /// </summary>
    private float GetDistance(PathfindingNode nodeA, PathfindingNode nodeB)
    {
        float dstX = Mathf.Abs(nodeA.gridPosition.x - nodeB.gridPosition.x);
        float dstY = Mathf.Abs(nodeA.gridPosition.y - nodeB.gridPosition.y);

        if (dstX > dstY)
        {
            return 14 * dstY + 10 * (dstX - dstY); // 對角線移動成本更高
        }
        return 14 * dstX + 10 * (dstY - dstX);
    }

    /// <summary>
    /// 重置所有節點
    /// </summary>
    private void ResetAllNodes()
    {
        for (int x = 0; x < pathfindingGrid.GridWidth; x++)
        {
            for (int y = 0; y < pathfindingGrid.GridHeight; y++)
            {
                PathfindingNode node = pathfindingGrid.GetNode(x, y);
                if (node != null)
                {
                    node.Reset();
                }
            }
        }
    }

    /// <summary>
    /// 獲取當前路徑
    /// </summary>
    public List<PathfindingNode> GetCurrentPath()
    {
        return currentPath;
    }

    /// <summary>
    /// 獲取探索過的節點（除錯用）
    /// </summary>
    public List<PathfindingNode> GetExploredNodes()
    {
        return exploredNodes;
    }

    /// <summary>
    /// 簡化路徑（移除不必要的節點）
    /// </summary>
    public List<PathfindingNode> SimplifyPath(List<PathfindingNode> path)
    {
        if (path == null || path.Count <= 2)
        {
            return path;
        }

        List<PathfindingNode> simplifiedPath = new List<PathfindingNode>();
        simplifiedPath.Add(path[0]);

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2 directionOld = (path[i].worldPosition - path[i - 1].worldPosition).normalized;
            Vector2 directionNew = (path[i + 1].worldPosition - path[i].worldPosition).normalized;

            if (directionOld != directionNew)
            {
                simplifiedPath.Add(path[i]);
            }
        }

        simplifiedPath.Add(path[path.Count - 1]);
        return simplifiedPath;
    }

    /// <summary>
    /// 在Scene視圖中繪製路徑（除錯用）
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugPath || currentPath == null) return;

        // 繪製路徑
        Gizmos.color = pathColor;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i].worldPosition, currentPath[i + 1].worldPosition);
        }

        // 繪製探索過的節點
        Gizmos.color = exploredColor;
        foreach (PathfindingNode node in exploredNodes)
        {
            Gizmos.DrawWireCube(node.worldPosition, Vector3.one * 0.3f);
        }
    }
}
