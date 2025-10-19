using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 門的腳本，用於管理Tilemap中的門
/// </summary>
public class Door : MonoBehaviour
{
    [Header("門的設定")]
    [SerializeField] private Vector3Int doorPosition;
    [SerializeField] private bool isOpen = false;
    
    [Header("Tilemap引用")]
    [SerializeField] private Tilemap visualTilemap;
    [SerializeField] private Tilemap collisionTilemap;
    
    [Header("門的Tile")]
    [SerializeField] private TileBase doorClosedVisual;
    [SerializeField] private TileBase doorOpenVisual;
    [SerializeField] private TileBase doorClosedCollider;
    
    /// <summary>
    /// 門的位置
    /// </summary>
    public Vector3Int DoorPosition => doorPosition;
    
    /// <summary>
    /// 門是否開啟
    /// </summary>
    public bool IsOpen => isOpen;
    
    /// <summary>
    /// 初始化門
    /// </summary>
    /// <param name="position">門的位置</param>
    /// <param name="visualTilemap">視覺Tilemap</param>
    /// <param name="collisionTilemap">碰撞Tilemap</param>
    /// <param name="closedVisual">關閉時的視覺Tile</param>
    /// <param name="openVisual">開啟時的視覺Tile</param>
    /// <param name="closedCollider">關閉時的碰撞Tile</param>
    public void Initialize(Vector3Int position, Tilemap visualTilemap, Tilemap collisionTilemap, 
                          TileBase closedVisual, TileBase openVisual, TileBase closedCollider)
    {
        doorPosition = position;
        this.visualTilemap = visualTilemap;
        this.collisionTilemap = collisionTilemap;
        doorClosedVisual = closedVisual;
        doorOpenVisual = openVisual;
        doorClosedCollider = closedCollider;
        
        // 初始化門的狀態
        isOpen = false;
        UpdateDoorVisual();
    }
    
    /// <summary>
    /// 開啟門
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen) return;
        
        isOpen = true;
        UpdateDoorVisual();
        Debug.Log($"門在位置 {doorPosition} 已開啟");
    }
    
    /// <summary>
    /// 關閉門
    /// </summary>
    public void CloseDoor()
    {
        if (!isOpen) return;
        
        isOpen = false;
        UpdateDoorVisual();
        Debug.Log($"門在位置 {doorPosition} 已關閉");
    }
    
    /// <summary>
    /// 切換門的狀態
    /// </summary>
    public void ToggleDoor()
    {
        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }
    
    /// <summary>
    /// 刪除門（直接從Tilemap中移除）
    /// </summary>
    public void RemoveDoor()
    {
        if (visualTilemap != null)
        {
            visualTilemap.SetTile(doorPosition, null);
        }
        
        if (collisionTilemap != null)
        {
            collisionTilemap.SetTile(doorPosition, null);
        }
        
        Debug.Log($"門在位置 {doorPosition} 已刪除");
        
        // 銷毀這個門物件
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 更新門的視覺效果
    /// </summary>
    private void UpdateDoorVisual()
    {
        if (visualTilemap == null) return;
        
        if (isOpen)
        {
            // 開啟狀態：顯示開啟的視覺，移除碰撞
            visualTilemap.SetTile(doorPosition, doorOpenVisual);
            if (collisionTilemap != null)
            {
                collisionTilemap.SetTile(doorPosition, null);
            }
        }
        else
        {
            // 關閉狀態：顯示關閉的視覺，添加碰撞
            visualTilemap.SetTile(doorPosition, doorClosedVisual);
            if (collisionTilemap != null)
            {
                collisionTilemap.SetTile(doorPosition, doorClosedCollider);
            }
        }
    }
    
    /// <summary>
    /// 檢查指定位置是否為此門
    /// </summary>
    /// <param name="position">要檢查的位置</param>
    /// <returns>是否為此門</returns>
    public bool IsAtPosition(Vector3Int position)
    {
        return doorPosition == position;
    }
    
    /// <summary>
    /// 檢查指定世界位置是否為此門
    /// </summary>
    /// <param name="worldPosition">要檢查的世界位置</param>
    /// <returns>是否為此門</returns>
    public bool IsAtWorldPosition(Vector3 worldPosition)
    {
        if (visualTilemap == null) return false;
        Vector3Int cellPosition = visualTilemap.WorldToCell(worldPosition);
        return IsAtPosition(cellPosition);
    }
}
