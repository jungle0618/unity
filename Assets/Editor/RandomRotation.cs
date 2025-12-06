using UnityEngine;
using UnityEditor;

public static class RandomRotation
{
    [MenuItem("Tools/Randomize Floor Tile Rotation")]
    private static void RandomizeSelected()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {

            if (obj.transform.childCount == 0)
            {
                Debug.LogWarning($"{obj.name} has no children to rotate.");
                continue;
            }

            Transform child = obj.transform.GetChild(0);
            Undo.RecordObject(obj.transform, "Randomize Floor Rotation");

            

            float randomY = Random.Range(0, 4) * 90f; // 0, 90, 180, 270

            Vector3 euler = child.localEulerAngles;
            euler.y = randomY;
            child.localEulerAngles = euler;

            EditorUtility.SetDirty(child);
        }
    }
}
