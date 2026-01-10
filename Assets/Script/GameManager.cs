using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Time Settings")]
    public int currentDay = 1;
    public int currentHour = 8;   // เริ่ม 08.00
    public int currentMinute = 0;
    public DirectionArrowUI directionArrowUI;

    [Tooltip("เวลาจริง (วินาที) ต่อ 1 ชั่วโมงในเกม")]
    public float realSecondsPerGameHour = 30f;

    float timeAcc;   // ตัวนับเวลาจริง

    [Header("Money (Preview from EconomyManager)")]
    [Tooltip("ใช้ดูรวม ๆ ใน Inspector เฉย ๆ ค่าเงินจริงมาจาก EconomyManager")]
    public int totalMoney = 0;

    [Header("Clock UI")]
    [Tooltip("Text ไว้แสดงเวลา เช่น DAY 1  09:23")]
    public TMP_Text clockText;

    [Header("Delivery Storage (max 3 per round)")]
    public int maxActiveBoxes = 3;

    public bool shopIsOpen = true;
    [Header("Customer State")]
    public NPC currentCustomer;

    public MinimapController minimap;
    public DestinationRegistry destinationRegistry;


    [System.Serializable]
    public class DeliveryRecord
    {
        public BoxCore box;
        public DeliveryItemInstance itemInstance;
        public DeliveryItemData data;

        public int dayCreated;

        // ==== สำหรับระบบปลายทาง / minimap ====
        public string destinationId;      // เก็บแค่ ID ใช้ข้ามซีนได้
        public Transform worldTarget;     // จุดใน "ซีนปัจจุบัน" เท่านั้น
        public RectTransform minimapIcon; // icon ของกล่องนี้บนแมพใน "ซีนปัจจุบัน"
    }


    // กล่องที่อยู่ใน "รอบนี้" (สูงสุด 3)
    public List<DeliveryRecord> activeBoxes = new();
    [ContextMenu("DEV/Register Test Delivery")]
    public void DevRegisterTestDelivery()
    {
        // 🔧 เปลี่ยน ID ตรงนี้ได้ตามที่อยากเทส
        string testDestinationId = "Home1";

        DevRegisterTestDeliveryById(testDestinationId);
    }
    public void DevRegisterTestDeliveryById(string destinationId)
    {
        if (string.IsNullOrWhiteSpace(destinationId))
        {
            Debug.LogWarning("[DEV] destinationId is empty");
            return;
        }

        // หา world target จาก registry
        Transform target = ResolveDestinationTransform(destinationId);

        if (target == null)
        {
            Debug.LogWarning($"[DEV] Destination '{destinationId}' not found in this scene");
            return;
        }

        Debug.Log($"[DEV] Register TEST delivery to '{destinationId}'");

        // ===== Minimap =====
        if (minimap != null)
        {
            minimap.RegisterDeliveryTarget(target);
        }

        // ===== Direction Arrow =====
        if (directionArrowUI != null)
        {
            directionArrowUI.SetTarget(target);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        minimap = FindFirstObjectByType<MinimapController>();
        destinationRegistry = FindFirstObjectByType<DestinationRegistry>();
        UpdateClockUI();
        SyncDayToEconomy();
        SyncMoneyFromEconomy();
    }

    void Update()
    {
        UpdateGameTime();
    }



    void UpdateGameTime()
    {
        timeAcc += Time.deltaTime;

        float secPerGameMinute = realSecondsPerGameHour / 60f;

        while (timeAcc >= secPerGameMinute)
        {
            timeAcc -= secPerGameMinute;
            AdvanceOneMinute();
        }
    }

    void AdvanceOneMinute()
    {
        currentMinute++;

        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;

            if (currentHour >= 24)
            {
                currentHour = 0;

                BoxInventory.Instance?.AdvanceOneDay();

                currentDay++;
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.EndDayAndDeposit();
                    totalMoney = EconomyManager.Instance.TotalFunds;
                }

                Debug.Log($"[GameManager] New Day: {currentDay}");
                SyncDayToEconomy();
            }

        }

        UpdateClockUI();
    }

    void UpdateClockUI()
    {
        if (clockText == null) return;

        // รูปแบบ: DAY 1  09:23
        clockText.text = $"DAY {currentDay}  {currentHour:00}:{currentMinute:00}";
    }

    void SyncDayToEconomy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.currentDay = currentDay;
        }
    }

    void SyncMoneyFromEconomy()
    {
        if (EconomyManager.Instance != null)
        {
            totalMoney = EconomyManager.Instance.TotalFunds;
        }
    }

    // ================== MONEY ==================

    public void AddMoney(int amount)
    {
        // เงินจริงไปอยู่ใน EconomyManager
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddCashToday(amount);
            totalMoney = EconomyManager.Instance.TotalFunds;   // sync ไว้ดูเฉย ๆ
        }
        else
        {
            // fallback เผื่อยังไม่มี EconomyManager ในซีน
            totalMoney += amount;
            if (totalMoney < 0) totalMoney = 0;
        }

        Debug.Log($"[GameManager] Money (preview total funds): {totalMoney}");
    }

    // ================== DELIVERY ==================
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RelinkSceneSystemsAndRebuildMinimap();

        if (minimap != null)
            minimap.RebindWorldBoundsFromScene();
    }

    DestinationRegistry GetDestinationRegistry()
    {
        if (destinationRegistry == null)
        {
            destinationRegistry = FindFirstObjectByType<DestinationRegistry>();
        }
        return destinationRegistry;
    }

    MinimapController GetMinimap()
    {
        if (minimap == null)
            minimap = FindFirstObjectByType<MinimapController>();
        return minimap;
    }
    Transform ResolveDestinationTransform(string destId)
    {
        if (string.IsNullOrWhiteSpace(destId))
            return null;

        string key = destId.Trim();

        // หา registry ทุกตัวในซีนนี้
        var regs = FindObjectsByType<DestinationRegistry>(FindObjectsSortMode.None);
        if (regs == null || regs.Length == 0)
        {
            Debug.LogWarning("[GM] ResolveDestinationTransform: no DestinationRegistry in this scene");
            return null;
        }

        foreach (var reg in regs)
        {
            if (reg == null) continue;

            var t = reg.GetPoint(key);   // เราแก้ GetPoint ให้ trim/ignore-case แล้วนะ
            if (t != null)
            {
                Debug.Log($"[GM] ResolveDestinationTransform: destId='{key}' -> {t.name} (registry={reg.gameObject.name})");
                return t;
            }
        }

        Debug.LogWarning($"[GM] ResolveDestinationTransform: not found destId='{key}' in any DestinationRegistry");
        return null;
    }
    public void MarkDeliveredByDestination(string destinationId)
    {
        if (string.IsNullOrEmpty(destinationId)) return;

        DeliveryRecord rec = null;
        foreach (var r in activeBoxes)
        {
            if (r != null && r.destinationId == destinationId)
            {
                rec = r;
                break;
            }
        }
        if (rec == null) return;

        // ไม่ต้องคำนวณเงินซ้ำ สมมติว่าคุณคิด reward ที่อื่นไปแล้ว
        if (minimap != null && rec.minimapIcon != null)
        {
            minimap.UnregisterIcon(rec.minimapIcon);
            rec.minimapIcon = null;
        }
        if (directionArrowUI != null && rec.worldTarget != null)
        {
            directionArrowUI.RemoveTarget(rec.worldTarget);
        }

        activeBoxes.Remove(rec);
    }

    void RelinkSceneSystemsAndRebuildMinimap()
    {
        var mini = minimap != null ? minimap : FindFirstObjectByType<MinimapController>();

        foreach (var rec in activeBoxes)
        {
            if (rec == null || rec.data == null) continue;

            rec.minimapIcon = null;

            rec.worldTarget = ResolveDestinationTransform(rec.destinationId);

            Debug.Log($"[GM] Relink: destId='{rec.destinationId}' -> worldTarget={(rec.worldTarget ? rec.worldTarget.name : "NULL")}");

            if (mini != null && rec.worldTarget != null)
            {
                rec.minimapIcon = mini.RegisterDeliveryTarget(rec.worldTarget);
            }
        }

        if (directionArrowUI != null)
        {
            directionArrowUI.ClearAll();

            foreach (var r in activeBoxes)
            {
                if (r != null && r.worldTarget != null)
                {
                    directionArrowUI.SetTarget(r.worldTarget);
                }
            }
        }


        minimap = mini;
    }


    public void RegisterNewDelivery(BoxCore box, DeliveryItemInstance item)
    {
        if (!box || !item || !item.data)
        {
            Debug.LogWarning("[GM] RegisterNewDelivery: box/item/data is null");
            return;
        }

        if (activeBoxes.Count >= maxActiveBoxes)
        {
            Debug.LogWarning("[GM] Active boxes is full (3).");
            return;
        }

        var record = new DeliveryRecord
        {
            box = box,
            itemInstance = item,
            data = item.data,
            dayCreated = currentDay,
            destinationId = item.data.destinationId
        };

        Debug.Log($"[GM] NewDelivery item={record.data.itemName}, destId={record.destinationId}");
        // หา worldTarget จากทุก DestinationRegistry ในซีน
        record.worldTarget = ResolveDestinationTransform(record.destinationId);
        Debug.Log($"[GM] RegisterNewDelivery worldTarget => {(record.worldTarget ? record.worldTarget.name : "NULL")}");


        // ===== สร้าง icon บน minimap ถ้าทุกอย่างพร้อม =====
        var mini = minimap != null ? minimap : FindFirstObjectByType<MinimapController>();
        if (mini != null && record.worldTarget != null)
        {
            record.minimapIcon = mini.RegisterDeliveryTarget(record.worldTarget);
            Debug.Log($"[GM]  RegisterDeliveryTarget => {(record.minimapIcon ? record.minimapIcon.name : "NULL")}");
        }
        else
        {
            Debug.LogWarning("[GM]  (RegisterNewDelivery) mini=null หรือ worldTarget=null → ยังไม่สร้าง icon แรก");
        }
        activeBoxes.Add(record);

        if (directionArrowUI != null && record.worldTarget != null)
        {
            directionArrowUI.SetTarget(record.worldTarget);
        }

    }

    public void CompleteDelivery(BoxCore box)
    {
        if (box == null) return;

        DeliveryRecord rec = null;
        foreach (var r in activeBoxes)
        {
            if (r.box == box)
            {
                rec = r;
                break;
            }
        }
        if (rec == null || rec.itemInstance == null || rec.data == null) return;

        bool usedColdBox = false;
        bool hasIceBubble = false;

        if (rec.box != null)
        {
            usedColdBox = (rec.box.boxType == BoxKind.ColdBox);
            hasIceBubble = rec.box.hasIceBubble;
        }

        int baseLimit = rec.data.deliveryLimitDays;
        int effectiveLimit = rec.itemInstance.CalculateEffectiveDeadlineDays(
            baseLimit,
            usedColdBox,
            hasIceBubble
        );

        int dayCreated = rec.dayCreated;
        int dayDelivered = currentDay;

        int reward = rec.itemInstance.CalculateReward(dayCreated, dayDelivered, effectiveLimit);

        AddMoney(reward);
        if (rec.box != null && rec.box.ownerNPC != null)
        {
            rec.box.ownerNPC.HandleBoxStored();
        }
        box.MarkDelivered();

        // ลบ icon ของกล่องนี้ออกจาก minimap (ซีนปัจจุบัน)
        if (minimap != null && rec.minimapIcon != null)
        {
            minimap.UnregisterIcon(rec.minimapIcon);
            rec.minimapIcon = null;
        }
        // อัปเดตลูกศรหลังส่งสำเร็จ
        if (directionArrowUI != null && rec.worldTarget != null)
        {
            directionArrowUI.RemoveTarget(rec.worldTarget);
        }


        activeBoxes.Remove(rec);
    }



}
