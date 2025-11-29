using System;
using UnityEngine;

public class BoxInventory : MonoBehaviour
{
    public static BoxInventory Instance { get; private set; }

    [Header("Inventory Settings")]
    public int maxSlots = 3;

    [Header("Box Prefab")]
    public GameObject boxPrefab;

    [Header("Carry Protection")]
    [Tooltip("ตัวคูณดาเมจตอนอยู่ในตัวผู้เล่น 1 = ดาเมจเต็ม, 0.3 = 30% ของดาเมจ")]
    public float carriedDamageMultiplier = 0.3f;

    [Serializable]
    public class BoxSlot
    {
        public bool hasBox;
        public BoxKind boxType;
        public DeliveryItemData itemData;
        [Range(0, 100)]
        public float itemQuality = 100f;
    }

    public BoxSlot[] slots;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (slots == null || slots.Length != maxSlots)
            slots = new BoxSlot[maxSlots];

        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                slots[i] = new BoxSlot();
    }

    public BoxSlot GetSlot(int idx)
    {
        if (idx < 0 || idx >= slots.Length) return null;
        return slots[idx];
    }

    int FindFirstFreeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
            if (!slots[i].hasBox)
                return i;
        return -1;
    }

    // ---------------- เก็บกล่องเข้า inventory ----------------
    public bool StoreBox(BoxCore box)
    {
        if (!box || !box.CurrentItemData || !box.CurrentItemInstance)
        {
            Debug.LogWarning("[BoxInventory] StoreBox: Box หรือ ItemData/Instance ว่าง");
            return false;
        }

        int free = FindFirstFreeSlot();
        if (free < 0)
        {
            Debug.Log("[BoxInventory] StoreBox: Inventory เต็มแล้ว");
            return false;
        }

        var slot = slots[free];
        slot.hasBox = true;
        slot.boxType = box.boxType;
        slot.itemData = box.CurrentItemData;
        slot.itemQuality = box.CurrentItemInstance.currentQuality;

        Debug.Log($"[BoxInventory] StoreBox: slot={free}, item={slot.itemData.itemName}, quality={slot.itemQuality:F1}");

        Destroy(box.gameObject);
        return true;
    }

    // ---------------- เอากล่องจาก inventory ออกมาในโลก ----------------
    public BoxCore SpawnBoxFromSlot(int slotIndex, Transform spawnPoint)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogWarning($"[BoxInventory] SpawnBoxFromSlot: index {slotIndex} ไม่ถูกต้อง");
            return null;
        }

        var slot = slots[slotIndex];
        if (!slot.hasBox || slot.itemData == null)
        {
            Debug.Log($"[BoxInventory] SpawnBoxFromSlot: slot {slotIndex} ว่าง");
            return null;
        }

        if (!boxPrefab)
        {
            Debug.LogError("[BoxInventory] boxPrefab ยังไม่ได้เซ็ต");
            return null;
        }

        GameObject go = Instantiate(boxPrefab, spawnPoint.position, spawnPoint.rotation);
        var core = go.GetComponent<BoxCore>();
        var itemInst = go.GetComponentInChildren<DeliveryItemInstance>();

        if (!core || !itemInst)
        {
            Debug.LogError("[BoxInventory] prefab ไม่มี BoxCore หรือ DeliveryItemInstance");
            return null;
        }

        core.boxType = slot.boxType;
        itemInst.data = slot.itemData;
        itemInst.currentQuality = slot.itemQuality;
        core.SetAsCurrent();

        Debug.Log($"[BoxInventory] SpawnBoxFromSlot: เอา {slot.itemData.itemName} ออกจาก slot {slotIndex} ด้วย quality={slot.itemQuality:F1}");

        slot.hasBox = false;
        slot.itemData = null;
        // slot.itemQuality จะยังเก็บค่าล่าสุดไว้ (ใช้ debug ได้)

        return core;
    }

    // ---------------- ดาเมจตอนผู้เล่นตกจากที่สูง ----------------
    /// <summary>
    /// ให้ผู้เล่นเรียกเวลาตกจากที่สูง
    /// fallHeight = ส่วนต่างความสูงที่ตก (เมตร)
    /// </summary>
    public void ApplyFallDamageToAll(float fallHeight)
    {
        if (fallHeight <= 0f)
        {
            Debug.Log($"[BoxInventory] ApplyFallDamageToAll: fallHeight={fallHeight:F2} → ไม่คิดดาเมจ");
            return;
        }

        Debug.Log($"[BoxInventory] ApplyFallDamageToAll: เริ่มคำนวนจากความสูงตก={fallHeight:F2} m");

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (!slot.hasBox || slot.itemData == null) continue;

            var data = slot.itemData;

            // ❗ เช็คว่าไอเท็มนี้กำหนด minFallHeightForDamage ไว้เท่าไหร่
            if (fallHeight < data.minFallHeightForDamage)
            {
                Debug.Log($"[BoxInventory] Slot {i} {data.itemName}: ตก {fallHeight:F2} < minFallHeight {data.minFallHeightForDamage:F2} → ยังไม่เสียหาย");
                continue;
            }

            // แปลงความสูง h → ความเร็วกระแทก v ≈ sqrt(2 g h)
            float g = 9.81f;
            float impactV = Mathf.Sqrt(2f * g * fallHeight);

            Debug.Log($"[BoxInventory] Slot {i} {data.itemName}: impactV≈{impactV:F2}, safeV={data.safeImpactVelocity:F2}");

            if (impactV <= data.safeImpactVelocity)
            {
                Debug.Log($"[BoxInventory] Slot {i} {data.itemName}: v ไม่เกิน safeImpactVelocity → ไม่โดนดาเมจ");
                continue;
            }

            float over = impactV - data.safeImpactVelocity;
            float damage = over * data.damagePerVelocity * Mathf.Max(0f, carriedDamageMultiplier);

            if (damage <= 0f)
            {
                Debug.Log($"[BoxInventory] Slot {i} {data.itemName}: คำนวนดาเมจได้น้อยกว่า 0 → ข้าม");
                continue;
            }

            float oldQ = slot.itemQuality;
            slot.itemQuality -= damage;
            slot.itemQuality = Mathf.Clamp(slot.itemQuality, 0f, 100f);

            Debug.Log($"[BoxInventory] Slot {i} {data.itemName}: oldQ={oldQ:F1}, damage={damage:F1}, newQ={slot.itemQuality:F1}");
        }
    }
}
