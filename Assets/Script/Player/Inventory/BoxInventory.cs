using NUnit;
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
    [Tooltip("ตัวหารดาเมจตอนอยู่ใน inventory (2 = ครึ่งหนึ่ง, 3 = เหลือ 1/3)")]
    public int inventoryDamageDivisor = 2;
    [Header("Water Damage (Inventory)")]
    [Tooltip("ดาเมจต่อวินาทีสำหรับ item ที่ waterSensitive เมื่อผู้เล่นอยู่ในน้ำ")]
    public float waterSensitiveDamagePerSecond = 1f;



    [Serializable]
    public class BoxSlot
    {
        public bool hasBox;
        public BoxKind boxType;
        public DeliveryItemData itemData;

        [Header("ITEM EXTRA INFO")]
        public string ownerNPCName;
        public string address;
        [TextArea]
        public string information;

        [Header("QUALITY")]
        [Range(0, 100)]
        public float itemQuality = 100f;

        [Header("DELIVERY TIME")]
        public int remainingDays = 0;

        [Header("STATE")]
        public bool isDamaged;
        public bool isBroken;

        [Header("PROTECTION SNAPSHOT")]
        [Tooltip("ตัวหารดาเมจรวมที่เซฟมาจากกล่อง + บับเบิล ตอนเก็บเข้าช่องนี้")]
        public int protectionDivisor = 1;

        [Tooltip("เปอร์เซ็นต์การเซฟดาเมจ (0–100%)")]
        [Range(0f, 100f)]
        public float protectionPercent = 0f;

        [Tooltip("กล่องนี้เป็น Waterproof (กันน้ำ100%) หรือไม่")]
        public bool isWaterproof = false;
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

    void UpdateItemState(BoxSlot slot)
    {
        if (slot.itemData == null) return;

        slot.isDamaged = slot.itemQuality <= slot.itemData.damagedThreshold;
        slot.isBroken = slot.itemQuality <= slot.itemData.brokenThreshold;
    }
    public void AdvanceOneDay()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s.hasBox) continue;

            s.remainingDays--;

            if (s.remainingDays < 0)
                s.remainingDays = 0;

            Debug.Log($"[BoxInventory] Slot {i} remainingDays = {s.remainingDays}");
        }
    }
    // ================== DELIVERY FROM INVENTORY ==================
    public void ApplyObstacleDamage(float rawDamage)
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s.hasBox || s.itemData == null) continue;
            if (s.isBroken) continue;

            // 🔹 logic: โดนทุกกล่อง (หรือคุณจะเลือกโดนแค่ 1 ก็ได้)
            int divisor = Mathf.Max(1, s.protectionDivisor);
            float finalDmg = rawDamage / divisor;

            float oldQ = s.itemQuality;
            s.itemQuality = Mathf.Clamp(oldQ - finalDmg, 0f, 100f);

            // update state
            s.isDamaged = s.itemQuality <= s.itemData.damagedThreshold;
            s.isBroken = s.itemQuality <= s.itemData.brokenThreshold;

            Debug.Log(
                $"[ObstacleDamage] Slot {i} {s.itemData.itemName}: " +
                $"Q {oldQ:F1} → {s.itemQuality:F1}"
            );
        }
    }

    public int FindSlotByDestination(string destId)
    {
        if (string.IsNullOrEmpty(destId) || slots == null) return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s.hasBox || s.itemData == null) continue;

            // สมมติว่าใน DeliveryItemData มี field ชื่อ destinationId
            if (s.itemData.destinationId == destId)
                return i;
        }

        return -1;
    }

    public bool TryDeliverFromInventory(string destId, out int reward)
    {
        reward = 0;

        int slotIndex = FindSlotByDestination(destId);
        if (slotIndex < 0) return false;

        var slot = slots[slotIndex];
        if (!slot.hasBox || slot.itemData == null) return false;

        var data = slot.itemData;

        // ========= คำนวณค่า reward แบบง่าย =========
        float r = data.baseReward;                     // เงินพื้นฐานจาก Data
        float qualityFactor = Mathf.Clamp01(slot.itemQuality / 100f);
        r *= qualityFactor;                           // คุณภาพต่ำ → เงินน้อยลง

        // ถ้าคุณอยากให้ "ของพัง = 0 บาท" ง่าย ๆ:
        if (slot.itemQuality <= 0f)
            r = 0f;

        reward = Mathf.Max(0, Mathf.RoundToInt(r));

        // ลบของจาก inventory
        slot.hasBox = false;
        slot.itemData = null;
        // (slot.itemQuality ยังเก็บค่าล่าสุดไว้ได้ เผื่อ debug)

        Debug.Log($"[BoxInventory] DeliverFromInventory dest={destId}, reward={reward}");
        var hud = FindFirstObjectByType<BoxInventoryHUD>(
    FindObjectsInactive.Include
);

        if (hud != null)
        {
            hud.RefreshHUD();
        }
        return true;
    }

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
        slot.remainingDays =
            box.CurrentItemInstance.CalculateEffectiveDeadlineDays(
                box.CurrentItemData.deliveryLimitDays,
                box.boxType == BoxKind.ColdBox
            );

        // 🔹 ดึงค่าการเซฟจากกล่อง (รวมกล่อง + บับเบิล)
        int div = box.GetTotalDamageDivisor();
        slot.protectionDivisor = div;

        float p01 = box.GetProtection01();
        slot.protectionPercent = p01 * 100f;

        // 🔹 เซฟ flag กันน้ำ
        slot.isWaterproof = box.IsWaterproof;
        var itemInst = box.CurrentItemInstance;

        // ===== SNAPSHOT EXTRA INFO =====

        // 1) NPC Name (จาก NPCData)
        if (itemInst != null && itemInst.ownerNPC != null && itemInst.ownerNPC.data != null)
        {
            slot.ownerNPCName = itemInst.ownerNPC.data.npcName;
        }
        else
        {
            slot.ownerNPCName = "Unknown";
        } 

        // 2) Address (copy จาก ItemData)
        slot.address = slot.itemData != null
            ? slot.itemData.address
            : "";

        // 3) Information (copy จาก ItemData)
        slot.information = slot.itemData != null
            ? slot.itemData.information
            : "";

        // เซ็ตสถานะ
        UpdateItemState(slot);

        Debug.Log($"[BoxInventory] StoreBox: slot={free}, item={slot.itemData.itemName}, " +
                  $"Q={slot.itemQuality:F1}, protectDiv={slot.protectionDivisor}, save={slot.protectionPercent:F0}%");

        Destroy(box.gameObject);



        var hud = FindFirstObjectByType<BoxInventoryHUD>(
            FindObjectsInactive.Include
        );

        if (hud != null)
        {
            Debug.Log("Force HUD Refresh");

            hud.gameObject.SetActive(true);
            hud.RefreshHUD();
        }

        InventorySelectionController.Instance?.RefreshAll();

        return true;



    }

    // ---------------- เอากล่องจาก inventory ออกมาในโลก ----------------
    //public BoxCore SpawnBoxFromSlot(int slotIndex, Transform spawnPoint)
    //{
    //    if (slotIndex < 0 || slotIndex >= slots.Length)
    //    {
    //        Debug.LogWarning($"[BoxInventory] SpawnBoxFromSlot: index {slotIndex} ไม่ถูกต้อง");
    //        return null;
    //    }

    //    var slot = slots[slotIndex];
    //    if (!slot.hasBox || slot.itemData == null)
    //    {
    //        Debug.Log($"[BoxInventory] SpawnBoxFromSlot: slot {slotIndex} ว่าง");
    //        return null;
    //    }

    //    if (!boxPrefab)
    //    {
    //        Debug.LogError("[BoxInventory] boxPrefab ยังไม่ได้เซ็ต");
    //        return null;
    //    }

    //    GameObject go = Instantiate(boxPrefab, spawnPoint.position, spawnPoint.rotation);
    //    var core = go.GetComponent<BoxCore>();
    //    var itemInst = go.GetComponentInChildren<DeliveryItemInstance>();

    //    if (!core || !itemInst)
    //    {
    //        Debug.LogError("[BoxInventory] prefab ไม่มี BoxCore หรือ DeliveryItemInstance");
    //        return null;
    //    }

    //    core.boxType = slot.boxType;
    //    itemInst.data = slot.itemData;
    //    itemInst.currentQuality = slot.itemQuality;
    //    //core.SetCurrent(this);

    //    Debug.Log($"[BoxInventory] SpawnBoxFromSlot: เอา {slot.itemData.itemName} ออกจาก slot {slotIndex} ด้วย quality={slot.itemQuality:F1}");

    //    slot.hasBox = false;
    //    slot.itemData = null;
    //    // slot.itemQuality จะยังเก็บค่าล่าสุดไว้ (ใช้ debug ได้)

    //    return core;
    //}
    public void ApplyWaterDamageToSensitive(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s.hasBox || s.itemData == null) continue;

            var data = s.itemData;

            // ของที่ไม่ได้ตั้งให้ sensitive กับน้ำ → ไม่โดน
            if (!data.waterSensitive) continue;

            // ถ้าของพังไปแล้ว → ปล่อย
            if (s.isBroken) continue;

            // กล่องกันน้ำ → กัน 100%
            if (s.isWaterproof)
            {
                // debug เผื่อดูใน Console
                // Debug.Log($"[WaterInv] Slot {i} {data.itemName}: waterproof box → no water damage");
                continue;
            }

            // base damage = 1 หน่วย/วินาที (หรือค่าที่ตั้งใน waterSensitiveDamagePerSecond)
            float baseDmgPerSec = waterSensitiveDamagePerSecond;

            // รวม % การป้องกันจากกล่อง + บับเบิล
            int divisor = Mathf.Max(1, s.protectionDivisor);
            float effectiveDmgPerSec = baseDmgPerSec / divisor;

            float dmg = effectiveDmgPerSec * deltaTime;
            if (dmg <= 0f) dmg = 0.01f; // กันไม่ให้กลืนหายหมด

            float oldQ = s.itemQuality;
            s.itemQuality = Mathf.Clamp(oldQ - dmg, 0f, 100f);

            UpdateItemState(s);

            Debug.Log($"[WaterInv] slot {i} {data.itemName}: base={baseDmgPerSec:F2}/s, div={divisor}, " +
                      $"dmg={dmg:F3}, Q {oldQ:F1}→{s.itemQuality:F1}");
        }
    }
    public void ApplyWaterDamageTick(float damagePerTick)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s.hasBox || s.itemData == null) continue;

            var data = s.itemData;

            // ถ้าไอเท็มนี้ไม่พังเพราะน้ำ → ข้าม
            if (!data.breaksOnWater) continue;

            // ถ้ากล่องเป็นแบบกันน้ำ 100% → ไม่โดน
            if (s.isWaterproof)
            {
                Debug.Log($"[WaterDamage] Slot {i} {data.itemName}: waterproof box → no damage");
                continue;
            }

            // รวม % การเซฟจากกล่อง + บับเบิล (ใช้ protectionDivisor เดิม)
            int divisor = Mathf.Max(1, s.protectionDivisor);

            float dmg = damagePerTick / divisor;
            if (dmg <= 0f) dmg = 0.1f; // กันไม่ให้ดาเมจกลายเป็น 0

            float oldQ = s.itemQuality;
            s.itemQuality = Mathf.Clamp(oldQ - dmg, 0f, 100f);

            UpdateItemState(s);

            Debug.Log($"[WaterDamage] Slot {i} {data.itemName}: base={damagePerTick}, div={divisor}, " +
                      $"dmg={dmg:F2}, Q {oldQ:F1}→{s.itemQuality:F1}");
        }
    }

    public void ApplyFallDamageToAll(float fallHeight)
    {
        int meters = Mathf.RoundToInt(fallHeight);
        if (meters <= 0) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s.hasBox || s.itemData == null) continue;

            var data = s.itemData;
            if (meters < data.minFallHeightMeter) continue;

            int perMeter = Mathf.Max(0, data.damagePerMeter);
            int raw = perMeter * meters;

            // 🔹 ใช้ตัวหารเดียวกับที่กล่องเซฟมา (กล่อง + บับเบิล)
            int divisor = Mathf.Max(1, s.protectionDivisor);
            int dmg = raw / divisor;
            if (dmg <= 0) dmg = 1;

            float oldQ = s.itemQuality;
            s.itemQuality = Mathf.Clamp(oldQ - dmg, 0f, 100f);
            UpdateItemState(s);

            Debug.Log($"[BoxInventory] slot {i} {data.itemName}: fall={fallHeight:F2}m " +
                      $"({meters}m), raw={raw}, div={divisor}, dmg={dmg}, Q {oldQ:F0}→{s.itemQuality:F0}");
        }
    }


    public int GetUsedSlotCount()
    {
        int count = 0;
        if (slots == null) return 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].hasBox)
                count++;
        }

        return count;
    }

    public int SlotCount
    {
        get
        {
            return slots != null ? slots.Length : 0;
        }
    }

    public InventorySaveData Capture()
    {
        var data = new InventorySaveData();
        data.slots = new();

        foreach (var s in slots)
        {
            data.slots.Add(new BoxSlotSaveData
            {
                hasBox = s.hasBox,
                boxType = s.boxType,
                itemId = s.itemData ? s.itemData.itemId : "",
                quality = s.itemQuality,
                remainingDays = s.remainingDays,
                protectionDivisor = s.protectionDivisor,
                protectionPercent = s.protectionPercent,
                isWaterproof = s.isWaterproof
            });
        }

        return data;
    }
    public void Restore(InventorySaveData data)
    {
        if (data == null || data.slots == null)
            return;

        for (int i = 0; i < slots.Length && i < data.slots.Count; i++)
        {
            var save = data.slots[i];
            var slot = slots[i];

            slot.hasBox = save.hasBox;
            slot.boxType = save.boxType;
            slot.itemQuality = save.quality;
            slot.remainingDays = save.remainingDays;
            slot.protectionDivisor = save.protectionDivisor;
            slot.protectionPercent = save.protectionPercent;
            slot.isWaterproof = save.isWaterproof;

            if (save.hasBox && !string.IsNullOrEmpty(save.itemId))
            {
                slot.itemData = ItemResolver.GetItem(save.itemId);
            }
            else
            {
                slot.itemData = null;
            }

        }

        Debug.Log("[BoxInventory] Restore complete");
    }

    public int FindSlotByDestinationId(string destinationId)
    {
        if (string.IsNullOrEmpty(destinationId)) return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s.hasBox || s.itemData == null) continue;

            if (s.itemData.destinationId == destinationId)
                return i;
        }
        return -1;
    }
    public BoxSlot GetNearestSlot()
    {
        BoxSlot best = null;
        int bestDay = int.MaxValue;

        foreach (var s in slots)
        {
            if (!s.hasBox || s.itemData == null) continue;

            if (s.remainingDays < bestDay)
            {
                bestDay = s.remainingDays;
                best = s;
            }
        }
        return best;
    }
    public bool HasAnyBox()
    {
        foreach (var s in slots)
        {
            if (s != null && s.hasBox)
                return true;
        }

        return false;
    }
    public BoxSlot GetFirstFilledSlot()
    {
        foreach (var s in slots)
        {
            if (s != null && s.hasBox)
                return s;
        }

        return null;
    }

    public BoxSlot GetNearestSlotInSceneFirst(string currentScene, DestinationRegistry registry)
    {
        if (registry == null)
            return GetNearestSlot(); // fallback ใช้ของเดิม

        BoxSlot bestInScene = null;
        int bestInSceneDay = int.MaxValue;

        BoxSlot bestOther = null;
        int bestOtherDay = int.MaxValue;

        foreach (var s in slots)
        {
            if (!s.hasBox || s.itemData == null)
                continue;

            string destScene = registry.GetSceneById(s.itemData.destinationId);

            if (destScene == currentScene)
            {
                if (s.remainingDays < bestInSceneDay)
                {
                    bestInSceneDay = s.remainingDays;
                    bestInScene = s;
                }
            }
            else
            {
                if (s.remainingDays < bestOtherDay)
                {
                    bestOtherDay = s.remainingDays;
                    bestOther = s;
                }
            }
        }

        // 🔥 สำคัญ: แมพนี้ต้องมาก่อนเสมอ
        return bestInScene != null ? bestInScene : bestOther;
    }
    public bool TryCheckHasItem(string destId, out int reward)
    {
        reward = 0;

        int slotIndex = FindSlotByDestination(destId);
        if (slotIndex < 0) return false;

        var slot = slots[slotIndex];
        if (!slot.hasBox || slot.itemData == null) return false;

        // (optional) ถ้าอยาก preview reward ก็ใส่ได้
        float r = slot.itemData.baseReward;
        float qualityFactor = Mathf.Clamp01(slot.itemQuality / 100f);
        r *= qualityFactor;

        reward = Mathf.Max(0, Mathf.RoundToInt(r));

        return true;
    }
}
