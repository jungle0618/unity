using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    [Header("�T�w�Y�� (�۾��j�p�T�w)")]
    public float fixedOrthographicSize = 5f; // �۾��T�w�� orthographicSize�]�}�����|���ܡ^

    [Header("�����l�� (�� player �M�w)")]
    public Transform player;                 // ���V player �� transform�]�b Inspector ���w�^
    [Tooltip("�����l�ܥ��Ƴt�סA�ȶV�j�l�ܶV�֡]��ڬO�b Lerp �W�� Time.deltaTime�^�C")]
    public float horizontalLerpSpeed = 10f;

    [Header("�u����������")]
    public float scrollMoveSpeed = 5f;       // �u������W�U���ʳt�ס]world units per wheel-step�^
    public bool invertScroll = false;        // �ϦV�u����V�ﶵ

    [Header("������ɭ���]�i��^")]
    public bool useBounds = false;           // �O�_�ϥ���ɭ���
    public Vector2 minBounds = new Vector2(-10, -10);
    public Vector2 maxBounds = new Vector2(10, 10);

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("CameraController2D �������b�@�� Camera �W�C");
            enabled = false;
            return;
        }

        if (cam.orthographic == false)
        {
            Debug.LogWarning("�o�Ӹ}���O������۾��]Orthographic�^�]�p���A��ĳ������ 2D �Ҧ��C");
        }

        // �N orthographicSize �T�w
        cam.orthographicSize = fixedOrthographicSize;
    }

    void Update()
    {
        // ������ player �M�w�]���ơ^
        FollowPlayerHorizontal();

        // �u������W�U
        HandleVerticalScroll();
    }

    void FollowPlayerHorizontal()
    {
        if (player == null) return;

        float targetX = player.position.x;
        // ���ưl�ܡ]simple Lerp�^
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

    // �̬۾� viewport �j�p�P��ɰ� clamp�]�קK�۾����X��ɡ^
    void ClampCameraToBounds()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.aspect * halfHeight;

        float minX = minBounds.x + halfWidth;
        float maxX = maxBounds.x - halfWidth;
        float minY = minBounds.y + halfHeight;
        float maxY = maxBounds.y - halfHeight;

        Vector3 pos = transform.position;

        // �Y��ɤӤp�]��۾����f�٤p�^�A�h��۾��T�w�b��ɤ���
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

    // ���m�۾����l��m�M�T�w�j�p
    public void ResetCamera()
    {
        float z = transform.position.z;
        float x = (player != null) ? player.position.x : 0f;
        transform.position = new Vector3(x, 0f, z);
        cam.orthographicSize = fixedOrthographicSize;
    }
}

