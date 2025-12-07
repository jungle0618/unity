using UnityEngine;
using System.Collections;

public class BulletTracerLine : MonoBehaviour
{
    public float stretchLength = 5f;
    public float zOffset = 1f;
    private LineRenderer lr;


    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
    }

    void LateUpdate()
    {
        Vector3 head = transform.position; 
        Vector3 tail = head - transform.up * stretchLength;
        head.z += zOffset;
        tail.z += zOffset;

        lr.SetPosition(0, tail);
        lr.SetPosition(1, head);
    }
}
