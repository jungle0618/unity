using UnityEngine;
using System.Collections.Generic;

public class VisionCone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;

    [Header("Spotlight Settings")]
    public float circleRadius = 5f; 
    public float coneAngle = 45f;
    public float coneRange = 10f;
    public float featherDistance = 1f;
    public float featherAngle = 5f;

    private List<Material> originalMaterials = new List<Material>();
    private List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();


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
            SkinnedMeshRenderer[] skinnedRenderers = enemy.GetComponentsInChildren<SkinnedMeshRenderer>();
            renderers.AddRange(skinnedRenderers);
            foreach (var r in skinnedRenderers)
                originalMaterials.AddRange(r.materials);
            Debug.Log($"Renderer count: {renderers.Count}");
        }

        foreach (var target in targets)
        {
            SkinnedMeshRenderer[] skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
            renderers.AddRange(skinnedRenderers);
            foreach (var r in skinnedRenderers)
                originalMaterials.AddRange(r.materials);
        }

        foreach (var mat in originalMaterials)
            MakeMaterialTransparent(mat);

    }


    void Update()
    {
        Vector2 playerPos = player.transform.position;
        Vector2 aimDir = player.GetWeaponDirection().normalized;

        int idx = 0;
        foreach (Renderer rend in renderers)
        {
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
            
            float circleFade = Mathf.Clamp01(1f - distance / circleRadius);


            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }

            idx++;
        }
    }
}
