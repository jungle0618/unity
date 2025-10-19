using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    [Header("固定縮放 (相機大小固定)")]
    public float fixedOrthographicSize = 5f; // 相機固定的 orthographicSize（腳本不會改變）

    [Header("水平追蹤 (由 player 決定)")]
    public Transform player;                 // 指向 player 的 transform（在 Inspector 指定）
    [Tooltip("水平追蹤平滑速度，值越大追蹤越快（實際是在 Lerp 上乘 Time.deltaTime）。")]
    public float horizontalLerpSpeed = 10f;

    [Header("滾輪垂直移動")]
    public float scrollMoveSpeed = 5f;       // 滾輪控制上下移動速度（world units per wheel-step）
    public bool invertScroll = false;        // 反向滾輪方向選項

    [Header("移動邊界限制（可選）")]
    public bool useBounds = false;           // 是否使用邊界限制
    public Vector2 minBounds = new Vector2(-10, -10);
    public Vector2 maxBounds = new Vector2(10, 10);

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("CameraController2D 必須掛在一個 Camera 上。");
            enabled = false;
            return;
        }

        if (cam.orthographic == false)
        {
            Debug.LogWarning("這個腳本是為正交相機（Orthographic）設計的，建議切換到 2D 模式。");
        }

        // 將 orthographicSize 固定
        cam.orthographicSize = fixedOrthographicSize;
    }

    void Update()
    {
        // 水平由 player 決定（平滑）
        FollowPlayerHorizontal();

        // 滾輪控制上下
        HandleVerticalScroll();
    }

    void FollowPlayerHorizontal()
    {
        if (player == null) return;

        float targetX = player.position.x;
        // 平滑追蹤（simple Lerp）
        float t = Mathf.Clamp01(horizontalLerpSpeed * Time.deltaTime);
        float newX = Mathf.Lerp(transform.position.x, targetX, t);

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (useBounds)
            ClampCameraToBounds();
    }

    void HandleVerticalScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        float dir = invertScroll ? -scroll : scroll;
        float newY = transform.position.y + dir * scrollMoveSpeed;

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        if (useBounds)
            ClampCameraToBounds();
    }

    // 依相機 viewport 大小與邊界做 clamp（避免相機移出邊界）
    void ClampCameraToBounds()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.aspect * halfHeight;

        float minX = minBounds.x + halfWidth;
        float maxX = maxBounds.x - halfWidth;
        float minY = minBounds.y + halfHeight;
        float maxY = maxBounds.y - halfHeight;

        Vector3 pos = transform.position;

        // 若邊界太小（比相機視口還小），則把相機固定在邊界中心
        if (minX > maxX)
            pos.x = (minBounds.x + maxBounds.x) * 0.5f;
        else
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY > maxY)
            pos.y = (minBounds.y + maxBounds.y) * 0.5f;
        else
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    // 重置相機到初始位置和固定大小
    public void ResetCamera()
    {
        float z = transform.position.z;
        float x = (player != null) ? player.position.x : 0f;
        transform.position = new Vector3(x, 0f, z);
        cam.orthographicSize = fixedOrthographicSize;
    }
}