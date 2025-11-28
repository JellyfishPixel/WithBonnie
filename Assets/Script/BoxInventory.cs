using System;
using UnityEngine;

public class BoxInventory : MonoBehaviour
{
    public static BoxInventory Instance { get; private set; }

    [Header("Inventory Settings")]
    [Tooltip("จำนวนช่องเก็บกล่องสูงสุด (ตอนนี้ใช้ 3 ช่อง)")]
    public int maxSlots = 3;

    [Header("Box Prefab (ตัวที่ spawn กลับออกมาในโลก)")]
    [Tooltip("Prefab กล่องที่มี BoxCore + DeliveryItemInstance ติดอยู่")]
    public GameObject boxPrefab;

    [Header("Carry Protection")]
    [Tooltip("ตัวคูณดาเมจตอนผู้เล่นถือกล่องไว้ในตัวแล้วตกจากที่สูง 1 = ดาเมจเต็ม, 0.3 = 30% ของดาเมจ")]
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
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // เตรียม Array ช่องเก็บกล่อง
        if (slots == null || slots.Length != maxSlots)
            slots = new BoxSlot[maxSlots];

        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                slots[i] = new BoxSlot();
    }

    // ---------------------------------------------------
    // เข้าถึงข้อมูลช่อง
    // ---------------------------------------------------
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

    // ---------------------------------------------------
    // เก็บกล่องจากโลกเข้า inventory
    // (เรียกจาก PlayerInteractionSystem.StoreHeldBoxToInventory)
    // ---------------------------------------------------
    public bool StoreBox(BoxCore box)
    {
        if (!box || !box.CurrentItemData || !box.CurrentItemInstance)
        {
            Debug.LogWarning("[BoxInventory] Missing BoxCore / CurrentItemData / CurrentItemInstance");
            return false;
        }

        int freeIndex = FindFirstFreeSlot();
        if (freeIndex < 0)
        {
            Debug.Log("[BoxInventory] Inventory full (no free slot).");
            return false;
        }

        var slot = slots[freeIndex];
        slot.hasBox = true;
        slot.boxType = box.boxType;
        slot.itemData = box.CurrentItemData;
        slot.itemQuality = box.CurrentItemInstance.currentQuality;

        // ลบกล่องจากโลก (ถือว่าเก็บเข้าตัวจริง)
        Destroy(box.gameObject);

        Debug.Log($"[BoxInventory] Stored box in slot {freeIndex} ({slot.itemData.itemName})");
        return true;
    }

    // ---------------------------------------------------
    // เอากล่องจากช่องออกมาสร้างในโลก
    // (เรียกจาก PlayerInteractionSystem.TakeBoxFromInventorySlot)
    // ---------------------------------------------------
    public BoxCore SpawnBoxFromSlot(int slotIndex, Transform spawnPoint)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogWarning($"[BoxInventory] Invalid slot index {slotIndex}");
            return null;
        }

        var slot = slots[slotIndex];
        if (!slot.hasBox || slot.itemData == null)
        {
            Debug.Log($"[BoxInventory] Slot {slotIndex} is empty.");
            return null;
        }

        if (!boxPrefab)
        {
            Debug.LogError("[BoxInventory] boxPrefab is not assigned.");
            return null;
        }

        // สร้างกล่องใหม่จาก prefab
        GameObject go = Instantiate(
            boxPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        var core = go.GetComponent<BoxCore>();
        var itemInst = go.GetComponentInChildren<DeliveryItemInstance>();

        if (!core || !itemInst)
        {
            Debug.LogError("[BoxInventory] Box prefab missing BoxCore or DeliveryItemInstance.");
            return null;
        }

        // เซ็ตข้อมูลให้กล่องตามที่เก็บไว้ใน inventory
        core.boxType = slot.boxType;

        itemInst.data = slot.itemData;
        itemInst.currentQuality = slot.itemQuality;
        core.SetAsCurrent(); // ถ้าคุณใช้ Current box

        // เคลียร์ช่องหลังจากเอากล่องออกมาแล้ว
        slot.hasBox = false;
        slot.itemData = null;
        // ไม่ล้าง itemQuality ก็ได้ เผื่อใช้ debug ดูย้อนหลัง

        Debug.Log($"[BoxInventory] Spawned box from slot {slotIndex} ({itemInst.data.itemName})");
        return core;
    }

    // ---------------------------------------------------
    // ดาเมจของใน inventory เวลา player ตกจากที่สูง
    // (เรียกจาก PlayerFallDamageCarrier.ApplyFallDamageToAll)
    // ---------------------------------------------------
    public void ApplyFallDamageToAll(float fallHeight)
    {
        if (fallHeight <= 0f) return;

        // แปลงความสูงการตก → ความเร็วกระแทกประมาณ v ≈ sqrt(2 * g * h)
        float g = 9.81f;
        float impactVelocity = Mathf.Sqrt(2f * g * fallHeight);

        Debug.Log($"[BoxInventory] Fall height={fallHeight:F2}m, impactVel≈{impactVelocity:F2}");

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (!slot.hasBox || slot.itemData == null) continue;

            var data = slot.itemData;

            // ถ้าแรงไม่ถึง safeImpactVelocity → ไม่เสียหาย
            if (impactVelocity <= data.safeImpactVelocity)
                continue;

            float over = impactVelocity - data.safeImpactVelocity;
            float damage = over * data.damagePerVelocity * Mathf.Max(0f, carriedDamageMultiplier);

            if (damage <= 0f) continue;

            slot.itemQuality -= damage;
            slot.itemQuality = Mathf.Clamp(slot.itemQuality, 0f, 100f);

            Debug.Log($"[BoxInventory] Slot {i} {data.itemName} took {damage:F1} dmg → quality={slot.itemQuality:F1}");
        }
    }
}
