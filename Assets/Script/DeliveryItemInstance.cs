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

        // ถ้าอยากใช้ logic โดนน้ำค่อยมาเปิดทีหลัง
        // if (collision.collider.CompareTag("Water") && data.breaksOnWater)
        // {
        //     ApplyDamage(999f);
        //     Debug.Log($"[ItemInstance] {data.itemName} พังเพราะโดนน้ำ");
        //     return;
        // }

        // ใช้ความเร็วชนแปลงเป็น "ความสูงที่ตก" โดยประมาณ
        float v = collision.relativeVelocity.magnitude;
        float g = 9.81f;
        float approxHeight = (v * v) / (2f * g);

        // ของเปล่า → divisor = 1 (ดาเมจเต็ม)
        ApplyFallHeight(approxHeight, 1);
    }

    public void ApplyFallHeight(float fallHeight, int damageDivisor)
    {
        if (data == null || isBroken) return;

        // ปัดเป็นเมตร (จำนวนเต็ม)
        int meters = Mathf.RoundToInt(fallHeight);
        if (meters < data.minFallHeightMeter) return;

        int perMeter = Mathf.Max(0, data.damagePerMeter);
        int raw = perMeter * meters;

        int divisor = Mathf.Max(1, damageDivisor); // อย่างน้อย 1
        int dmg = raw / divisor;
        if (dmg <= 0) dmg = 1;

        ApplyDamage(dmg);

        Debug.Log($"[ItemInstance] {data.itemName} fallHeight≈{fallHeight:F2}m ({meters}m), perMeter={perMeter}, divisor={divisor}, dmg={dmg}, Q={currentQuality:F0}");
    }


    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;

        currentQuality -= amount;
        currentQuality = Mathf.Clamp(currentQuality, 0f, 100f);

        if (data != null)
        {
            isDamaged = currentQuality <= data.damagedThreshold;
            isBroken = currentQuality <= data.brokenThreshold;
        }
        else
        {
            isDamaged = currentQuality < 100f;
            isBroken = currentQuality <= 0f;
        }
    }
    public int CalculateEffectiveDeadlineDays(int baseDays, bool inColdBox)
    {
        if (data == null) return baseDays;

        if (!data.requiresCold) return baseDays;

        // ต้องการกล่องเย็น
        if (inColdBox) return baseDays;

        // ใส่กล่องธรรมดา → เหลือ 1/3 (อย่างน้อย 1 วัน)
        int reduced = baseDays / 3;
        return Mathf.Max(1, reduced);
    }

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
