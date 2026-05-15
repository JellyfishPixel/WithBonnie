using System;
using UnityEngine;

public class BoxInventory : MonoBehaviour
{
    public static BoxInventory Instance { get; private set; }
    public static event Action QuestPinChanged;

    [Header("Inventory Settings")]
    public int maxSlots = 3;

    [Header("Box Prefab")]
    public GameObject boxPrefab;
    [SerializeField] int pinnedSlotIndex = -1;
    [SerializeField] GameManager gameManager;

    Transform storageRoot;

    [Header("Carry Protection")]
    [Tooltip("ตัวหารดาเมจตอนอยู่ใน inventory (2 = ครึ่งหนึ่ง, 3 = เหลือ 1/3)")]
    public int inventoryDamageDivisor = 2;
    [Header("Water Damage (Inventory)")]
    [Tooltip("ดาเมจต่อวินาทีสำหรับ item ที่ waterSensitive เมื่อผู้เล่นอยู่ในน้ำ")]
    public float waterSensitiveDamagePerSecond = 1f;



    [Serializable]
    public class BoxSlot
    {
        public PackedBoxData packageData;
        public BoxCore storedBoxShell;
        public bool hasBox;

        public BoxKind boxType
        {
            get => packageData != null ? packageData.boxType : default;
            set
            {
                EnsurePackage();
                packageData.boxType = value;
            }
        }

        public DeliveryItemData itemData
        {
            get => packageData != null ? packageData.itemData : null;
            set
            {
                EnsurePackage();
                packageData.itemData = value;
                packageData.RefreshState();
            }
        }

        public string ownerNPCName
        {
            get => packageData != null ? packageData.ownerNPCName : "";
            set
            {
                EnsurePackage();
                packageData.ownerNPCName = value;
            }
        }

        public string address
        {
            get => packageData != null ? packageData.address : "";
            set
            {
                EnsurePackage();
                packageData.address = value;
            }
        }

        public string information
        {
            get => packageData != null ? packageData.information : "";
            set
            {
                EnsurePackage();
                packageData.information = value;
            }
        }

        public float itemQuality
        {
            get => packageData != null ? packageData.itemQuality : 100f;
            set
            {
                EnsurePackage();
                packageData.itemQuality = value;
                packageData.RefreshState();
            }
        }

        public int remainingDays
        {
            get => packageData != null ? packageData.remainingDays : 0;
            set
            {
                EnsurePackage();
                packageData.remainingDays = value;
            }
        }

        public bool isDamaged
        {
            get => packageData != null && packageData.isDamaged;
            set
            {
                EnsurePackage();
                packageData.isDamaged = value;
            }
        }

        public bool isBroken
        {
            get => packageData != null && packageData.isBroken;
            set
            {
                EnsurePackage();
                packageData.isBroken = value;
            }
        }

        public int protectionDivisor
        {
            get => packageData != null ? packageData.protectionDivisor : 1;
            set
            {
                EnsurePackage();
                packageData.protectionDivisor = value;
            }
        }

        public float protectionPercent
        {
            get => packageData != null ? packageData.protectionPercent : 0f;
            set
            {
                EnsurePackage();
                packageData.protectionPercent = value;
            }
        }

        public bool isWaterproof
        {
            get => packageData != null && packageData.isWaterproof;
            set
            {
                EnsurePackage();
                packageData.isWaterproof = value;
            }
        }

        void EnsurePackage()
        {
            if (packageData == null)
                packageData = new PackedBoxData();
        }

        public void Clear(bool destroyShell = true)
        {
            if (destroyShell && storedBoxShell != null)
                Destroy(storedBoxShell.gameObject);

            storedBoxShell = null;
            packageData = null;
            hasBox = false;
        }

        public void SetFromPackage(PackedBoxData package)
        {
            packageData = package;
            hasBox = package != null;
            if (packageData != null)
                packageData.RefreshState();
        }

        public void SetStoredShell(BoxCore shell)
        {
            storedBoxShell = shell;
        }

        public void SyncPackageState()
        {
            if (packageData == null) return;
            packageData.RefreshState();
        }
    }




    public BoxSlot[] slots;
    public int PinnedSlotIndex => pinnedSlotIndex;

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

