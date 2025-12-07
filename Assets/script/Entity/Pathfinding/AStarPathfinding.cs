using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A* 路徑規劃算法
/// 使用啟發式搜索找到最短路徑
/// 比貪心算法更可靠，保證找到最優路徑（如果存在）
/// </summary>
public class AStarPathfinding : MonoBehaviour
{
    [Header("路徑規劃設定")]
    [SerializeField] private PathfindingGrid pathfindingGrid;
    [SerializeField] private bool showDebugPath = true;
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private int maxIterations = 2000; // 最大迭代次數
    [SerializeField] private bool enablePathSmoothing = true; // 啟用路徑平滑
    [SerializeField] private bool allowDiagonal = true; // 允許對角移動
    
    private List<PathfindingNode> currentPath = new List<PathfindingNode>();

    private void Awake()
    {
        if (pathfindingGrid == null)
        {
            pathfindingGrid = FindFirstObjectByType<PathfindingGrid>();
        }
    }

    /// <summary>
    /// 計算從起點到終點的路徑（A* 算法）
    /// </summary>
    public List<PathfindingNode> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        if (showDebugPath)
        {
            //Debug.Log($"[A*] 開始尋路: {startPos} → {targetPos}");
        }
        
        if (pathfindingGrid == null)
        {
            Debug.LogError("[A*] PathfindingGrid 未設定！");
            return null;
        }

        PathfindingNode startNode = pathfindingGrid.GetNode(startPos);
        PathfindingNode targetNode = pathfindingGrid.GetNode(targetPos);

        if (startNode == null || targetNode == null)
        {
            Debug.LogWarning($"[A*] 無法找到起點或終點節點");
            return null;
        }

        if (!startNode.isWalkable)
        {
            // 如果起點不可走，找最近的可走點
            startNode = pathfindingGrid.GetNearestWalkableNode(startPos, 5f);
            if (startNode == null)
            {
                Debug.LogWarning("[A*] 起點附近沒有可走的節點");
                return null;
            }
        }
        
        if (!targetNode.isWalkable)
        {
            // 如果終點不可走，找最近的可走點
            targetNode = pathfindingGrid.GetNearestWalkableNode(targetPos, 5f);
            if (targetNode == null)
            {
                Debug.LogWarning("[A*] 終點附近沒有可走的節點");
                return null;
            }
        }

