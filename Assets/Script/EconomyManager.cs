using TMPro;
using UnityEngine;

public enum BoxSizeSimple
{
    Small,
    Medium,
    Large,
    ColdBox,
    WaterMedium,  // กล่องกันน้ำ M
    WaterLarge    // กล่องกันน้ำ L
}


public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Day")]
    [Tooltip("วันปัจจุบัน (ให้ซิงค์กับ GameManager.currentDay ถ้าใช้ร่วมกัน)")]
    public int currentDay = 1;

    [Header("Money (2 กระเป๋า)")]
    [Tooltip("เงินสดที่ได้จากการส่งของของวันปัจจุบัน (รีเซ็ตทุกวัน)")]
    public int cashToday = 0;

    [Tooltip("เงินกองกลาง/ธนาคาร ข้ามวัน ข้ามซีน ใช้ซื้อของ/อัปเกรด")]
    public int bankBalance = 0;

    [Tooltip("ดูอย่างเดียว: เงินทั้งหมดที่เอาไปซื้อของได้ตอนนี้ = cashToday + bankBalance")]
    public int TotalFunds => cashToday + bankBalance;
    [Header("Money Text")]
    public TMP_Text cashText;
    [Header("Box Stock")]
    public int boxStockS = 0;
    public int boxStockM = 0;
    public int boxStockL = 0;
    public int boxStockCold = 0;
    // 🆕 กล่องกันน้ำ
    public int boxStockWaterM = 0;
    public int boxStockWaterL = 0;
    // ========= PlayerPrefs Keys =========
    const string KEY_DAY = "Eco_Day";
    const string KEY_CASH = "Eco_CashToday";
    const string KEY_BANK = "Eco_Bank";
    const string KEY_BOX_S = "Eco_BoxS";
    const string KEY_BOX_M = "Eco_BoxM";
    const string KEY_BOX_L = "Eco_BoxL";
    const string KEY_BOX_Cold = "Eco_ColdBox";
    const string KEY_BOX_WM = "Eco_BoxWaterM";
    const string KEY_BOX_WL = "Eco_BoxWaterL";
    const string KEY_TAPE_RED = "Eco_TapeRedUses";
    const string KEY_TAPE_BLUE = "Eco_TapeBlueUses";
    const string KEY_TAPE_GREEN = "Eco_TapeGreenUses";
    const string KEY_BUBBLE_BASIC = "Eco_BubbleBasic";
    const string KEY_BUBBLE_STRONG = "Eco_BubbleStrong";
    const string KEY_BUBBLE_ICE = "Eco_BubbleIce";



    [SerializeField] bool loadFromSaveOnStart = false;
    [Header("Tape Stock (uses)")]
    [Tooltip("จำนวนครั้งที่ยังใช้ได้ของเทปสีแดง (1 ม้วน = 10 ครั้ง)")]
    public int tapeUsesRed = 0;

    [Tooltip("จำนวนครั้งที่ยังใช้ได้ของเทปสีน้ำเงิน")]
    public int tapeUsesBlue = 0;

    [Tooltip("จำนวนครั้งที่ยังใช้ได้ของเทปสีเขียว")]
    public int tapeUsesGreen = 0;
    [Header("Bubble Stock")]
    [Tooltip("จำนวนบับเบิลธรรมดา (Basic) ที่มีในสต็อก")]
    public int bubbleStockBasic = 0;

    [Tooltip("จำนวนบับเบิลกันแรง (Strong) ที่มีในสต็อก")]
    public int bubbleStockStrong = 0;

    [Tooltip("จำนวนบับเบิลน้ำแข็ง (Ice) ที่มีในสต็อก")]
    public int bubbleStockIce = 0;
    [Header("Bubble Uses (1 purchase = 3 uses)")]
    public int bubbleUsesBasic = 0;
    public int bubbleUsesStrong = 0;
    public int bubbleUsesIce = 0;

    // 1 ชิ้นจากร้าน = กดได้กี่ครั้ง
    public const int BUBBLE_USES_PER_PURCHASE = 3;
    public const int TAPE_USES_PER_ROLL = 10;

    [Header("TEST MODE")]
    public bool testSessionNoPrefs = true;  // ✅ เปิดไว้ตอนเทส

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!testSessionNoPrefs)
            LoadFromPrefs();

        UpdateMoneyUI();
    }


    void OnApplicationQuit()
    {
        if (!testSessionNoPrefs)
            SaveToPrefs();
    }

    void OnApplicationPause(bool pause)
    {
        if (!testSessionNoPrefs && pause)
            SaveToPrefs();
    }

    void OnApplicationFocus(bool focus)
    {
        if (!testSessionNoPrefs && !focus)
            SaveToPrefs();
    }
    void SaveIfAllowed()
    {
        if (!testSessionNoPrefs)
            SaveToPrefs();
    }

    // ========== API เงิน ==========

    public void AddCashToday(int amount)
    {
        cashToday += amount;
        if (cashToday < 0) cashToday = 0;

        SaveToPrefs();
        UpdateMoneyUI();   // ✅
        Debug.Log($"[Eco] AddCashToday {amount} => cashToday={cashToday}, bank={bankBalance}");
    }


    public bool CanAfford(int price)
    {
        return TotalFunds >= price;
    }


    public bool TrySpend(int price)
    {
        if (!CanAfford(price))
            return false;

        int remaining = price;

        // หักจาก bank ก่อน
        if (bankBalance >= remaining)
        {
            bankBalance -= remaining;
            remaining = 0;
        }
        else
        {
            remaining -= bankBalance;
            bankBalance = 0;
        }

        // ถ้ายังเหลือให้หักจาก cashToday
        if (remaining > 0)
        {
            cashToday -= remaining;
            if (cashToday < 0) cashToday = 0; // กันพลาด
        }

        SaveIfAllowed();
        UpdateMoneyUI();   // ✅
        Debug.Log($"[Eco] Spend {price} => cashToday={cashToday}, bank={bankBalance}");

        return true;
    }

    /// <summary>
    /// จบวัน: โอน cashToday เข้าธนาคาร แล้วรีเซ็ตเงินสด
    /// (ให้ไปเรียกจากระบบนอน/เข้านอน/จบวัน)
    /// </summary>
    public void EndDayAndDeposit()
    {
        bankBalance += cashToday;
        cashToday = 0;
        currentDay++;

        SaveIfAllowed();
        UpdateMoneyUI();   // ✅
        Debug.Log($"[Eco] EndDay => Day={currentDay}, bank={bankBalance}, cashToday={cashToday}");

    }

    // ========== UI MONEY ==========

    void UpdateMoneyUI()
    {
        if (cashText != null)
        {
            cashText.text = $"CASH : {TotalFunds}$";
        }
    }


    // ========== API สต็อกกล่อง ==========

    public bool HasBox(BoxSizeSimple size)
    {
        return size switch
        {
            BoxSizeSimple.Small => boxStockS > 0,
            BoxSizeSimple.Medium => boxStockM > 0,
            BoxSizeSimple.Large => boxStockL > 0,
            BoxSizeSimple.ColdBox => boxStockCold > 0,
            BoxSizeSimple.WaterMedium => boxStockWaterM > 0,
            BoxSizeSimple.WaterLarge => boxStockWaterL > 0,
            _ => false
        };
    }
    public int GetBoxStock(BoxSizeSimple size)
    {
        return size switch
        {
            BoxSizeSimple.Small => boxStockS,
            BoxSizeSimple.Medium => boxStockM,
            BoxSizeSimple.Large => boxStockL,
            BoxSizeSimple.ColdBox => boxStockCold,
            BoxSizeSimple.WaterMedium => boxStockWaterM,
            BoxSizeSimple.WaterLarge => boxStockWaterL,
            _ => 0
        };
    }

    public bool TryConsumeBox(BoxSizeSimple size)
    {
        if (!HasBox(size)) return false;

        switch (size)
        {
            case BoxSizeSimple.Small: boxStockS--; break;
            case BoxSizeSimple.Medium: boxStockM--; break;
            case BoxSizeSimple.Large: boxStockL--; break;
            case BoxSizeSimple.ColdBox: boxStockCold--; break;
            case BoxSizeSimple.WaterMedium: boxStockWaterM--; break;
            case BoxSizeSimple.WaterLarge: boxStockWaterL--; break;
        }

        SaveIfAllowed();
        return true;
    }

    public void AddBox(BoxSizeSimple size, int amount)
    {
        if (amount <= 0) return;

        switch (size)
        {
            case BoxSizeSimple.Small: boxStockS += amount; break;
            case BoxSizeSimple.Medium: boxStockM += amount; break;
            case BoxSizeSimple.Large: boxStockL += amount; break;
            case BoxSizeSimple.ColdBox: boxStockCold += amount; break;
            case BoxSizeSimple.WaterMedium: boxStockWaterM += amount; break;
            case BoxSizeSimple.WaterLarge: boxStockWaterL += amount; break;
        }

        SaveIfAllowed();
        UpdateMoneyUI();

    }


    int GetTapeUses(TapeColor color)
    {
        return color switch
        {
            TapeColor.Red => tapeUsesRed,
            TapeColor.Blue => tapeUsesBlue,
            TapeColor.Green => tapeUsesGreen,
            _ => 0
        };
    }

    void SetTapeUses(TapeColor color, int uses)
    {
        uses = Mathf.Max(0, uses);
        switch (color)
        {
            case TapeColor.Red: tapeUsesRed = uses; break;
            case TapeColor.Blue: tapeUsesBlue = uses; break;
            case TapeColor.Green: tapeUsesGreen = uses; break;
        }
    }

    public bool HasTapeUse(TapeColor color)
    {
        return GetTapeUses(color) > 0;
    }

    public int GetTapeRolls(TapeColor color)
    {
        // เผื่ออยากโชว์ใน UI ว่ามีกี่ "ม้วน" (ใช้ floor)
        return GetTapeUses(color) / TAPE_USES_PER_ROLL;
    }

    /// <summary>
    /// ซื้อเทปเป็น "ม้วน" (1 ม้วน = 10 ครั้ง)
    /// </summary>
    public void AddTapeRoll(TapeColor color, int rolls)
    {
        if (rolls <= 0) return;

        int addUses = rolls * TAPE_USES_PER_ROLL;
        int current = GetTapeUses(color);
        SetTapeUses(color, current + addUses);

        SaveIfAllowed();
        UpdateMoneyUI();
    }

    /// <summary>
    /// ใช้เทปไป 1 ครั้ง (ถ้าไม่มีแล้วจะคืน false)
    /// </summary>
    public bool TryConsumeTapeUse(TapeColor color)
    {
        int current = GetTapeUses(color);
        if (current <= 0) return false;

        SetTapeUses(color, current - 1);

        SaveIfAllowed();
        // ไม่จำเป็นต้อง UpdateMoneyUI เพราะเงินไม่เกี่ยว แต่จะเรียกก็ได้
        return true;
    }
    // ========== API สต็อกบับเบิล ==========
    int GetBubbleUses(BubbleType type)
    {
        return type switch
        {
            BubbleType.Basic => bubbleUsesBasic,
            BubbleType.Strong => bubbleUsesStrong,
            BubbleType.Ice => bubbleUsesIce,
            _ => 0
        };
    }

    void SetBubbleUses(BubbleType type, int uses)
    {
        uses = Mathf.Max(0, uses);

        switch (type)
        {
            case BubbleType.Basic: bubbleUsesBasic = uses; break;
            case BubbleType.Strong: bubbleUsesStrong = uses; break;
            case BubbleType.Ice: bubbleUsesIce = uses; break;
        }
    }

    /// <summary>ซื้อบับเบิลจากร้าน (1 หน่วย = 3 uses)</summary>
    public void AddBubble(BubbleType type, int amount)
    {
        if (amount <= 0) return;

        int addUses = amount * BUBBLE_USES_PER_PURCHASE;
        int current = GetBubbleUses(type);

        SetBubbleUses(type, current + addUses);
        SaveIfAllowed();
    }

    /// <summary>เช็คว่ายังเหลือ uses ไหม</summary>
    public bool HasBubbleStock(BubbleType type)
    {
        return GetBubbleUses(type) > 0;
    }

    /// <summary>ใช้บับเบิล 1 ครั้ง</summary>
    public bool TryConsumeBubble(BubbleType type)
    {
        int current = GetBubbleUses(type);
        if (current <= 0) return false;

        SetBubbleUses(type, current - 1);
        SaveIfAllowed();
        return true;
    }


    public void SaveToPrefs()
    {
        PlayerPrefs.SetInt(KEY_DAY, currentDay);
        PlayerPrefs.SetInt(KEY_CASH, cashToday);
        PlayerPrefs.SetInt(KEY_BANK, bankBalance);
        PlayerPrefs.SetInt(KEY_BOX_S, boxStockS);
        PlayerPrefs.SetInt(KEY_BOX_M, boxStockM);
        PlayerPrefs.SetInt(KEY_BOX_L, boxStockL);
        PlayerPrefs.SetInt(KEY_BOX_Cold, boxStockCold);
        PlayerPrefs.SetInt(KEY_TAPE_RED, tapeUsesRed);
        PlayerPrefs.SetInt(KEY_TAPE_BLUE, tapeUsesBlue);
        PlayerPrefs.SetInt(KEY_TAPE_GREEN, tapeUsesGreen);
        PlayerPrefs.SetInt(KEY_BOX_WM, boxStockWaterM);
        PlayerPrefs.SetInt(KEY_BOX_WL, boxStockWaterL);

        // 🔹 เซฟ "จำนวนครั้งที่ใช้ได้" แทน stock เดิม
        PlayerPrefs.SetInt(KEY_BUBBLE_BASIC, bubbleUsesBasic);
        PlayerPrefs.SetInt(KEY_BUBBLE_STRONG, bubbleUsesStrong);
        PlayerPrefs.SetInt(KEY_BUBBLE_ICE, bubbleUsesIce);

        PlayerPrefs.Save();
    }

    public void LoadFromPrefs()
    {
        currentDay = PlayerPrefs.GetInt(KEY_DAY, 1);
        cashToday = PlayerPrefs.GetInt(KEY_CASH, 0);
        bankBalance = PlayerPrefs.GetInt(KEY_BANK, 0);
        boxStockS = PlayerPrefs.GetInt(KEY_BOX_S, 0);
        boxStockM = PlayerPrefs.GetInt(KEY_BOX_M, 0);
        boxStockL = PlayerPrefs.GetInt(KEY_BOX_L, 0);
        boxStockCold = PlayerPrefs.GetInt(KEY_BOX_Cold, 0);
        tapeUsesRed = PlayerPrefs.GetInt(KEY_TAPE_RED, 0);
        tapeUsesBlue = PlayerPrefs.GetInt(KEY_TAPE_BLUE, 0);
        tapeUsesGreen = PlayerPrefs.GetInt(KEY_TAPE_GREEN, 0);
        boxStockWaterM = PlayerPrefs.GetInt(KEY_BOX_WM, 0);
        boxStockWaterL = PlayerPrefs.GetInt(KEY_BOX_WL, 0);

        // 🔹 โหลดกลับเข้า uses
        bubbleUsesBasic = PlayerPrefs.GetInt(KEY_BUBBLE_BASIC, 0);
        bubbleUsesStrong = PlayerPrefs.GetInt(KEY_BUBBLE_STRONG, 0);
        bubbleUsesIce = PlayerPrefs.GetInt(KEY_BUBBLE_ICE, 0);

        Debug.Log($"[Eco] Load => Day={currentDay}, cashToday={cashToday}, bank={bankBalance}, S={boxStockS}, M={boxStockM}, L={boxStockL}");
        UpdateMoneyUI();
    }

}
