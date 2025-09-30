using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlayerFOV : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private PlayerManager playerManager;

    [Header("�����]�w")]
    [SerializeField] private float viewRadius = 5f;
    [SerializeField, Range(0, 360)] private float viewAngle = 90f;
    [SerializeField] private int rayCount = 120;

    [Header("��ê���]�w")]
    [SerializeField] private LayerMask wallLayer = -1;
    [SerializeField] private float raycastOffset = 0.01f;

    [Header("�g���]�w")]
    [SerializeField] private float fogOfWarSize = 50f; // �g���л\�d��
    [SerializeField] private Color fogColor = Color.black; // �g���C��
    [SerializeField] private Color visibleColor = Color.white; // �i���ϰ��C��

    [Header("����")]
    [SerializeField] private bool showDebugRays = false;

    private MeshFilter meshFilter;
    private Mesh viewMesh;
    private MeshRenderer meshRenderer;
    private Material fogMaterial;

    // �u�ơG���ΦC��M�}�C�A�קKGC
    private List<Vector3> viewPoints;
    private List<Vector3> worldViewPoints;
    private List<Color> vertexColors; // ���I�C��
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

        // ��l�ƭ��Ϊ��C��
        viewPoints = new List<Vector3>();
        worldViewPoints = new List<Vector3>();
        vertexColors = new List<Color>();

        // �]�w����
        SetupFogMaterial();
    }

    private void SetupFogMaterial()
    {
        // �p�G�S������A�Ыؤ@�Ӥ䴩���I��m������
        if (meshRenderer.material == null)
        {
            fogMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            fogMaterial = meshRenderer.material;
        }

        // �T�O����䴩�z����
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
                Debug.LogWarning("PlayerFOV: PlayerManager ���]�w�I");
            return;
        }

        DrawFogOfWar();
    }

    private void DrawFogOfWar()
    {
        Vector3 origin = playerManager.Position;

        // �M�ũҦ��C��
        viewPoints.Clear();
        worldViewPoints.Clear();
        vertexColors.Clear();

        // �Ĥ@�B�G�Ыؤj�d�򪺶¦�g���|���
        CreateFogQuad(origin);

        // �ĤG�B�G�K�[�i���d�򪺥զ�ϰ�
        AddVisibleArea(origin);

        // �ഫ�����a�y�Шåͦ�����
        ConvertToLocalCoordinates(origin);
        GenerateMeshWithColors();
    }

    private void CreateFogQuad(Vector3 center)
    {
        // �Ыؤ@�Ӥj���|����л\��Ӱϰ�
        float halfSize = fogOfWarSize * 0.5f;

        // �|�Ө������I�]�@�ɮy�С^
        worldViewPoints.Add(center + new Vector3(-halfSize, -halfSize, 0)); // ���U
        worldViewPoints.Add(center + new Vector3(halfSize, -halfSize, 0));  // �k�U
        worldViewPoints.Add(center + new Vector3(halfSize, halfSize, 0));   // �k�W
        worldViewPoints.Add(center + new Vector3(-halfSize, halfSize, 0));  // ���W

        // �������C��]�¦�g���^
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

        // �K�[���������I
        worldViewPoints.Add(origin);
        vertexColors.Add(visibleColor); // �i���ϰ쬰�զ�

        // �ͦ�������t�I
        for (int i = 0; i <= actualRayCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector3 direction = DirFromAngle(currentAngle, true);

            Vector3 rayEndPoint = GetRaycastEndPoint(origin, direction, viewRadius);
            worldViewPoints.Add(rayEndPoint);
            vertexColors.Add(visibleColor); // �i���ϰ쬰�զ�

            // �����g�u
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
            Debug.LogWarning("PlayerFOV: ���I�ƶq�����H�ͦ�����I");
            return;
        }

        // ���s���t�}�C
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];

        // �]�w���I�M�C��
        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = viewPoints[i];
            colors[i] = i < vertexColors.Count ? vertexColors[i] : fogColor;
        }

        // �ͦ��T���ί���
        List<int> triangleList = new List<int>();

        // �g���|��Ϊ��T���� (�e4�ӳ��I)
        triangleList.AddRange(new int[] { 0, 1, 2 }); // �Ĥ@�ӤT����
        triangleList.AddRange(new int[] { 0, 2, 3 }); // �ĤG�ӤT����

        // �������Ϊ��T���� (�q��5�ӳ��I�}�l�A����4�O�����I)
        int centerIndex = 4; // ���������I������
        int visibleStartIndex = 5; // ������t�I�}�l������
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

        // ��s����
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

    // ���}��k
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