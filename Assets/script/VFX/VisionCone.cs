using UnityEngine;
using System.Collections.Generic;

public class VisionCone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private LayerMask wallsLayer = -1;
    [SerializeField] protected LayerMask objectsLayer = -1;

    [Header("Spotlight Settings")]
    public float circleRadius = 5f; 
    public float coneAngle = 45f;
    public float coneRange = 10f;
    public float featherDistance = 1f;
    public float featherAngle = 5f;
    public float fadeSpeed = 2f;

    private Dictionary<Renderer, float> currentAlphas = new Dictionary<Renderer, float>();

    private List<Material> originalMaterials = new List<Material>();
    private List<Renderer> renderers = new List<Renderer>();


    private void MakeMaterialTransparent(Material mat)
    {
        if (!mat.HasProperty("_Color"))
            return;

        mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent (URP/Standard Shader)
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
        Invoke(nameof(InitializeRenderers), 0.1f);
    }

    void InitializeRenderers()
    {
        if (player == null)
            player = FindObjectOfType<Player>();

        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Target[] targets = FindObjectsOfType<Target>();

        Debug.Log($"Enemies found: {enemies.Length}, Targets found: {targets.Length}");

        foreach (var enemy in enemies)
        {   
            Renderer[] r1 = enemy.GetComponentsInChildren<Renderer>();
            renderers.AddRange(r1);
            foreach (var r in r1)
                originalMaterials.AddRange(r.materials);
        }

        foreach (var target in targets)
        {
            Renderer[] r1 = target.GetComponentsInChildren<Renderer>();
            renderers.AddRange(r1);
            foreach (var r in r1)
                originalMaterials.AddRange(r.materials);
        }

        foreach (var mat in originalMaterials)
            MakeMaterialTransparent(mat);
        
        foreach (var rend in renderers)
            currentAlphas[rend] = 0f;
    }
    private LayerMask GetObstacleLayerMask()
    {
        LayerMask baseMask = wallsLayer; // 獲取 walls layer        
        // 如果玩家正在蹲下，添加 objects layer
        if (player != null && player.IsSquatting)
        {
            return baseMask | objectsLayer;
        }
        
        // 玩家站立時，只使用 walls layer
        return baseMask;
    }

    void Update()
    {
        Vector2 playerPos = player.transform.position;
        Vector2 aimDir = player.GetWeaponDirection().normalized;

        int idx = 0;
        foreach (Renderer rend in renderers)
        {
            if (rend == null)
            {
                idx++;
                continue;
            }
            Vector2 toEnemy = (Vector2)rend.transform.position - playerPos;
            float distance = toEnemy.magnitude;


            float angleToEnemy = Vector2.Angle(aimDir, toEnemy.normalized);

            float alpha;

            // feather using cone angle
            if (angleToEnemy <= coneAngle / 2f + featherAngle && distance > circleRadius)
            {
                alpha = 1f - Mathf.Clamp(angleToEnemy - (coneAngle / 2f), 0, featherAngle) / featherAngle;
                if (distance > coneRange)
                    alpha = 1f - Mathf.Clamp(distance - coneRange, 0, featherDistance) / featherDistance;
            }
            else // Feather using circle distance
            {   
                alpha = 1f - Mathf.Clamp(distance - circleRadius, 0, featherDistance) / featherDistance;
            }
            
            if (wallsLayer != -1 && objectsLayer != -1)
            {
                RaycastHit2D hit = Physics2D.Raycast(
                    playerPos,
                    toEnemy.normalized,
                    distance,
                    GetObstacleLayerMask()
                );
                if (hit.collider != null && hit.collider.gameObject != rend.gameObject)
                {
                    alpha = 0f;
                }
            }
            else
            {
                Debug.LogWarning("WallsLayer or ObjectsLayer is not set properly in VisionCone.");
            }
            
            float currentAlpha = currentAlphas[rend];
            currentAlpha = Mathf.MoveTowards(currentAlpha, alpha, Time.deltaTime * fadeSpeed);
            currentAlphas[rend] = currentAlpha;
            
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = currentAlpha;
                    mat.color = c;
                }
            }

            idx++;
        }
    }
}
