using System.Collections.Generic;
using UnityEngine;
using TMPro; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Time Settings")]
    public int currentDay = 1;
    public int currentHour = 8;   // เริ่ม 08.00
    public int currentMinute = 0;

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

        // ==== ใหม่ สำหรับ minimap/destination ====
        public string destinationId;   // มาจาก DeliveryItemData
        public Transform worldTarget;  // จุดจริงในฉาก (หาได้จาก DestinationRegistry)
        public bool minimapRegistered; // เคยส่งไปให้ minimap หรือยัง
    }

    // กล่องที่อยู่ใน "รอบนี้" (สูงสุด 3)
    public List<DeliveryRecord> activeBoxes = new();

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
        // อัปเดต UI เวลา + sync วันไป EconomyManager (ถ้ามี)
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
    DestinationRegistry GetDestinationRegistry()
    {
        if (destinationRegistry == null)
        {
            destinationRegistry = FindFirstObjectByType<DestinationRegistry>();
        }
        return destinationRegistry;
    }

    public void RegisterNewDelivery(BoxCore box, DeliveryItemInstance item)
    {
        if (!box || !item || !item.data) return;

        if (activeBoxes.Count >= maxActiveBoxes)
        {
            Debug.LogWarning("[GameManager] Active boxes is full (3). ต้องไปจัดการ UI เอาออกก่อน");
            return;
        }

        var record = new DeliveryRecord
        {
            box = box,
            itemInstance = item,
            data = item.data,
            dayCreated = currentDay
        };

        // ====== ใหม่: ผูกกับ destinationId & minimap ======
        record.destinationId = item.data.destinationId;

        if (!string.IsNullOrEmpty(record.destinationId))
        {
            var reg = GetDestinationRegistry();
            if (reg != null)
            {
                record.worldTarget = reg.GetPoint(record.destinationId);
                if (record.worldTarget != null && minimap != null)
                {
                    minimap.RegisterDeliveryTarget(record.worldTarget);
                    record.minimapRegistered = true;
                }
            }
        }
        // ==================================================

        activeBoxes.Add(record);

        Debug.Log($"[GameManager] Register box: {record.data.itemName} day={currentDay}");
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
            // ถ้าใช้ BoxKind.ColdBox ถือว่าเป็นกล่องเย็น
            usedColdBox = (rec.box.boxType == BoxKind.ColdBox);

            // ตรงนี้ใช้ flag จาก BoxCore ที่เราเพิ่ม
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

        // ✅ ใช้ effectiveLimit ในการคิดดีเลย์จริง ๆ
        int reward = rec.itemInstance.CalculateReward(dayCreated, dayDelivered, effectiveLimit);

        AddMoney(reward);
        activeBoxes.Remove(rec);
        // ==== ใหม่: ลบจาก minimap ถ้าเคยลงทะเบียนไว้ ====
        if (rec.minimapRegistered && minimap != null && rec.worldTarget != null)
        {
            minimap.UnregisterDeliveryTarget(rec.worldTarget);
            rec.minimapRegistered = false;
        }
        // ===================================================

    }


}
