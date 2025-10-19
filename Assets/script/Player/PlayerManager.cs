using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] public Transform playerTransform;

    public Vector2 Position
    {
        get { return playerTransform.position; }
    }

    public Vector3 EulerAngles
    {
        get { return playerTransform.eulerAngles; } // 旋轉角度
    }
}