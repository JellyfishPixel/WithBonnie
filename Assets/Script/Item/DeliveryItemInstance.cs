using UnityEngine;


[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DeliveryItemInstance : MonoBehaviour, IAbandonable
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


    public NPC ownerNPC;

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
    public void OnAbandoned()
    {
        if (data == null) return;

        int penalty = Mathf.Max(0, data.baseReward);
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyPenalty(penalty);
        if (ownerNPC != null)
        {
            ownerNPC.ForceExitAndClearItem();
        }

        Destroy(gameObject);


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
        HandleWaterDamage();
    }
    private void HandleWaterDamage()
    {
        if (!inWater) return;
        if (data == null || isBroken) return;

        // ถ้าไม่ได้ตั้งให้ sensitive ก็ไม่ต้องทำอะไร
        if (!data.waterSensitive) return;

        float interval = data.waterDamageInterval > 0f
            ? data.waterDamageInterval
            : waterDamageInterval;
        float damage = data.waterDamagePerTick > 0f
            ? data.waterDamagePerTick
            : waterDamagePerTick;

        waterTimer += Time.deltaTime;

        while (waterTimer >= interval)
        {
            waterTimer -= interval;

            float before = currentQuality;
            ApplyDamage(damage);

            Debug.Log($"[ItemInstance] {data.itemName} water tick dmg={damage:F1}, Q {before:F1} -> {currentQuality:F1}");

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

        int dmg = DeliveryCalculationService.CalculateFallDamage(data, fallHeight, damageDivisor);
        if (dmg <= 0)
            return;  

        ApplyDamage(dmg);

        Debug.Log($"[ItemInstance] {data.itemName} fallHeight≈{fallHeight:F2}m, divisor={Mathf.Max(1, damageDivisor)}, dmg={dmg}, Q={currentQuality:F0}");
    }


    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;

        currentQuality = DeliveryCalculationService.ApplyQualityDamage(currentQuality, amount);
        DeliveryCalculationService.EvaluateQualityState(data, currentQuality, out isDamaged, out isBroken);
    }
    public int CalculateEffectiveDeadlineDays(int baseDays, bool inColdBox, bool hasIceBubble)
    {
        return DeliveryCalculationService.CalculateEffectiveDeadlineDays(data, baseDays, inColdBox, hasIceBubble);
    }

    // overload เก่า (ให้โค้ดเดิมที่เคยเรียกยังใช้ได้)
    public int CalculateEffectiveDeadlineDays(int baseDays, bool inColdBox)
    {
        return CalculateEffectiveDeadlineDays(baseDays, inColdBox, false);
    }


    public int CalculateReward(int dayCreated, int dayDelivered, int effectiveLimitDays)
    {
        return DeliveryCalculationService.CalculateReward(
            data,
            currentQuality,
            dayCreated,
            dayDelivered,
            effectiveLimitDays,
            isBroken);
    }

    // overload เก่า เผื่อที่อื่นยังเรียกอยู่ จะใช้ deliveryLimitDays ตาม data ปกติ
    public int CalculateReward(int dayCreated, int dayDelivered)
    {
        int limit = (data != null) ? data.deliveryLimitDays : 0;
        return CalculateReward(dayCreated, dayDelivered, limit);
    }

}
