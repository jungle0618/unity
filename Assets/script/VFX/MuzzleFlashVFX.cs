using UnityEngine;
using System.Collections;


public class MuzzleFlashVFX : MonoBehaviour
{
    public Material[] frames;
    public float fps = 16f;
    [SerializeField] private MeshRenderer[] meshRenderers;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
    }

    private IEnumerator PlayMuzzleFlashVFX()
    {
        if (frames == null || frames.Length == 0)
            yield break;

        Light light = GetComponentInChildren<Light>();
        light.enabled = true;

        float frameTime = 1f / fps;

        for (int i = 0; i < frames.Length; i++)
        {
            if (i == 2) light.enabled = false;
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.sharedMaterial = frames[i];
            }
            yield return new WaitForSeconds(frameTime);
        }

        Destroy(gameObject);
    }
}
