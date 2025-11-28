using UnityEngine;

public class PlayerFallDamageCarrier : MonoBehaviour
{
    [Header("Ground Check")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer = ~0;   // หรือกำหนดเฉพาะชั้นพื้น

    [Header("Fall Damage Settings")]
    [Tooltip("ความสูงที่ตก (เมตร) ขั้นต่ำก่อนเริ่มคิดดาเมจ")]
    public float minFallHeightForDamage = 2.5f;

    float fallStartY;
    bool wasGrounded = true;

    void Update()
    {
        bool grounded = IsGrounded();

        // เริ่มตก: จาก grounded → not grounded
        if (!grounded && wasGrounded)
        {
            fallStartY = transform.position.y;
            // Debug.Log($"[PlayerFall] Start fall from Y={fallStartY:F2}");
        }

        // ลงพื้น: จาก not grounded → grounded
        if (grounded && !wasGrounded)
        {
            float drop = fallStartY - transform.position.y;
            if (drop > minFallHeightForDamage)
            {
                Debug.Log($"[PlayerFall] Landed, drop={drop:F2}m → apply inventory damage");

                if (BoxInventory.Instance != null)
                    BoxInventory.Instance.ApplyFallDamageToAll(drop);
            }
        }

        wasGrounded = grounded;
    }

    bool IsGrounded()
    {
        // เช็คจาก Raycast ลงข้างล่าง
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        bool hit = Physics.Raycast(origin, Vector3.down, out var hitInfo,
            groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);

        // Debug.DrawRay(origin, Vector3.down * groundCheckDistance, hit ? Color.green : Color.red);
        return hit;
    }
}
