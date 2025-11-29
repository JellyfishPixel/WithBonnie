using TMPro;
using UnityEngine;

public enum BoxSizeSimple { Small, Medium, Large }

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

    // ========= PlayerPrefs Keys =========
    const string KEY_DAY = "Eco_Day";
    const string KEY_CASH = "Eco_CashToday";
    const string KEY_BANK = "Eco_Bank";
    const string KEY_BOX_S = "Eco_BoxS";
    const string KEY_BOX_M = "Eco_BoxM";
    const string KEY_BOX_L = "Eco_BoxL";

    [SerializeField] bool loadFromSaveOnStart = false;   // เปิดใน Inspector ได้

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadFromSaveOnStart)
        {
            LoadFromPrefs();
        }
        UpdateMoneyUI();
    }


    void OnApplicationQuit()
    {
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

        SaveToPrefs();
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

        SaveToPrefs();
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
            _ => false
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
        }

        SaveToPrefs();
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
        }

        SaveToPrefs();
        UpdateMoneyUI();
    }

    // ========== Save / Load ==========

    public void SaveToPrefs()
    {
        PlayerPrefs.SetInt(KEY_DAY, currentDay);
        PlayerPrefs.SetInt(KEY_CASH, cashToday);
        PlayerPrefs.SetInt(KEY_BANK, bankBalance);
        PlayerPrefs.SetInt(KEY_BOX_S, boxStockS);
        PlayerPrefs.SetInt(KEY_BOX_M, boxStockM);
        PlayerPrefs.SetInt(KEY_BOX_L, boxStockL);
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

        Debug.Log($"[Eco] Load => Day={currentDay}, cashToday={cashToday}, bank={bankBalance}, S={boxStockS}, M={boxStockM}, L={boxStockL}");
        UpdateMoneyUI();
    }
}
