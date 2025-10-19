using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DoorManager : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap visualTilemap;    // 顯示用
    public Tilemap collisionTilemap; // 放 collider tile 的 tilemap（與 visual 對齊）

    [Header("Tiles")]
    public TileBase doorClosedVisual;
    public TileBase doorOpenVisual;
    public TileBase doorClosedCollider; // 這是有 collider 的 tile（放在 collision tilemap）
    // 若開門時要移除 collider，直接把 collision tilemap 的該格設為 null

    //（可選）事先掃描所有門格位置
    private HashSet<Vector3Int> doorCells = new HashSet<Vector3Int>();

    void Start()
    {
        // 掃描 visual tilemap 範圍，找出一開始是 doorClosedVisual 的格子
        var bounds = visualTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (visualTilemap.GetTile(cell) == doorClosedVisual)
                {
                    doorCells.Add(cell);
                }
            }
    }

    public void ToggleDoorAtCell(Vector3Int cell)
    {
        // 只有在我們記錄的門位置或 visual 確認是門才做切換
        if (!doorCells.Contains(cell) && visualTilemap.GetTile(cell) != doorClosedVisual && visualTilemap.GetTile(cell) != doorOpenVisual)
            return;

        var current = visualTilemap.GetTile(cell);
        if (current == doorClosedVisual)
        {
            // 開門：換 visual，移除 collision tile
            visualTilemap.SetTile(cell, doorOpenVisual);
            collisionTilemap.SetTile(cell, null);
        }
        else
        {
            // 關門：換 visual，放上 collision tile
            visualTilemap.SetTile(cell, doorClosedVisual);
            collisionTilemap.SetTile(cell, doorClosedCollider);
        }
        // 若使用 CompositeCollider2D + tilemapCollider2D，tilemap 會自動重新生成碰撞形狀
    }

    public void ToggleDoorAtWorldPos(Vector3 worldPos)
    {
        Vector3Int cell = visualTilemap.WorldToCell(worldPos);
        ToggleDoorAtCell(cell);
    }
}