        return FindPathAStar(startNode, targetNode);
    }

    /// <summary>
    /// A* 算法核心實現
    /// </summary>
    private List<PathfindingNode> FindPathAStar(PathfindingNode startNode, PathfindingNode targetNode)
    {
        // 檢查起點和終點是否相同
        if (startNode == targetNode)
        {
            if (showDebugPath) //Debug.Log("[A*] 起點和終點相同，返回空路徑");
            return new List<PathfindingNode>();
        }

        // 開放列表（待探索的節點）
        List<PathfindingNode> openSet = new List<PathfindingNode>();
        // 關閉列表（已探索的節點）
        HashSet<PathfindingNode> closedSet = new HashSet<PathfindingNode>();
        
        // G值：從起點到當前節點的實際代價
        Dictionary<PathfindingNode, float> gScore = new Dictionary<PathfindingNode, float>();
        // F值：G值 + H值（啟發式估計到終點的代價）
        Dictionary<PathfindingNode, float> fScore = new Dictionary<PathfindingNode, float>();
        // 用於回溯路徑
        Dictionary<PathfindingNode, PathfindingNode> cameFrom = new Dictionary<PathfindingNode, PathfindingNode>();
        
        // 初始化起點
        openSet.Add(startNode);
        gScore[startNode] = 0;
        fScore[startNode] = Heuristic(startNode, targetNode);
        
        int iterations = 0;
        
        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            // 從開放列表中找到F值最小的節點
            PathfindingNode current = GetLowestFScoreNode(openSet, fScore);
            
            // 如果到達目標，重建路徑
            if (current == targetNode)
            {
                List<PathfindingNode> path = ReconstructPath(cameFrom, current);
                
                // 路徑平滑
                if (enablePathSmoothing && path.Count > 2)
                {
                    path = SmoothPath(path);
                }
                
                currentPath = path;
                if (showDebugPath)
                {
                    string pathStr = "路徑: ";
                    for (int i = 0; i < path.Count; i++)
                    {
                        pathStr += $"[{i}]({path[i].worldPosition.x:F1}, {path[i].worldPosition.y:F1})";
                        if (i < path.Count - 1) pathStr += " → ";
                    }
                    //Debug.Log($"[A*] ✓ 找到路徑! 節點數: {path.Count}, 迭代: {iterations}\n{pathStr}");
                }
                return path;
            }
            
            openSet.Remove(current);
            closedSet.Add(current);
            
            // 檢查所有鄰居
            List<PathfindingNode> neighbors = pathfindingGrid.GetNeighbors(current);
            
            foreach (PathfindingNode neighbor in neighbors)
            {
                // 跳過不可走的節點和已在關閉列表中的節點
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }
                
                // 計算從起點到鄰居的代價
                float tentativeGScore = gScore[current] + GetDistance(current, neighbor);
                
                // 如果這條路徑更好，或者鄰居還沒被探索過
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    // 記錄路徑
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, targetNode);
                    
                    // 如果鄰居不在開放列表中，添加它
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        
        // 找不到路徑
        if (iterations >= maxIterations)
        {
            Debug.LogWarning($"[A*] 達到最大迭代次數 {maxIterations}，無法找到路徑");
        }
        else
        {
            Debug.LogWarning("[A*] 無法找到路徑");
        }
        
        return null;
    }
    
    /// <summary>
    /// 從開放列表中獲取F值最小的節點
    /// </summary>
    private PathfindingNode GetLowestFScoreNode(List<PathfindingNode> openSet, Dictionary<PathfindingNode, float> fScore)
    {
        PathfindingNode lowest = openSet[0];
        float lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;
        
        for (int i = 1; i < openSet.Count; i++)
        {
            float score = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : float.MaxValue;
            if (score < lowestScore)
            {
                lowestScore = score;
                lowest = openSet[i];
            }
        }
        
        return lowest;
    }
    
    /// <summary>
    /// 啟發式函數：估計從當前節點到目標的距離
    /// 使用曼哈頓距離（適合網格）或歐幾里得距離
    /// </summary>
    private float Heuristic(PathfindingNode a, PathfindingNode b)
    {
        if (allowDiagonal)
        {
            // 使用歐幾里得距離（允許對角移動）
            return Vector2.Distance(a.worldPosition, b.worldPosition);
        }
        else
        {
            // 使用曼哈頓距離（只允許上下左右移動）
            return Mathf.Abs(a.worldPosition.x - b.worldPosition.x) + 
                   Mathf.Abs(a.worldPosition.y - b.worldPosition.y);
        }
    }
    
    /// <summary>
    /// 計算兩個節點之間的實際距離
    /// </summary>
    private float GetDistance(PathfindingNode a, PathfindingNode b)
    {
        return Vector2.Distance(a.worldPosition, b.worldPosition);
    }
    
    /// <summary>
    /// 重建路徑（從終點回溯到起點）
    /// </summary>
    private List<PathfindingNode> ReconstructPath(Dictionary<PathfindingNode, PathfindingNode> cameFrom, PathfindingNode current)
    {
        List<PathfindingNode> path = new List<PathfindingNode>();
        path.Add(current);
        
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current); // 插入到前面，使路徑從起點到終點
        }
        
        // 移除起點（因為實體已經在起點）
        if (path.Count > 0)
        {
            path.RemoveAt(0);
        }
        
        return path;
    }
    
    /// <summary>
    /// 路徑平滑：移除不必要的轉折點
    /// </summary>
    private List<PathfindingNode> SmoothPath(List<PathfindingNode> path)
    {
        if (path == null || path.Count <= 2)
        {
            return path;
        }
        
        List<PathfindingNode> smoothed = new List<PathfindingNode>();
        smoothed.Add(path[0]); // 添加起點
        
        int checkIndex = 0;
        
        while (checkIndex < path.Count - 1)
        {
            // 嘗試從當前點直接連到最遠的可見點
            for (int i = path.Count - 1; i > checkIndex; i--)
            {
                if (HasLineOfSight(path[checkIndex], path[i]))
                {
                    smoothed.Add(path[i]);
                    checkIndex = i;
                    break;
                }
                
                // 如果找不到更遠的點，就用下一個點
                if (i == checkIndex + 1)
                {
                    smoothed.Add(path[checkIndex + 1]);
                    checkIndex++;
                }
            }
        }
        
        return smoothed;
    }
    
    /// <summary>
    /// 檢查兩個節點之間是否有視線（用於路徑平滑）
    /// </summary>
    private bool HasLineOfSight(PathfindingNode from, PathfindingNode to)
    {
        Vector2 direction = (to.worldPosition - from.worldPosition).normalized;
        float distance = Vector2.Distance(from.worldPosition, to.worldPosition);
        
        // 使用較小的圓形投射檢測障礙物
        RaycastHit2D hit = Physics2D.CircleCast(
            from.worldPosition, 
            pathfindingGrid.CellSize * 0.3f, 
            direction, 
            distance,
            LayerMask.GetMask("Walls", "Obstacles", "Objects")
        );
        
        return hit.collider == null;
    }
    
    /// <summary>
    /// 獲取當前路徑（用於可視化）
    /// </summary>
    public List<PathfindingNode> GetCurrentPath()
    {
        return currentPath;
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugPath || currentPath == null || currentPath.Count == 0)
        {
            return;
        }
        
        // 繪製路徑
        Gizmos.color = pathColor;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i].worldPosition, currentPath[i + 1].worldPosition);
        }
        
        // 繪製路徑點
        foreach (var node in currentPath)
        {
            Gizmos.DrawSphere(node.worldPosition, 0.2f);
        }
    }
}

