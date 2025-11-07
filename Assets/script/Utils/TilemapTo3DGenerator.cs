using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

[System.Serializable]
public struct Tile3DMapping
{
    public TileBase tile;
    public GameObject prefab3D;
}



[ExecuteInEditMode]
public class TilemapTo3DGenerator : MonoBehaviour
{
    [Header("Source 2D Tilemap")]
    public Tilemap sourceTilemap;

    [Header("Mappings")]
    public List<Tile3DMapping> tileMappings = new List<Tile3DMapping>();

    [Header("Output Parent (optional)")]
    public Transform outputParent;

    private Dictionary<TileBase, GameObject> mappingDict;

#if UNITY_EDITOR
    [ContextMenu("Generate 3D Map")]
    public void Generate3DMap()
    {
        if (sourceTilemap == null)
        {
            Debug.LogError("No source tilemap assigned");
            return;
        }

        if (tileMappings.Count == 0)
        {
            Debug.LogError("No tile mappings provided");
            return;
        }

        // Build lookup
        mappingDict = new Dictionary<TileBase, GameObject>();
        foreach (var map in tileMappings)
        {
            if (map.tile != null && map.prefab3D != null)
                mappingDict[map.tile] = map.prefab3D;
        }

        Undo.RegisterFullObjectHierarchyUndo(this.gameObject, "Generate 3D Map");

        int count = 0;
        BoundsInt bounds = sourceTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase tile = sourceTilemap.GetTile(pos);
            Vector3 cellWorld = sourceTilemap.CellToWorld(pos);

            if (tile == null) continue;

            if (mappingDict.TryGetValue(tile, out GameObject prefab))
            {
                Vector3 worldPos = new Vector3(
                    cellWorld.x + sourceTilemap.cellSize.x / 2f,
                    cellWorld.y + sourceTilemap.cellSize.y / 2f,
                    0f);

                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                obj.transform.position = worldPos;
                obj.transform.SetParent(outputParent != null ? outputParent : transform);
                obj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); 

                Undo.RegisterCreatedObjectUndo(obj, "Generated 3D Tile");
                count++;
            }
            else
            {
                Debug.LogWarning($"No mapping found for '{tile.name}' at {pos}");
            }
        }

        Debug.Log($"Done");
    }
#endif
}

