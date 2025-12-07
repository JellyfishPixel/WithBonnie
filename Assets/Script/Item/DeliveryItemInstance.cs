using UnityEngine;


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

    [Header("Water Damage")]
    [Tooltip("เวลาที่ต้องอยู่ในน้ำต่อ 1 ดาเมจ")]
    public float waterDamageInterval = 3f;

    [Tooltip("ดาเมจต่อหนึ่ง interval จากน้ำ")]
    public float waterDamagePerTick = 1f;



    bool inWater = false;
    float waterTimer = 0f;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // ถ้ามี Data ให้เซ็ตคุณภาพเริ่มต้นจาก baseQuality
        if (data != null)
            currentQuality = data.baseQuality;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (data == null || isBroken) return;

        if (!other.CompareTag("Water")) return;

        // กรณี "พังทันทีเมื่อโดนน้ำ"
        if (data.breaksOnWater)
        {
            Debug.Log($"[ItemInstance] {data.itemName} hit water -> breaksOnWater=true");
            // ทำให้พังเลย: ดาเมจเท่ากับคุณภาพที่เหลือ
            ApplyDamage(currentQuality);
            return;
        }

        // กรณี "ค่อย ๆ เสื่อมเมื่ออยู่ในน้ำ"
        if (data.waterSensitive)
        {
            inWater = true;
            waterTimer = 0f;
            Debug.Log($"[ItemInstance] {data.itemName} enter water (waterSensitive)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Water")) return;

        if (inWater)
        {
            inWater = false;
            Debug.Log("[ItemInstance] leave water");
        }
    }


    void Update()
    {
        if (!inWater || data == null || isBroken) return;

        // ถ้าของนี้ไม่แคร์น้ำ (breaksOnWater=false) จะข้าม
        if (!data.breaksOnWater) return;

        waterTimer += Time.deltaTime;
        if (waterTimer >= waterDamageInterval)
        {
            waterTimer -= waterDamageInterval;

            // 3 วินาที → 1 ดาเมจ
            ApplyDamage(waterDamagePerTick);

            Debug.Log($"[ItemInstance] {data.itemName} water dmg={waterDamagePerTick}, Q={currentQuality:F0}");
        }

        HandleWaterDamage();
    }
    private void HandleWaterDamage()
    {
        if (!inWater) return;
        if (data == null || isBroken) return;

        // ถ้าไม่ได้ตั้งให้ sensitive ก็ไม่ต้องทำอะไร
        if (!data.waterSensitive) return;

        // นับเวลา
        waterTimer += Time.deltaTime;

        // ทุกๆ 1 วินาที -> ลดคุณภาพ 1 หน่วย
        while (waterTimer >= 1f)
        {
            waterTimer -= 1f;

            float before = currentQuality;
            ApplyDamage(1f);  // ลด 1 หน่วย

            Debug.Log($"[ItemInstance] {data.itemName} water tick dmg=1, Q {before:F1} -> {currentQuality:F1}");

            if (isBroken)     // ถ้าพังแล้ว จะไม่ต้องนับต่อ
            {
                inWater = false;
                break;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (data == null || isBroken) return;

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
    public int CalculateEffectiveDeadlineDays(int baseDays, bool inColdBox, bool hasIceBubble)
    {
        if (data == null) return baseDays;

        // ถ้าไม่ใช่ของที่ต้องเย็น → ใช้ baseDays ปกติ
        if (!data.requiresCold) return baseDays;

        // ===== กรณีใช้กล่องเย็นถูกต้อง =====
        if (inColdBox)
        {
            // ปกติใช้ baseDays
            int result = baseDays;

            // ถ้ามี Ice bubble → เพิ่มเวลาอีก 1 วัน (ปรับได้ตามชอบ)
            if (hasIceBubble)
            {
                result += 1;
            }

            return result;
        }

        // ===== ใส่กล่องธรรมดา (ผิดประเภท) =====
        // ไม่ว่าจะมี Ice bubble หรือไม่ ก็ถือว่าผิด → ลดเหลือ 1/3
        int reduced = baseDays / 3;
        return Mathf.Max(1, reduced);
    }

    // overload เก่า (ให้โค้ดเดิมที่เคยเรียกยังใช้ได้)
    public int CalculateEffectiveDeadlineDays(int baseDays, bool inColdBox)
    {
        return CalculateEffectiveDeadlineDays(baseDays, inColdBox, false);
    }


    public int CalculateReward(int dayCreated, int dayDelivered, int effectiveLimitDays)
    {
        if (data == null) return 0;

        int daysUsed = Mathf.Max(0, dayDelivered - dayCreated);

        // 1) เงินพื้นฐาน
        float reward = data.baseReward;

        // 2) หักตามคุณภาพปัจจุบัน (0-100 → 0.0-1.0)
        float qualityFactor = currentQuality / 100f;
        reward *= qualityFactor;

        // 3) เช็คดีเลย์โดยใช้ effectiveLimitDays (จากระบบกล่องเย็น/ธรรมดา)
        if (effectiveLimitDays > 0 && daysUsed > effectiveLimitDays)
        {
            // ตัวอย่าง: ถ้าส่งช้ากว่า deadline → ได้แค่ 50%
            reward *= 0.5f;
        }

        // 4) ถ้าพังแล้ว → ไม่ได้อะไรเลย
        if (isBroken)
            reward = 0f;

        return Mathf.Max(0, Mathf.RoundToInt(reward));
    }

    // overload เก่า เผื่อที่อื่นยังเรียกอยู่ จะใช้ deliveryLimitDays ตาม data ปกติ
    public int CalculateReward(int dayCreated, int dayDelivered)
    {
        int limit = (data != null) ? data.deliveryLimitDays : 0;
        return CalculateReward(dayCreated, dayDelivered, limit);
    }

}
