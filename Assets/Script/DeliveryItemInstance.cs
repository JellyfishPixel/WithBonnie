using UnityEngine;

/// <summary>
/// ตัวแทนไอเท็ม 1 ชิ้นในโลก 3D
/// - ผูกกับ DeliveryItemData (ScriptableObject)
/// - เก็บคุณภาพปัจจุบัน
/// - คำนวนดาเมจจากการชน / การตก / จาก container (กล่อง / ตัวผู้เล่น)
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DeliveryItemInstance : MonoBehaviour
{
    [Header("Data อ้างอิง")]
    [Tooltip("ScriptableObject ข้อมููลพื้นฐานของไอเท็มชิ้นนี้")]
    public DeliveryItemData data;

    [Header("สถานะ Runtime (อ่านได้/แก้ได้จากโค้ด)")]
    [Tooltip("คุณภาพปัจจุบันของไอเท็ม (0-100)")]
    [Range(0, 100)]
    public float currentQuality = 100f;

    [Tooltip("ของเสียหาย (แต่ยังไม่ถึงขั้นพัง) ถ้าคุณภาพต่ำกว่าค่า damagedThreshold ใน Data")]
    public bool isDamaged;

    [Tooltip("ของแตก/พัง ใช้งานไม่ได้ ถ้าคุณภาพต่ำกว่าค่า brokenThreshold ใน Data")]
    public bool isBroken;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // ถ้ามี Data ให้เซ็ตคุณภาพเริ่มต้นจาก baseQuality
        if (data != null)
            currentQuality = data.baseQuality;
    }

    // -------------------------------------------------------
    // ดาเมจจากการชนโดยตรง (ของชิ้นนี้ชนกับพื้น/กำแพงเอง)
    // -------------------------------------------------------
    void OnCollisionEnter(Collision collision)
    {
        if (data == null || isBroken) return;

        // ถ้าโดนน้ำและไอเท็มนี้พังเมื่อโดนน้ำ
        if (collision.collider.CompareTag("Water") && data.breaksOnWater)
        {
            ApplyDamage(999f); // ให้ดาเมจเยอะ ๆ เพื่อให้คุณภาพเหลือ 0
            Debug.Log($"[ItemInstance] {data.itemName} พังเพราะโดนน้ำ");
            return;
        }

        // ใช้ความเร็วการชนของ Rigidbody ในการคำนวนดาเมจ
        float impact = collision.relativeVelocity.magnitude;

        // ถ้าชนเบากว่า safeImpactVelocity ให้ถือว่าไม่เสียหาย
        if (impact <= data.safeImpactVelocity)
            return;

        float over = impact - data.safeImpactVelocity;
        float damage = over * data.damagePerVelocity;

        if (damage > 0f)
        {
            ApplyDamage(damage);
            Debug.Log($"[ItemInstance] {data.itemName} ชนแรง impact={impact:F2}, damage={damage:F1}, quality={currentQuality:F1}");
        }
    }

    // -------------------------------------------------------
    // ให้ Container (เช่น กล่อง / อินเวนทอรีผู้เล่น) ส่งดาเมจจากแรงชน/ตกมาให้
    // -------------------------------------------------------
    /// <summary>
    /// ใช้เวลาของอยู่ "ข้างใน" อะไรสักอย่าง (เช่น กล่อง/ตัวผู้เล่น)
    /// แล้ว container นั้นเป็นคนคำนวนแรงชน/ความสูงตก แทนไอเท็มเอง
    /// protectionMultiplier:
    ///   - 1   = โดนดาเมจเต็ม
    ///   - 0.3 = โดนดาเมจ 30% (กล่องช่วยรับแรงไป 70%)
    /// </summary>
    public void ApplyImpactFromContainer(float impactVelocity, float protectionMultiplier)
    {
        if (data == null || isBroken) return;

        if (impactVelocity <= data.safeImpactVelocity)
            return;

        float over = impactVelocity - data.safeImpactVelocity;
        float damage = over * data.damagePerVelocity * Mathf.Max(0f, protectionMultiplier);

        if (damage > 0f)
        {
            ApplyDamage(damage);
            Debug.Log($"[ItemInstance] {data.itemName} โดนดาเมจจาก Container impact={impactVelocity:F2}, damage={damage:F1}, quality={currentQuality:F1}");
        }
    }

    /// <summary>
    /// ใช้เวลาคำนวนจาก "ความสูงที่ตก" โดยตรง (เช่น ตอนอยู่ใน inventory ผู้เล่น)
    /// จะไปคำนวณความเร็วโดยประมาณเองจากความสูง แล้วส่งเข้าระบบดาเมจ
    /// </summary>
    public void ApplyFallHeight(float fallHeight, float protectionMultiplier)
    {
        if (data == null || isBroken) return;

        // ไม่ถึงความสูงขั้นต่ำที่ตั้งไว้ → ไม่โดนดาเมจ
        if (fallHeight < data.minFallHeightForDamage)
            return;

        // แปลงความสูง h → ความเร็วกระแทกประมาณ v = sqrt(2gh)
        float g = 9.81f;
        float impactVelocity = Mathf.Sqrt(2f * g * fallHeight);

        ApplyImpactFromContainer(impactVelocity, protectionMultiplier);
    }

    // -------------------------------------------------------
    // ฟังก์ชันกลางไว้ลดคุณภาพ + เช็คสถานะ พัง/เสียหาย
    // -------------------------------------------------------
    /// <summary>
    /// หักคุณภาพตาม amount และอัปเดต isDamaged / isBroken ตาม threshold ใน Data
    /// </summary>
    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;

        currentQuality -= amount;
        currentQuality = Mathf.Clamp(currentQuality, 0f, 100f);

        // ถ้ามี Data ให้ใช้ threshold ใน Data มาช่วยตัดสินสถานะ
        if (data != null)
        {
            // ถ้าคุณภาพต่ำกว่า damagedThreshold = ถือว่า "เสียหาย"
            isDamaged = currentQuality <= data.damagedThreshold;

            // ถ้าคุณภาพต่ำกว่า brokenThreshold = ถือว่า "แตก/พัง"
            isBroken = currentQuality <= data.brokenThreshold;
        }
        else
        {
            // กรณีไม่มี Data สำรอง logic ง่าย ๆ
            isDamaged = currentQuality < 100f;
            isBroken = currentQuality <= 0f;
        }
    }

    // -------------------------------------------------------
    // ใช้คำนวณเงินตอนส่งของ
    // -------------------------------------------------------
    /// <summary>
    /// คำนวณเงินที่จะได้จากของชิ้นนี้ เมื่อส่งเสร็จ
    /// - ใช้ baseReward จาก Data เป็นหลัก
    /// - quality ต่ำ → ได้เงินน้อยลง
    /// - ส่งเกินกำหนดวัน → หักเพิ่ม
    /// - ถ้าพัง (isBroken) → ได้ 0
    /// </summary>
    public int CalculateReward(int dayCreated, int dayDelivered)
    {
        if (data == null) return 0;

        int daysUsed = Mathf.Max(0, dayDelivered - dayCreated);

        // 1) เงินพื้นฐาน
        float reward = data.baseReward;

        // 2) หักตามคุณภาพปัจจุบัน (0-100 → 0.0-1.0)
        float qualityFactor = currentQuality / 100f;
        reward *= qualityFactor;

        // 3) ถ้าส่งช้ากว่า deliveryLimitDays ให้หักเพิ่ม
        if (daysUsed > data.deliveryLimitDays)
        {
            // ตัวอย่าง: ถ้าช้า → ได้แค่ 50%
            reward *= 0.5f;
        }

        // 4) ถ้าพังแล้ว → ไม่ได้อะไรเลย
        if (isBroken)
            reward = 0f;

        return Mathf.Max(0, Mathf.RoundToInt(reward));
    }
}
