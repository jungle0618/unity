using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints = new Transform[0];

    // 單例模式
    public static SpawnPointManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 遊戲開始時隱藏所有 spawn points
        HideSpawnPointsInGame();
    }

    /// <summary>
    /// 隱藏所有 spawn points（遊戲中不顯示）
    /// </summary>
    void HideSpawnPointsInGame()
    {
        if (spawnPoints == null) return;

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                // 隱藏 MeshRenderer（如果有的話）
                MeshRenderer renderer = point.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.enabled = false;

                // 隱藏其他可能的渲染組件
                SpriteRenderer spriteRenderer = point.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// 取得一個隨機的有效生成位置和旋轉
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        if (HasValidSpawnPoints())
        {
            // 嘗試找到非 null 的 spawn point
            for (int tries = 0; tries < 10; tries++)
            {
                Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (randomPoint != null)
                {
                    return randomPoint.position;
                }
            }
        }

        Debug.LogWarning("No valid spawn points found!");
        return Vector3.zero;
    }

    /// <summary>
    /// 取得隨機生成點的位置和旋轉
    /// </summary>
    public bool GetRandomSpawnTransform(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (HasValidSpawnPoints())
        {
            for (int tries = 0; tries < 10; tries++)
            {
                Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (randomPoint != null)
                {
                    position = randomPoint.position;
                    rotation = randomPoint.rotation;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 取得特定索引的生成位置
    /// </summary>
    public Vector3 GetSpawnPosition(int index)
    {
        if (IsValidIndex(index) && spawnPoints[index] != null)
        {
            return spawnPoints[index].position;
        }

        Debug.LogWarning($"Invalid spawn point index: {index}");
        return Vector3.zero;
    }
    public Transform[] GetAllSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("SpawnPointManager: No spawn points available");
            return new Transform[0];
        }

        // 過濾出非 null 的生成點
        var validSpawnPoints = new System.Collections.Generic.List<Transform>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                validSpawnPoints.Add(spawnPoints[i]);
            }
        }

        return validSpawnPoints.ToArray();
    }

    /// <summary>
    /// 檢查索引是否有效
    /// </summary>
    private bool IsValidIndex(int index)
    {
        return spawnPoints != null && index >= 0 && index < spawnPoints.Length;
    }

    /// <summary>
    /// 取得有效 spawn points 的數量
    /// </summary>
    public int GetValidSpawnPointCount()
    {
        if (spawnPoints == null) return 0;

        int count = 0;
        foreach (Transform point in spawnPoints)
        {
            if (point != null) count++;
        }
        return count;
    }

    /// <summary>
    /// 檢查是否有任何有效的 spawn point
    /// </summary>
    public bool HasValidSpawnPoints()
    {
        return GetValidSpawnPointCount() > 0;
    }

    // 在 Scene 視圖中顯示 spawn points（只在編輯器中顯示）
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point != null)
            {
                // 顯示位置 - 綠色圓圈
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(point.position, 0.5f);

                // 顯示方向 - 紅色箭頭
                Gizmos.color = Color.red;
                Gizmos.DrawRay(point.position, point.forward * 2f);

                // 顯示編號標籤
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(point.position + Vector3.up * 0.8f, $"SP {i + 1}");
#endif
            }
        }
    }

    // 只在選中 SpawnPointManager 時顯示詳細資訊
    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point != null)
            {
                // 選中時顯示黃色立方體框
                Gizmos.DrawWireCube(point.position, Vector3.one * 1.2f);

#if UNITY_EDITOR
                // 顯示座標資訊
                UnityEditor.Handles.color = Color.cyan;
                string posText = $"({point.position.x:F1}, {point.position.y:F1}, {point.position.z:F1})";
                UnityEditor.Handles.Label(point.position + Vector3.down * 0.5f, posText);
#endif
            }
        }
    }
}