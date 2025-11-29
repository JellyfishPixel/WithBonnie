using UnityEngine;

/// <summary>
/// สคริปต์เช็คว่า Player ตกจากที่สูงแค่ไหน แล้วแจ้ง BoxInventory ให้ลดคุณภาพของในตัว
/// แนะนำให้ติดบนตัว Player (ตัวเดียวกับ Controller)
/// </summary>
public class PlayerFallDamageCarrier : MonoBehaviour
{
    [Header("Ground Check")]
    [Tooltip("ความสูงของ Raycast จากตัวผู้เล่นลงพื้น (ลอง 0.6f - 1.0f ก่อน)")]
    public float groundCheckDistance = 0.6f;

    [Tooltip("Layer ที่ถือว่าเป็นพื้น (ถ้าไม่มั่นใจลองตั้งเป็น Everything/~0 ชั่วคราว)")]
    public LayerMask groundLayer = ~0;

    [Header("Minimum fall height for any damage (เมตร)")]
    [Tooltip("ตกจากที่สูงน้อยกว่าค่านี้จะไม่เรียกใช้ดาเมจเลย")]
    public float minFallHeightForAnyDamage = 1.0f;

    float fallStartY;
    bool wasGrounded = true;

    void Update()
    {
        bool grounded = IsGrounded();

        // เริ่มตก: จาก grounded → not grounded
        if (!grounded && wasGrounded)
        {
            fallStartY = transform.position.y;
            Debug.Log($"[PlayerFall] Start falling from Y={fallStartY:F2}");
        }

        // ลงพื้น: จาก not grounded → grounded
        if (grounded && !wasGrounded)
        {
            float drop = fallStartY - transform.position.y;
            Debug.Log($"[PlayerFall] Landed at Y={transform.position.y:F2}, drop={drop:F2} m");

            if (drop > minFallHeightForAnyDamage)
            {
                if (BoxInventory.Instance != null)
                {
                    Debug.Log($"[PlayerFall] Apply fall damage to inventory with drop={drop:F2} m");
                    BoxInventory.Instance.ApplyFallDamageToAll(drop);
                }
                else
                {
                    Debug.LogWarning("[PlayerFall] BoxInventory.Instance = null");
                }
            }
        }

        wasGrounded = grounded;
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        bool hit = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hitInfo,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, hit ? Color.green : Color.red);

        return hit;
    }
}
