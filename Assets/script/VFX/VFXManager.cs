using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Death VFX Settings")]
    public float fadeDelay = 0.2f;
    public float fadeDuration = 0.6f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // optional if you want it persistent
    }


    private void MakeMaterialTransparent(Material mat)
    {
        if (!mat.HasProperty("_Color"))
            return;

        // Only switch once
        mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent (URP/Standard Shader)
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    public void PlayDeathVFXHandler<TState>(BaseEntity<TState> entity) where TState : System.Enum
    {
        StartCoroutine(PlayDeathVFX(entity));
    }

    public void PlayerPlayDeathVFXHandler()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            StartCoroutine(PlayDeathVFX(player));
        }
    }

    private IEnumerator PlayDeathVFX<TState>(BaseEntity<TState> entity) where TState : System.Enum
    {
        
        GameObject clone = Instantiate(
            entity.gameObject,
            entity.transform.position,
            entity.transform.rotation
        );


        Animator animator = clone.GetComponentInChildren<Animator>();
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        animator.SetTrigger("Die");

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found for Death VFX.");
        }
        if (animator == null)
        {
            Debug.LogWarning("No animator found for Death VFX.");
        }

        yield return new WaitForSeconds(fadeDelay);

        float t = 0;

        SkinnedMeshRenderer[] skinnedRenderers = clone.GetComponentsInChildren<SkinnedMeshRenderer>();
        MeshRenderer[] meshRenderers = clone.GetComponentsInChildren<MeshRenderer>();

        List<Material> materials = new List<Material>();

        foreach (var r in skinnedRenderers)
            materials.AddRange(r.materials);
        foreach (var r in meshRenderers)
            materials.AddRange(r.materials);

       foreach (var mat in materials)
        {
            MakeMaterialTransparent(mat);
        } 

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);

            foreach (var mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }

            yield return null;
        }

        Destroy(clone);
    }
}