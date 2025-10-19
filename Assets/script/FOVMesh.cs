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

    // 除錯優除錯化除錯：除錯重除錯用除錯列除錯表除錯和除錯陣除錯列除錯，除錯避除錯免除錯G除錯C除錯
    private List<Vector3> viewPoints;
    private List<Vector3> worldViewPoints;
    private List<Color> vertexColors; // 除錯頂除錯點除錯顏除錯色除錯
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

        // 除錯初除錯始除錯化除錯重除錯用除錯的除錯列除錯表除錯
        viewPoints = new List<Vector3>();
        worldViewPoints = new List<Vector3>();
        vertexColors = new List<Color>();

        // 除錯設除錯定除錯材除錯質除錯
        SetupFogMaterial();
    }

    private void SetupFogMaterial()
    {
        // 除錯如除錯果除錯沒除錯有除錯材除錯質除錯，除錯創除錯建除錯一除錯個除錯支除錯援除錯頂除錯點除錯色除錯彩除錯的除錯材除錯質除錯
        if (meshRenderer.material == null)
        {
            fogMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            fogMaterial = meshRenderer.material;
        }

        // 除錯確除錯保除錯材除錯質除錯支除錯援除錯透除錯明除錯度除錯
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

        // 除錯清除錯空除錯所除錯有除錯列除錯表除錯
        viewPoints.Clear();
        worldViewPoints.Clear();
        vertexColors.Clear();

        // 除錯第除錯一除錯步除錯：除錯創除錯建除錯大除錯範除錯圍除錯的除錯黑除錯色除錯迷除錯霧除錯四除錯邊除錯形除錯
        CreateFogQuad(origin);

        // 除錯第除錯二除錯步除錯：除錯添除錯加除錯可除錯見除錯範除錯圍除錯的除錯白除錯色除錯區除錯域除錯
        AddVisibleArea(origin);

        // 除錯轉除錯換除錯為除錯本除錯地除錯座除錯標除錯並除錯生除錯成除錯網除錯格除錯
        ConvertToLocalCoordinates(origin);
        GenerateMeshWithColors();
    }

    private void CreateFogQuad(Vector3 center)
    {
        // 除錯創除錯建除錯一除錯個除錯大除錯的除錯四除錯邊除錯形除錯覆除錯蓋除錯整除錯個除錯區除錯域除錯
        float halfSize = fogOfWarSize * 0.5f;

        // 除錯四除錯個除錯角除錯落除錯的除錯點除錯（除錯世除錯界除錯座除錯標除錯）除錯
        worldViewPoints.Add(center + new Vector3(-halfSize, -halfSize, 0)); // 除錯左除錯下除錯
        worldViewPoints.Add(center + new Vector3(halfSize, -halfSize, 0));  // 除錯右除錯下除錯
        worldViewPoints.Add(center + new Vector3(halfSize, halfSize, 0));   // 除錯右除錯上除錯
        worldViewPoints.Add(center + new Vector3(-halfSize, halfSize, 0));  // 除錯左除錯上除錯

        // 除錯對除錯應除錯的除錯顏除錯色除錯（除錯黑除錯色除錯迷除錯霧除錯）除錯
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

        // 除錯添除錯加除錯視除錯野除錯中除錯心除錯點除錯
        worldViewPoints.Add(origin);
        vertexColors.Add(visibleColor); // 除錯可除錯見除錯區除錯域除錯為除錯白除錯色除錯

        // 除錯生除錯成除錯視除錯野除錯邊除錯緣除錯點除錯
        for (int i = 0; i <= actualRayCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector3 direction = DirFromAngle(currentAngle, true);

            Vector3 rayEndPoint = GetRaycastEndPoint(origin, direction, viewRadius);
            worldViewPoints.Add(rayEndPoint);
            vertexColors.Add(visibleColor); // 除錯可除錯見除錯區除錯域除錯為除錯白除錯色除錯

            // 除錯射除錯線除錯
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

        // 除錯重除錯新除錯分除錯配除錯陣除錯列除錯
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];

        // 除錯設除錯定除錯頂除錯點除錯和除錯顏除錯色除錯
        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = viewPoints[i];
            colors[i] = i < vertexColors.Count ? vertexColors[i] : fogColor;
        }

        // 除錯生除錯成除錯三除錯角除錯形除錯索除錯引除錯
        List<int> triangleList = new List<int>();

        // 除錯迷除錯霧除錯四除錯邊除錯形除錯的除錯三除錯角除錯形除錯 除錯(除錯前除錯4除錯個除錯頂除錯點除錯)除錯
        triangleList.AddRange(new int[] { 0, 1, 2 }); // 除錯第除錯一除錯個除錯三除錯角除錯形除錯
        triangleList.AddRange(new int[] { 0, 2, 3 }); // 除錯第除錯二除錯個除錯三除錯角除錯形除錯

        // 除錯視除錯野除錯扇除錯形除錯的除錯三除錯角除錯形除錯 除錯(除錯從除錯第除錯5除錯個除錯頂除錯點除錯開除錯始除錯，除錯索除錯引除錯4除錯是除錯中除錯心除錯點除錯)除錯
        int centerIndex = 4; // 除錯視除錯野除錯中除錯心除錯點除錯的除錯索除錯引除錯
        int visibleStartIndex = 5; // 除錯視除錯野除錯邊除錯緣除錯點除錯開除錯始除錯的除錯索除錯引除錯
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

        // 除錯更除錯新除錯網除錯格除錯
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

    // 除錯公除錯開除錯方除錯法除錯
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