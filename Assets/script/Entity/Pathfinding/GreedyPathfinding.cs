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
    [SerializeField] private int maxIterations = 1000; // 最大迭代次數
    [SerializeField] private bool enablePathSmoothing = true; // 啟用路徑平滑
    
    [Header("貪心算法優化")]
    [SerializeField] private bool useImprovedGreedy = true; // 使用改進的貪心算法
    [SerializeField] private float explorationBonus = 0.1f; // 探索獎勵，鼓勵探索新區域
    [SerializeField] private int maxBacktrackSteps = 10; // 最大回溯步數

    private List<PathfindingNode> currentPath = new List<PathfindingNode>();
    private List<PathfindingNode> exploredNodes = new List<PathfindingNode>();

    private void Awake()
    {
        if (pathfindingGrid == null)
        {
            pathfindingGrid = FindFirstObjectByType<PathfindingGrid>();
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
        //Debug.Log($"GreedyPathfinding: 開始計算路徑從 {startPos} 到 {targetPos}");
        
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
            Debug.LogWarning($"起點或終點不可行走！起點可走: {startNode.isWalkable}, 終點可走: {targetNode.isWalkable}");
            return null;
        }

        //Debug.Log($"GreedyPathfinding: 使用改進貪心算法: {useImprovedGreedy}");
        
        if (useImprovedGreedy)
        {
            return FindPathImproved(startNode, targetNode);
        }
        else
        {
            return FindPath(startNode, targetNode);
        }
    }

    /// <summary>
    /// 計算從起點到終點的路徑（優化貪心算法）
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
        // 不將起點加入路徑，因為敵人已經在那裡
        visited.Add(currentNode);
        exploredNodes.Add(currentNode);

        int iterations = 0;
        int stuckCounter = 0; // 防止在局部最優解中卡住

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
                if (path.Count > 0)
                {
                    // 移除當前節點並回到上一個節點
                    visited.Remove(currentNode);
                    currentNode = path[path.Count - 1];
                    path.RemoveAt(path.Count - 1);
                    stuckCounter++;
                    
                    // 如果回溯太多次，放棄
                    if (stuckCounter > 10)
                    {
                        Debug.LogWarning("貪心算法陷入死路，無法找到路徑！");
                        return null;
                    }
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

            // 檢查是否真的在接近目標
            float currentDistance = GetDistance(currentNode, targetNode);
            float nextDistance = GetDistance(bestNeighbor, targetNode);
            
            // 如果沒有更接近目標，增加stuck counter
            if (nextDistance >= currentDistance)
            {
                stuckCounter++;
            }
            else
            {
                stuckCounter = 0; // 重置stuck counter
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

        // 如果啟用路徑平滑，對路徑進行平滑處理
        if (enablePathSmoothing && path.Count > 2)
        {
            currentPath = SmoothPath(path);
        }
        else
        {
            currentPath = path;
        }
        
        // 除錯信息：輸出路徑
        //Debug.Log($"優化貪心算法找到路徑，包含 {currentPath.Count} 個節點，迭代次數: {iterations}");
        for (int i = 0; i < currentPath.Count; i++)
        {
            //Debug.Log($"  路徑點 {i}: {currentPath[i].worldPosition}");
        }
        
        return currentPath;
    }

    /// <summary>
    /// 改進的貪心算法（更好的避免重複路徑）
    /// </summary>
    private List<PathfindingNode> FindPathImproved(PathfindingNode startNode, PathfindingNode targetNode)
    {
        List<PathfindingNode> path = new List<PathfindingNode>();
        HashSet<PathfindingNode> visited = new HashSet<PathfindingNode>();
        Dictionary<PathfindingNode, int> visitCount = new Dictionary<PathfindingNode, int>(); // 記錄每個節點的訪問次數
        exploredNodes.Clear();

        // 重置所有節點
        ResetAllNodes();

        // 檢查起點和終點是否相同或非常接近
        if (startNode == targetNode)
        {
            //Debug.Log("起點和終點是同一節點，返回空路徑");
            return path; // 返回空路徑，表示已經在目標位置
        }

        // 檢查起點和終點是否非常接近（在同一個網格單元內）
        float distance = Vector2.Distance(startNode.worldPosition, targetNode.worldPosition);
        if (distance < pathfindingGrid.CellSize * 0.5f)
        {
            //Debug.Log($"起點和終點非常接近 (距離: {distance:F2})，返回空路徑");
            return path; // 返回空路徑，表示已經在目標位置附近
        }

        PathfindingNode currentNode = startNode;
        visited.Add(currentNode);
        exploredNodes.Add(currentNode);
        visitCount[currentNode] = 1;

        int iterations = 0;
        int backtrackSteps = 0;

        while (currentNode != targetNode && iterations < maxIterations)
        {
            iterations++;
            
            // 獲取鄰居節點
            List<PathfindingNode> neighbors = pathfindingGrid.GetNeighbors(currentNode);
            
            // 過濾掉不可行走的節點
            List<PathfindingNode> validNeighbors = new List<PathfindingNode>();
            foreach (PathfindingNode neighbor in neighbors)
            {
                if (neighbor.isWalkable)
                {
                    validNeighbors.Add(neighbor);
                }
            }

            if (validNeighbors.Count == 0)
            {
                // 沒有可用的鄰居，回溯
                if (path.Count > 0)
                {
                    visited.Remove(currentNode);
                    currentNode = path[path.Count - 1];
                    path.RemoveAt(path.Count - 1);
                    backtrackSteps++;
                    
                    if (backtrackSteps > maxBacktrackSteps)
                    {
                        Debug.LogWarning("改進貪心算法回溯次數過多，無法找到路徑！");
                        return null;
                    }
                    continue;
                }
                else
                {
                    Debug.LogWarning("改進貪心算法找不到路徑！");
                    return null;
                }
            }

            // 改進的貪心選擇：考慮距離和訪問次數
            PathfindingNode bestNeighbor = validNeighbors[0];
            float bestScore = CalculateNodeScore(bestNeighbor, targetNode, visitCount);

            foreach (PathfindingNode neighbor in validNeighbors)
            {
                float score = CalculateNodeScore(neighbor, targetNode, visitCount);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNeighbor = neighbor;
                }
            }

            // 移動到下一個節點
            currentNode = bestNeighbor;
            path.Add(currentNode);
            visited.Add(currentNode);
            exploredNodes.Add(currentNode);
            
            // 更新訪問次數
            if (visitCount.ContainsKey(currentNode))
            {
                visitCount[currentNode]++;
            }
            else
            {
                visitCount[currentNode] = 1;
            }
            
            backtrackSteps = 0; // 重置回溯計數器
        }

        if (iterations >= maxIterations)
        {
            //Debug.Log("改進貪心算法達到最大迭代次數！");
            return null;
        }

        // 路徑後處理：優化和平滑
        currentPath = path;
        
        // 首先優化路徑（移除重複節點）
        if (currentPath.Count > 2)
        {
            currentPath = OptimizePath(currentPath);
        }
        
        // 然後平滑路徑（減少急轉彎）
        if (enablePathSmoothing && currentPath.Count > 2)
        {
            currentPath = SmoothPath(currentPath);
        }
        
        // 除錯信息：輸出路徑
        //Debug.Log($"改進貪心算法找到路徑，包含 {currentPath.Count} 個節點，迭代次數: {iterations}");
        for (int i = 0; i < currentPath.Count; i++)
        {
            //Debug.Log($"  路徑點 {i}: {currentPath[i].worldPosition}");
        }
        
        return currentPath;
    }

    /// <summary>
    /// 計算節點分數（距離 + 訪問次數懲罰）
    /// </summary>
    private float CalculateNodeScore(PathfindingNode node, PathfindingNode target, Dictionary<PathfindingNode, int> visitCount)
    {
        float distance = GetDistance(node, target);
        int visits = visitCount.ContainsKey(node) ? visitCount[node] : 0;
        
        // 訪問次數越多，分數越高（越不優先選擇）
        float visitPenalty = visits * explorationBonus;
        
        return distance + visitPenalty;
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
    /// 平滑路徑（減少急轉彎）
    /// </summary>
    private List<PathfindingNode> SmoothPath(List<PathfindingNode> path)
    {
        if (path == null || path.Count <= 2)
        {
            return path;
        }

        List<PathfindingNode> smoothedPath = new List<PathfindingNode>();
        smoothedPath.Add(path[0]);

        for (int i = 1; i < path.Count - 1; i++)
        {
            // 檢查是否需要平滑轉彎
            Vector2 prevDir = (path[i].worldPosition - path[i - 1].worldPosition).normalized;
            Vector2 nextDir = (path[i + 1].worldPosition - path[i].worldPosition).normalized;
            
            float angle = Vector2.Angle(prevDir, nextDir);
            
            // 如果轉彎角度太大，保留這個節點
            if (angle > 45f)
            {
                smoothedPath.Add(path[i]);
            }
        }

        smoothedPath.Add(path[path.Count - 1]);
        return smoothedPath;
    }

    /// <summary>
    /// 優化路徑（移除重複和不必要的節點）
    /// </summary>
    private List<PathfindingNode> OptimizePath(List<PathfindingNode> path)
    {
        if (path == null || path.Count <= 2)
        {
            return path;
        }

        List<PathfindingNode> optimizedPath = new List<PathfindingNode>();
        optimizedPath.Add(path[0]);

        for (int i = 1; i < path.Count - 1; i++)
        {
            // 檢查當前節點是否與前一個節點相同
            if (path[i].gridPosition != path[i - 1].gridPosition)
            {
                // 檢查是否可以直線到達下一個節點（跳過當前節點）
                bool canSkip = CanDirectlyReach(path[i - 1], path[i + 1]);
                
                if (!canSkip)
                {
                    optimizedPath.Add(path[i]);
                }
            }
        }

        optimizedPath.Add(path[path.Count - 1]);
        return optimizedPath;
    }

    /// <summary>
    /// 檢查兩個節點之間是否有直線路徑（沒有障礙物）
    /// </summary>
    private bool CanDirectlyReach(PathfindingNode from, PathfindingNode to)
    {
        Vector2 fromPos = from.worldPosition;
        Vector2 toPos = to.worldPosition;
        Vector2 direction = (toPos - fromPos).normalized;
        float distance = Vector2.Distance(fromPos, toPos);
        
        // 檢查路徑上是否有障礙物
        int steps = Mathf.CeilToInt(distance / pathfindingGrid.CellSize);
        for (int i = 1; i < steps; i++)
        {
            Vector2 checkPos = fromPos + direction * (i * pathfindingGrid.CellSize);
            PathfindingNode checkNode = pathfindingGrid.GetNode(checkPos);
            
            if (checkNode == null || !checkNode.isWalkable)
            {
                return false;
            }
        }
        
        return true;
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
