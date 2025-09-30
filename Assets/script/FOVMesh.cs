using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlayerFOV : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private PlayerManager playerManager;

    [Header("視野設定")]
    [SerializeField] private float viewRadius = 5f;
    [SerializeField, Range(0, 360)] private float viewAngle = 90f;
    [SerializeField] private int rayCount = 120;

    [Header("障礙物設定")]
    [SerializeField] private LayerMask wallLayer = -1;
    [SerializeField] private float raycastOffset = 0.01f;

    [Header("迷霧設定")]
    [SerializeField] private float fogOfWarSize = 50f; // 迷霧覆蓋範圍
    [SerializeField] private Color fogColor = Color.black; // 迷霧顏色
    [SerializeField] private Color visibleColor = Color.white; // 可見區域顏色

    [Header("除錯")]
    [SerializeField] private bool showDebugRays = false;

    private MeshFilter meshFilter;
    private Mesh viewMesh;
    private MeshRenderer meshRenderer;
    private Material fogMaterial;

    // 優化：重用列表和陣列，避免GC
    private List<Vector3> viewPoints;
    private List<Vector3> worldViewPoints;
    private List<Color> vertexColors; // 頂點顏色
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        viewMesh = new Mesh();
        viewMesh.name = "Fog of War Mesh";
        meshFilter.mesh = viewMesh;

        // 初始化重用的列表
        viewPoints = new List<Vector3>();
        worldViewPoints = new List<Vector3>();
        vertexColors = new List<Color>();

        // 設定材質
        SetupFogMaterial();
    }

    private void SetupFogMaterial()
    {
        // 如果沒有材質，創建一個支援頂點色彩的材質
        if (meshRenderer.material == null)
        {
            fogMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            fogMaterial = meshRenderer.material;
        }

        // 確保材質支援透明度
        fogMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fogMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fogMaterial.SetInt("_ZWrite", 0);
        fogMaterial.DisableKeyword("_ALPHATEST_ON");
        fogMaterial.EnableKeyword("_ALPHABLEND_ON");
        fogMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        fogMaterial.renderQueue = 3000;

        meshRenderer.material = fogMaterial;
    }

    private void LateUpdate()
    {
        if (playerManager == null || viewAngle <= 0f)
        {
            if (playerManager == null)
                Debug.LogWarning("PlayerFOV: PlayerManager 未設定！");
            return;
        }

        DrawFogOfWar();
    }

    private void DrawFogOfWar()
    {
        Vector3 origin = playerManager.Position;

        // 清空所有列表
        viewPoints.Clear();
        worldViewPoints.Clear();
        vertexColors.Clear();

        // 第一步：創建大範圍的黑色迷霧四邊形
        CreateFogQuad(origin);

        // 第二步：添加可見範圍的白色區域
        AddVisibleArea(origin);

        // 轉換為本地座標並生成網格
        ConvertToLocalCoordinates(origin);
        GenerateMeshWithColors();
    }

    private void CreateFogQuad(Vector3 center)
    {
        // 創建一個大的四邊形覆蓋整個區域
        float halfSize = fogOfWarSize * 0.5f;

        // 四個角落的點（世界座標）
        worldViewPoints.Add(center + new Vector3(-halfSize, -halfSize, 0)); // 左下
        worldViewPoints.Add(center + new Vector3(halfSize, -halfSize, 0));  // 右下
        worldViewPoints.Add(center + new Vector3(halfSize, halfSize, 0));   // 右上
        worldViewPoints.Add(center + new Vector3(-halfSize, halfSize, 0));  // 左上

        // 對應的顏色（黑色迷霧）
        for (int i = 0; i < 4; i++)
        {
            vertexColors.Add(fogColor);
        }
    }

    private void AddVisibleArea(Vector3 origin)
    {
        float playerZRotation = playerManager.EulerAngles.z;
        int actualRayCount = Mathf.Max(3, rayCount);
        float angleStep = viewAngle / actualRayCount;
        float startAngle = playerZRotation - viewAngle / 2f;

        // 添加視野中心點
        worldViewPoints.Add(origin);
        vertexColors.Add(visibleColor); // 可見區域為白色

        // 生成視野邊緣點
        for (int i = 0; i <= actualRayCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector3 direction = DirFromAngle(currentAngle, true);

            Vector3 rayEndPoint = GetRaycastEndPoint(origin, direction, viewRadius);
            worldViewPoints.Add(rayEndPoint);
            vertexColors.Add(visibleColor); // 可見區域為白色

            // 除錯射線
            if (showDebugRays)
            {
                Debug.DrawRay(origin, direction * viewRadius, Color.red, 0.1f);
                Debug.DrawLine(origin, rayEndPoint, Color.green, 0.1f);
            }
        }
    }

    private Vector3 GetRaycastEndPoint(Vector3 origin, Vector3 direction, float maxDistance)
    {
        Vector3 rayOrigin = origin + direction * raycastOffset;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, maxDistance - raycastOffset, wallLayer);

        if (hit.collider != null)
        {
            Vector3 hitPoint = (Vector3)hit.point - direction * raycastOffset;
            return hitPoint;
        }
        else
        {
            return origin + direction * maxDistance;
        }
    }

    private void ConvertToLocalCoordinates(Vector3 origin)
    {
        viewPoints.Clear();

        foreach (Vector3 worldPoint in worldViewPoints)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            viewPoints.Add(localPoint);
        }
    }

    private void GenerateMeshWithColors()
    {
        int vertexCount = viewPoints.Count;

        if (vertexCount < 3)
        {
            Debug.LogWarning("PlayerFOV: 頂點數量不足以生成網格！");
            return;
        }

        // 重新分配陣列
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];

        // 設定頂點和顏色
        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = viewPoints[i];
            colors[i] = i < vertexColors.Count ? vertexColors[i] : fogColor;
        }

        // 生成三角形索引
        List<int> triangleList = new List<int>();

        // 迷霧四邊形的三角形 (前4個頂點)
        triangleList.AddRange(new int[] { 0, 1, 2 }); // 第一個三角形
        triangleList.AddRange(new int[] { 0, 2, 3 }); // 第二個三角形

        // 視野扇形的三角形 (從第5個頂點開始，索引4是中心點)
        int centerIndex = 4; // 視野中心點的索引
        int visibleStartIndex = 5; // 視野邊緣點開始的索引
        int visibleVertexCount = vertexCount - visibleStartIndex;

        for (int i = 0; i < visibleVertexCount - 1; i++)
        {
            triangleList.AddRange(new int[] {
                centerIndex,
                visibleStartIndex + i,
                visibleStartIndex + i + 1
            });
        }

        triangles = triangleList.ToArray();

        // 更新網格
        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.colors = colors;
        viewMesh.RecalculateNormals();
        viewMesh.RecalculateBounds();
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal && playerManager != null && playerManager.playerTransform != null)
        {
            angleInDegrees += playerManager.playerTransform.eulerAngles.z;
        }

        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }

    // 公開方法
    public void UpdateFOV()
    {
        if (playerManager != null && viewAngle > 0f)
        {
            DrawFogOfWar();
        }
    }

    public void SetViewParameters(float radius, float angle, int rays)
    {
        viewRadius = Mathf.Max(0.1f, radius);
        viewAngle = Mathf.Clamp(angle, 0f, 360f);
        rayCount = Mathf.Max(3, rays);
    }

    public void SetFogParameters(float size, Color fog, Color visible)
    {
        fogOfWarSize = Mathf.Max(1f, size);
        fogColor = fog;
        visibleColor = visible;
    }

    private void OnValidate()
    {
        viewRadius = Mathf.Max(0.1f, viewRadius);
        viewAngle = Mathf.Clamp(viewAngle, 0f, 360f);
        rayCount = Mathf.Max(3, rayCount);
        fogOfWarSize = Mathf.Max(1f, fogOfWarSize);
    }
}