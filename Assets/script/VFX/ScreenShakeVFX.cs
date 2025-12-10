using UnityEngine;
using System.Collections;

public class ScreenShakeVFX : MonoBehaviour
{
    public static ScreenShakeVFX Instance;

    [SerializeField] private float intensity = 0.1f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private CameraController3D cameraController;

    private Vector3 originalLocalPos;
    private Coroutine shakeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cameraController == null)
        {
            cameraController = GetComponent<CameraController3D>();
        }

    }

    public void Shake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float timer = 0f;

        while (timer < duration)
        {
            originalLocalPos = transform.localPosition;

            Vector2 offset = Random.insideUnitCircle * intensity;
            Vector3 newPos = originalLocalPos + new Vector3(offset.x, offset.y, 0f);

            cameraController.OverwritePosition(newPos);

            timer += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
    }
}
