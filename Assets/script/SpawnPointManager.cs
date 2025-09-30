using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints = new Transform[0];

    // ��ҼҦ�
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

        // �C���}�l�����éҦ� spawn points
        HideSpawnPointsInGame();
    }

    /// <summary>
    /// ���éҦ� spawn points�]�C��������ܡ^
    /// </summary>
    void HideSpawnPointsInGame()
    {
        if (spawnPoints == null) return;

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                // ���� MeshRenderer�]�p�G�����ܡ^
                MeshRenderer renderer = point.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.enabled = false;

                // ���è�L�i�઺��V�ե�
                SpriteRenderer spriteRenderer = point.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// ���o�@���H�������ĥͦ���m�M����
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        if (HasValidSpawnPoints())
        {
            // ���է��D null �� spawn point
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
    /// ���o�H���ͦ��I����m�M����
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
    /// ���o�S�w���ު��ͦ���m
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

        // �L�o�X�D null ���ͦ��I
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
    /// �ˬd���ެO�_����
    /// </summary>
    private bool IsValidIndex(int index)
    {
        return spawnPoints != null && index >= 0 && index < spawnPoints.Length;
    }

    /// <summary>
    /// ���o���� spawn points ���ƶq
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
    /// �ˬd�O�_�����󦳮Ī� spawn point
    /// </summary>
    public bool HasValidSpawnPoints()
    {
        return GetValidSpawnPointCount() > 0;
    }

    // �b Scene ���Ϥ���� spawn points�]�u�b�s�边����ܡ^
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point != null)
            {
                // ��ܦ�m - �����
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(point.position, 0.5f);

                // ��ܤ�V - ����b�Y
                Gizmos.color = Color.red;
                Gizmos.DrawRay(point.position, point.forward * 2f);

                // ��ܽs������
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(point.position + Vector3.up * 0.8f, $"SP {i + 1}");
#endif
            }
        }
    }

    // �u�b�襤 SpawnPointManager ����ܸԲӸ�T
    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point != null)
            {
                // �襤����ܶ���ߤ����
                Gizmos.DrawWireCube(point.position, Vector3.one * 1.2f);

#if UNITY_EDITOR
                // ��ܮy�и�T
                UnityEditor.Handles.color = Color.cyan;
                string posText = $"({point.position.x:F1}, {point.position.y:F1}, {point.position.z:F1})";
                UnityEditor.Handles.Label(point.position + Vector3.down * 0.5f, posText);
#endif
            }
        }
    }
}