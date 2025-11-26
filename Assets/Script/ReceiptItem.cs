using UnityEngine;

public class ReceiptItem : MonoBehaviour
{
    public Vector2 halfSizeXZ = new Vector2(0.05f, 0.08f);
    public float surfaceOffset = 0.001f;
    public Rigidbody rb;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }
}