        storageRoot = transform.Find("StoredBoxes");
        if (storageRoot == null)
        {
            GameObject root = new GameObject("StoredBoxes");
            root.transform.SetParent(transform, false);
            storageRoot = root.transform;
        }
    }

    GameManager GameManagerRef
    {
        get
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;

            return gameManager;
        }
    }

    public BoxSlot GetSlot(int idx)
    {
        if (idx < 0 || idx >= slots.Length) return null;
        return slots[idx];
    }

    public bool IsSlotPinned(int idx)
    {
        return pinnedSlotIndex == idx;
    }

    public bool IsSlotTracked(int idx, string currentScene, DestinationRegistry registry)
    {
        if (idx < 0 || idx >= slots.Length)
            return false;

        var slot = slots[idx];
        if (slot == null || !slot.hasBox || slot.itemData == null)
            return false;

        var preferred = GetPreferredQuestSlot(currentScene, registry);
        return preferred == slot;
    }

    public void TogglePinSlot(int idx)
    {
        if (pinnedSlotIndex == idx)
            ClearPinnedSlot();
        else
            SetPinnedSlot(idx);
    }

    public void SetPinnedSlot(int idx)
    {
        if (idx < 0 || idx >= slots.Length)
        {
            ClearPinnedSlot();
            return;
        }

        var slot = slots[idx];
        if (slot == null || !slot.hasBox || slot.itemData == null)
        {
            ClearPinnedSlot();
            return;
        }

        pinnedSlotIndex = idx;
        RefreshQuestTrackingUI();
        QuestPinChanged?.Invoke();
    }

    public void ClearPinnedSlot()
    {
        pinnedSlotIndex = -1;
        RefreshQuestTrackingUI();
        QuestPinChanged?.Invoke();
    }

    public BoxSlot GetPinnedSlot()
    {
        if (pinnedSlotIndex < 0 || pinnedSlotIndex >= slots.Length)
            return null;

        var slot = slots[pinnedSlotIndex];
        if (slot == null || !slot.hasBox || slot.itemData == null)
            return null;

        return slot;
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

        DeliveryCalculationService.EvaluateQualityState(
            slot.itemData,
            slot.itemQuality,
            out bool isDamaged,
            out bool isBroken);

        slot.isDamaged = isDamaged;
        slot.isBroken = isBroken;
        slot.SyncPackageState();
    }

    void ValidatePinnedSlot()
    {
        if (GetPinnedSlot() != null)
            return;

        pinnedSlotIndex = -1;
    }

    void RefreshQuestTrackingUI()
    {
        ValidatePinnedSlot();

        var hud = FindFirstObjectByType<BoxInventoryHUD>(FindObjectsInactive.Include);
        if (hud != null)
            hud.RefreshHUD();

        var smallHud = FindFirstObjectByType<HUDNearestSlotUI>(FindObjectsInactive.Include);
        if (smallHud != null)
            smallHud.ShowHUDTemporarily();

        var arrow = FindFirstObjectByType<DirectionArrowUI>(FindObjectsInactive.Include);
        if (arrow != null)
            arrow.Rebuild();

        InventorySelectionController.Instance?.RefreshAll();
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

            s.SyncPackageState();

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
            s.itemQuality = DeliveryCalculationService.ApplyQualityDamage(oldQ, finalDmg);

            // update state
            UpdateItemState(s);

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

        return TryDeliverSlot(slotIndex, out reward, destroyShell: true);
    }

    public bool TryDeliverSlot(int slotIndex, out int reward, bool destroyShell = true)
    {
        reward = 0;

        if (slotIndex < 0 || slotIndex >= slots.Length)
            return false;

        var slot = slots[slotIndex];
        if (!slot.hasBox || slot.itemData == null) return false;

        var data = slot.itemData;

        reward = DeliveryCalculationService.CalculateReward(
            data,
            slot.itemQuality,
            0,
            0,
            slot.remainingDays,
            slot.isBroken);

        // ลบของจาก inventory
        slot.Clear(destroyShell);
        ValidatePinnedSlot();

        Debug.Log($"[BoxInventory] DeliverFromInventory slot={slotIndex}, reward={reward}");
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
        var package = box.CreatePackedData();
        if (package == null)
        {
            Debug.LogWarning("[BoxInventory] StoreBox: Could not create packed data.");
            return false;
        }

        slot.SetFromPackage(package);
        slot.SetStoredShell(box);
        ValidatePinnedSlot();

        Debug.Log($"[BoxInventory] StoreBox: slot={free}, item={slot.itemData.itemName}, " +
                  $"Q={slot.itemQuality:F1}, protectDiv={slot.protectionDivisor}, save={slot.protectionPercent:F0}%");

        GameManagerRef?.RegisterNewDelivery(package);

        box.PrepareForInventoryStorage(storageRoot);

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

    public BoxCore SpawnBoxFromSlot(int slotIndex, Transform spawnPoint, bool clearSlot = false)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return null;

        if (spawnPoint == null)
            return null;

        var slot = slots[slotIndex];
        if (!slot.hasBox || slot.packageData == null)
            return null;

        if (slot.storedBoxShell != null)
        {
            BoxCore storedShell = slot.storedBoxShell;
            storedShell.RestoreStoredShell(slot.packageData, spawnPoint.position, spawnPoint.rotation);

        if (clearSlot)
        {
            slot.SetStoredShell(null);
            slot.Clear(destroyShell: false);
            ValidatePinnedSlot();
        }

            return storedShell;
        }

        GameObject prefabToSpawn = slot.packageData.boxPrefab != null
            ? slot.packageData.boxPrefab
            : boxPrefab;

        if (prefabToSpawn == null)
            return null;

        BoxCore box = PackedBoxRuntimeFactory.Spawn(
            prefabToSpawn,
            slot.packageData,
            spawnPoint.position,
            spawnPoint.rotation);

        if (box == null && prefabToSpawn != boxPrefab && boxPrefab != null)
        {
            Debug.LogWarning($"[BoxInventory] Failed to spawn saved prefab '{prefabToSpawn.name}'. Falling back to default box prefab.");
            box = PackedBoxRuntimeFactory.Spawn(
                boxPrefab,
                slot.packageData,
                spawnPoint.position,
                spawnPoint.rotation);
        }

        if (box == null)
            return null;

        if (clearSlot)
            slot.Clear();

        return box;
    }
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
            s.itemQuality = DeliveryCalculationService.ApplyQualityDamage(oldQ, dmg);

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
            s.itemQuality = DeliveryCalculationService.ApplyQualityDamage(oldQ, dmg);

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

            int dmg = DeliveryCalculationService.CalculateFallDamage(data, fallHeight, s.protectionDivisor);
            if (dmg <= 0) continue;

            float oldQ = s.itemQuality;
            s.itemQuality = DeliveryCalculationService.ApplyQualityDamage(oldQ, dmg);
            UpdateItemState(s);

            Debug.Log($"[BoxInventory] slot {i} {data.itemName}: fall={fallHeight:F2}m " +
                      $"({meters}m), div={Mathf.Max(1, s.protectionDivisor)}, dmg={dmg}, Q {oldQ:F0}→{s.itemQuality:F0}");
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

            if (slot.hasBox && slot.itemData != null)
            {
                slot.SetFromPackage(new PackedBoxData
                {
                    boxType = slot.boxType,
                    itemData = slot.itemData,
                    destinationId = slot.itemData.destinationId,
                    ownerNPCName = slot.ownerNPCName,
                    address = slot.address,
                    information = slot.information,
                    itemQuality = slot.itemQuality,
                    remainingDays = slot.remainingDays,
                    protectionDivisor = slot.protectionDivisor,
                    protectionPercent = slot.protectionPercent,
                    isWaterproof = slot.isWaterproof
                });
            }
            else
            {
                slot.Clear();
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

    public BoxSlot GetPreferredQuestSlot(string currentScene, DestinationRegistry registry)
    {
        ValidatePinnedSlot();

        var pinned = GetPinnedSlot();
        if (pinned != null)
            return pinned;

        return GetNearestSlotInSceneFirst(currentScene, registry);
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
        reward = DeliveryCalculationService.CalculateReward(
            slot.itemData,
            slot.itemQuality,
            0,
            0,
            slot.remainingDays,
            slot.isBroken);

        return true;
    }
}

