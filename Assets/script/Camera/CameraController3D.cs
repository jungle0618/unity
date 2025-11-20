using UnityEngine;

public class CameraController3D : MonoBehaviour
{
    public Camera orthoCamera;
    private Camera perspectiveCamera;
    private float distance;
    void Start()
    {
        perspectiveCamera = GetComponent<Camera>();
        float orthoHalfHeight = orthoCamera.orthographicSize;
        float fov = perspectiveCamera.fieldOfView;
        distance = orthoHalfHeight / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

        Vector3 target = new Vector3(
            orthoCamera.transform.position.x,
            orthoCamera.transform.position.y,
            0f);
        perspectiveCamera.transform.position = target - perspectiveCamera.transform.forward * distance;
        perspectiveCamera.transform.LookAt(target);
    }

    void LateUpdate()
    {
        Vector3 target = new Vector3(
            orthoCamera.transform.position.x,
            orthoCamera.transform.position.y,
            0f);
        transform.position = target - transform.forward * distance;

        transform.LookAt(target);
    }
}
