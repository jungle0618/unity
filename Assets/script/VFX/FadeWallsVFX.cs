using System.Collections.Generic;
using UnityEngine;

public class FadeWallsVFX : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera cam;

    [Header("Raycast")]
    public LayerMask wallsMask;       // Set to your Walls layer
    public float sphereCastRadius = 0.3f;

    [Header("Fade Settings")]
    [Range(0f, 1f)] public float targetAlpha = 0.25f;
    public float fadeSpeed = 6f;
    public float playerDistOffset = 2f;
    public float boxSize = 1f;

    // Tracks currently faded renderers and their original materials
    private readonly Dictionary<Renderer, Material[]> originalMats = new();
    private readonly HashSet<Renderer> currentlyObstructing = new();

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (!player || !cam) return;

        // Find all obstacles between camera and player
        Vector3 origin = cam.transform.position;
        Vector3 dir = (player.position - origin).normalized;
        float dist = Vector3.Distance(origin, player.position);
        Vector3 halfExtents = new Vector3(boxSize, boxSize, boxSize); 

        var hits = Physics.BoxCastAll(
            origin + dir,
            halfExtents,
            dir,
            cam.transform.rotation,
            dist - playerDistOffset,
            wallsMask,
            QueryTriggerInteraction.Collide
        );

        

        currentlyObstructing.Clear();

        foreach (var hit in hits)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (!rend) continue;
            currentlyObstructing.Add(rend);
            FadeRenderer(rend, targetAlpha);
        }

        // Restore any renderers no longer obstructing
        var toRestore = new List<Renderer>();
        foreach (var kvp in originalMats)
        {
            if (!currentlyObstructing.Contains(kvp.Key))
                toRestore.Add(kvp.Key);
        }
        foreach (var rend in toRestore)
            RestoreRenderer(rend);

        // Smoothly update alpha
        foreach (var rend in currentlyObstructing)
            LerpAlpha(rend, targetAlpha);
        foreach (var rend in toRestore)
            LerpAlpha(rend, 1f);
    }

    void FadeRenderer(Renderer rend, float alpha)
    {
        if (!originalMats.ContainsKey(rend))
        {
            originalMats[rend] = rend.materials;
            // Duplicate materials so we don’t mutate shared assets
            var mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(mats[i]);
                SetMaterialFade(mats[i]);
                SetMaterialAlpha(mats[i], alpha);
            }
            rend.materials = mats;
        }
    }

    void RestoreRenderer(Renderer rend)
    {
        if (!originalMats.TryGetValue(rend, out var orig)) return;
        // Lerp back to opaque then restore originals
        var mats = rend.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            SetMaterialOpaque(mats[i]);
            SetMaterialAlpha(mats[i], 1f);
        }
        rend.materials = orig;
        originalMats.Remove(rend);
    }

    void LerpAlpha(Renderer rend, float target)
    {
        var mats = rend.materials;
        foreach (var m in mats)
        {
            var col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                     : m.HasProperty("_Color") ? m.GetColor("_Color")
                     : Color.white;
            float a = Mathf.Lerp(col.a, target, Time.deltaTime * fadeSpeed);
            col.a = a;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        }
    }

    // Switch to Fade mode (URP/Standard)
    void SetMaterialFade(Material m)
    {
        // URP Lit: Surface Type = Transparent
        if (m.shader.name.Contains("Universal Render Pipeline"))
        {
            m.SetFloat("_Surface", 1f);  // 0 Opaque, 1 Transparent
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            // Standard shader fallback
            m.SetFloat("_Mode", 2); // Fade
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    void SetMaterialOpaque(Material m)
    {
        if (m.shader.name.Contains("Universal Render Pipeline"))
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            m.SetFloat("_Mode", 0); // Opaque
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = -1;
        }
    }

    void SetMaterialAlpha(Material m, float a)
    {
        if (m.HasProperty("_BaseColor"))
        {
            var c = m.GetColor("_BaseColor"); c.a = a; m.SetColor("_BaseColor", c);
        }
        else if (m.HasProperty("_Color"))
        {
            var c = m.GetColor("_Color"); c.a = a; m.SetColor("_Color", c);
        }
    }
}