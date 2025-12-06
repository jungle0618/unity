using UnityEditor;
using UnityEngine;

public class MarkStaticGI
{
    [MenuItem("Tools/Mark static + GI for Inner 3D Models")]
    static void MarkInnerMeshes()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No prefabs selected in Project window.");
            return;
        }

        foreach (GameObject prefab in selectedObjects)
        {
            // Ensure it's a prefab asset
            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
                continue;

            // Load prefab asset
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue;

            // Find all children with a MeshRenderer
            MeshRenderer[] meshRenderers = prefabAsset.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer mr in meshRenderers)
            {
                GameObject model = mr.gameObject;
                GameObjectUtility.SetStaticEditorFlags(
                    model,
                    StaticEditorFlags.ContributeGI |
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.ReflectionProbeStatic
                );    
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                
                EditorUtility.SetDirty(model);
     
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Selected prefab inner meshes set to Static + GI.");
    }
}
