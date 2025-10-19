using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DoorManager : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap visualTilemap;    // ��ܥ�
    public Tilemap collisionTilemap; // �� collider tile �� Tilemap�]�P visual ����^

    [Header("Tiles")]
    public TileBase doorClosedVisual;
    public TileBase doorOpenVisual;
    public TileBase doorClosedCollider; // �o�O�� collider �� Tile�]��b collisionTilemap�^
    // �Y�}���ɭn���� collider�A������ collisionTilemap ���Ӯ�]�� null

    //�]�i��^�ƥ����y�Ҧ������m
    private HashSet<Vector3Int> doorCells = new HashSet<Vector3Int>();

    void Start()
    {
        // ���y visualTilemap �d��A��X�@�}�l�O doorClosedVisual ����l
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
        // �u���b�ڭ̬���������m�� visual �T�{�O���~������
        if (!doorCells.Contains(cell) && visualTilemap.GetTile(cell) != doorClosedVisual && visualTilemap.GetTile(cell) != doorOpenVisual)
            return;

        var current = visualTilemap.GetTile(cell);
        if (current == doorClosedVisual)
        {
            // �}���G�� visual�A���� collision tile
            visualTilemap.SetTile(cell, doorOpenVisual);
            collisionTilemap.SetTile(cell, null);
        }
        else
        {
            // �����G�� visual�A��W collision tile
            visualTilemap.SetTile(cell, doorClosedVisual);
            collisionTilemap.SetTile(cell, doorClosedCollider);
        }
        // �Y�ϥ� CompositeCollider2D + TilemapCollider2D�ATilemap �|�۰ʭ��s�ͦ��I���Ϊ�
    }

    public void ToggleDoorAtWorldPos(Vector3 worldPos)
    {
        Vector3Int cell = visualTilemap.WorldToCell(worldPos);
        ToggleDoorAtCell(cell);
    }
}
