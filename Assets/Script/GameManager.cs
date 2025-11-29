using System.Collections.Generic;
using UnityEngine;
using TMPro;   // ✅ เพิ่มสำหรับ TextMeshPro

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

    [System.Serializable]
    public class DeliveryRecord
    {
        public BoxCore box;
        public DeliveryItemInstance itemInstance;
        public DeliveryItemData data;
        public int dayCreated;
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

    // ================== TIME SYSTEM ==================

    void UpdateGameTime()
    {
        timeAcc += Time.deltaTime;

        // 1 ชั่วโมงในเกม ใช้ realSecondsPerGameHour วินาทีจริง
        // => 1 นาทีในเกม = realSecondsPerGameHour / 60
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
                currentDay++;

                Debug.Log($"[GameManager] New Day: {currentDay}");
                SyncDayToEconomy();    // ✅ ให้ EconomyManager รู้ว่าวันที่เปลี่ยน
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

    public void RegisterNewDelivery(BoxCore box, DeliveryItemInstance item)
    {
        if (!box || !item || !item.data) return;

        // ถ้าเต็ม 3 กล่องแล้ว ให้คุณตัดสินใจเองว่าจะทำอย่างไร
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
        activeBoxes.Add(record);

        Debug.Log($"[GameManager] Register box: {record.data.itemName} day={currentDay}");
    }

    public void CompleteDelivery(BoxCore box)
    {
        DeliveryRecord rec = null;
        foreach (var r in activeBoxes)
        {
            if (r.box == box)
            {
                rec = r;
                break;
            }
        }
        if (rec == null) return;

        bool usedColdBox = false;
        if (rec.box != null)
        {
            // ถ้าใช้ BoxKind.ColdBox ถือว่าเป็นกล่องเย็น
            usedColdBox = (rec.box.boxType == BoxKind.ColdBox);
        }

        int baseLimit = rec.data.deliveryLimitDays;
        int effectiveLimit = rec.itemInstance.CalculateEffectiveDeadlineDays(baseLimit, usedColdBox);

        int dayCreated = rec.dayCreated;
        int dayDelivered = GameManager.Instance.currentDay;

        // แทนที่จะใช้ data.deliveryLimitDays ตรง ๆ
        // คุณอาจจะเขียน CalculateReward ใหม่ให้รับ effectiveLimit เข้าไป
        int reward = rec.itemInstance.CalculateReward(dayCreated, dayDelivered /*, effectiveLimit*/);

        AddMoney(reward);
        activeBoxes.Remove(rec);
    }

}
