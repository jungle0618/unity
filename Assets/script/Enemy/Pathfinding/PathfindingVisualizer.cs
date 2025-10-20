using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 路徑規劃可視化組件
/// 用於在Scene視圖中顯示路徑和網格
/// </summary>
public class PathfindingVisualizer : MonoBehaviour
{
    [Header("可視化設定")]
    [SerializeField] private bool showPath = true;
    [SerializeField] private bool showGrid = false;
    [SerializeField] private bool showExploredNodes = false;
    
    [Header("顏色設定")]
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private Color exploredColor = Color.yellow;
    [SerializeField] private Color gridColor = Color.white;
    [SerializeField] private Color obstacleColor = Color.red;
    [SerializeField] private Color walkableColor = Color.white;
    
    [Header("線條設定")]
    [SerializeField] private float pathLineWidth = 0.1f;
    [SerializeField] private float gridLineWidth = 0.05f;
    
    private GreedyPathfinding pathfinding;
    private PathfindingGrid pathfindingGrid;
    private List<PathfindingNode> currentPath = new List<PathfindingNode>();
    private List<PathfindingNode> exploredNodes = new List<PathfindingNode>();

    private void Awake()
    {
        pathfinding = FindObjectOfType<GreedyPathfinding>();
        pathfindingGrid = FindObjectOfType<PathfindingGrid>();
    }

    private void Update()
    {
        if (pathfinding != null)
        {
            currentPath = pathfinding.GetCurrentPath();
            exploredNodes = pathfinding.GetExploredNodes();
        }
    }

    /// <summary>
    /// 設定路徑
    /// </summary>
    public void SetPath(List<PathfindingNode> path)
    {
        currentPath = path;
    }

    /// <summary>
    /// 設定探索過的節點
    /// </summary>
    public void SetExploredNodes(List<PathfindingNode> nodes)
    {
        exploredNodes = nodes;
    }

    /// <summary>
    /// 清除可視化
    /// </summary>
    public void ClearVisualization()
    {
        currentPath.Clear();
        exploredNodes.Clear();
    }

    private void OnDrawGizmos()
    {
        if (showPath && currentPath != null && currentPath.Count > 0)
        {
            DrawPath();
        }

        if (showExploredNodes && exploredNodes != null && exploredNodes.Count > 0)
        {
            DrawExploredNodes();
        }

        if (showGrid && pathfindingGrid != null)
        {
            DrawGrid();
        }
    }

    /// <summary>
    /// 繪製路徑
    /// </summary>
    private void DrawPath()
    {
        Gizmos.color = pathColor;
        
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 start = currentPath[i].worldPosition;
            Vector3 end = currentPath[i + 1].worldPosition;
            
            // 繪製線條
            Gizmos.DrawLine(start, end);
            
            // 繪製箭頭指示方向
            Vector3 direction = (end - start).normalized;
            Vector3 arrowHead = end - direction * 0.2f;
            Vector3 arrowLeft = arrowHead + new Vector3(-direction.y, direction.x, 0) * 0.1f;
            Vector3 arrowRight = arrowHead + new Vector3(direction.y, -direction.x, 0) * 0.1f;
            
            Gizmos.DrawLine(end, arrowLeft);
            Gizmos.DrawLine(end, arrowRight);
        }
    }

    /// <summary>
    /// 繪製探索過的節點
    /// </summary>
    private void DrawExploredNodes()
    {
        Gizmos.color = exploredColor;
        
        foreach (PathfindingNode node in exploredNodes)
        {
            Gizmos.DrawWireCube(node.worldPosition, Vector3.one * 0.3f);
        }
    }

    /// <summary>
    /// 繪製網格
    /// </summary>
    private void DrawGrid()
    {
        if (pathfindingGrid == null) return;

        Gizmos.color = gridColor;
        
        // 繪製網格線
        for (int x = 0; x <= pathfindingGrid.GridWidth; x++)
        {
            Vector3 start = pathfindingGrid.GetWorldPosition(x, 0);
            Vector3 end = pathfindingGrid.GetWorldPosition(x, pathfindingGrid.GridHeight);
            Gizmos.DrawLine(start, end);
        }
        
        for (int y = 0; y <= pathfindingGrid.GridHeight; y++)
        {
            Vector3 start = pathfindingGrid.GetWorldPosition(0, y);
            Vector3 end = pathfindingGrid.GetWorldPosition(pathfindingGrid.GridWidth, y);
            Gizmos.DrawLine(start, end);
        }

        // 繪製節點
        for (int x = 0; x < pathfindingGrid.GridWidth; x++)
        {
            for (int y = 0; y < pathfindingGrid.GridHeight; y++)
            {
                PathfindingNode node = pathfindingGrid.GetNode(x, y);
                if (node != null)
                {
                    Gizmos.color = node.isWalkable ? walkableColor : obstacleColor;
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * 0.8f);
                }
            }
        }
    }

    /// <summary>
    /// 在Scene視圖中繪製GUI信息
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            // 顯示路徑信息
            Vector3 screenPos = Camera.current.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"路徑長度: {currentPath.Count}");
            }
        }
    }
}
