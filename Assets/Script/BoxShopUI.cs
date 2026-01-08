using StarterAssets;
using TMPro;
using UnityEngine;

public class BoxShopUI : MonoBehaviour
{
    [Header("Root Panel")]
    public GameObject rootPanel;      // Panel หลักของร้าน
    [Header("Name Labels (show owned stock)")]
    public TMP_Text nameS;
    public TMP_Text nameM;
    public TMP_Text nameL;
    public TMP_Text nameC;
    public TMP_Text nameWaterM;
    public TMP_Text nameWaterL;

    public TMP_Text nameBubbleBasic;
    public TMP_Text nameBubbleStrong;
    public TMP_Text nameBubbleIce;

    public TMP_Text nameTapeRed;
    public TMP_Text nameTapeBlue;
    public TMP_Text nameTapeGreen;

    [Header("Price / unit")]
    public int priceS = 10;
    public int priceM = 15;
    public int priceL = 20;
    public int priceC = 25;
    public int priceWaterM = 25;
    public int priceWaterL = 30;
    [Header("Qty Text")]
    public TMP_Text qtySText;
    public TMP_Text qtyMText;
    public TMP_Text qtyLText;
    public TMP_Text qtyCText;
    public TMP_Text qtyWaterMText;
    public TMP_Text qtyWaterLText;

    [Header("Money Text")]
    public TMP_Text cashText;         // "CASH : 100$"

    [Header("Optional message")]
    public TMP_Text messageText;      // ไว้โชว์ "เงินไม่พอ" ฯลฯ

    // อ้างอิง Player ที่เปิดร้าน (จะใช้ enable/disable การขยับ)
    PlayerInteractionSystem currentPlayer;
    BoxShopTerminal currentTerminal;

    int qtyS, qtyM, qtyL, qtyC, qtyWaterM, qtyWaterL;

    FirstPersonController fpc;

    [Header("Bubble Price / unit")]
    public int priceBubbleBasic = 5;
    public int priceBubbleStrong = 10;
    public int priceBubbleIce = 15;

    [Header("Bubble Qty Text")]
    public TMP_Text qtyBubbleBasicText;
    public TMP_Text qtyBubbleStrongText;
    public TMP_Text qtyBubbleIceText;

    // ตัวแปรนับจำนวนที่ “เลือกซื้อรอบนี้”
    int qtyBubbleBasic, qtyBubbleStrong, qtyBubbleIce;

    [Header("Tape Price / roll")]
    public int priceTapeRed = 5;
    public int priceTapeBlue = 5;
    public int priceTapeGreen = 5;

    [Header("Tape Qty (rolls)")]
    public TMP_Text qtyTapeRedText;
    public TMP_Text qtyTapeBlueText;
    public TMP_Text qtyTapeGreenText;

    int qtyTapeRed, qtyTapeBlue, qtyTapeGreen;

    [Header("Total Cost Text")]
    public TMP_Text totalCostText; 



    public bool isOpen = false;
    public interactUI interactui;

    void Start()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
        isOpen = false;
        interactui = FindFirstObjectByType<interactUI>();
        
    }
    int CalculateCurrentTotalCost()
    {
        int totalCost =
            qtyS * priceS +
            qtyM * priceM +
            qtyL * priceL +
            qtyC * priceC +
            qtyWaterM * priceWaterM +
            qtyWaterL * priceWaterL +
            qtyBubbleBasic * priceBubbleBasic +
            qtyBubbleStrong * priceBubbleStrong +
            qtyBubbleIce * priceBubbleIce +

            qtyTapeRed * priceTapeRed +
            qtyTapeBlue * priceTapeBlue +
            qtyTapeGreen * priceTapeGreen;

        return totalCost;
    }

    public void Open(BoxShopTerminal terminal, PlayerInteractionSystem player)
    {
        // ถ้าร้านเปิดอยู่แล้ว ไม่ต้องทำอะไร (กัน ResetSelections ซ้ำ)
        if (isOpen) return;
        isOpen = true;
        interactui.gameObject.SetActive(false);
        currentTerminal = terminal;
        currentPlayer = player;

        fpc = currentPlayer.GetComponent<FirstPersonController>();
        if (fpc != null)
            fpc.LockMovement();

        if (rootPanel != null)
            rootPanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetSelections();
        RefreshUI();
    }

    public void Close()
    {
        if (!isOpen) return;    // ปิดซ้ำก็ไม่ทำอะไร
        isOpen = false;
        interactui.gameObject.SetActive(true);
        if (rootPanel != null)
            rootPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (fpc != null)
            fpc.UnlockMovement();

        if (currentTerminal != null)
            currentTerminal.NotifyShopClosed();

        currentPlayer = null;
        currentTerminal = null;
        fpc = null;
    }

    void ResetSelections()
    {
        qtyS = qtyM = qtyL = qtyC = 0;
        qtyWaterM = qtyWaterL = 0;

        qtyBubbleBasic = qtyBubbleStrong = qtyBubbleIce = 0;
        qtyTapeRed = qtyTapeBlue = qtyTapeGreen = 0;
    }
    void OnEnable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnStockChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnStockChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (qtySText) qtySText.text = qtyS.ToString();
        if (qtyMText) qtyMText.text = qtyM.ToString();
        if (qtyLText) qtyLText.text = qtyL.ToString();
        if (qtyCText) qtyCText.text = qtyC.ToString();
        if (qtyWaterMText) qtyWaterMText.text = qtyWaterM.ToString();
        if (qtyWaterLText) qtyWaterLText.text = qtyWaterL.ToString();

        if (qtyBubbleBasicText) qtyBubbleBasicText.text = qtyBubbleBasic.ToString();
        if (qtyBubbleStrongText) qtyBubbleStrongText.text = qtyBubbleStrong.ToString();
        if (qtyBubbleIceText) qtyBubbleIceText.text = qtyBubbleIce.ToString();

        if (qtyTapeRedText) qtyTapeRedText.text = qtyTapeRed.ToString();
        if (qtyTapeBlueText) qtyTapeBlueText.text = qtyTapeBlue.ToString();
        if (qtyTapeGreenText) qtyTapeGreenText.text = qtyTapeGreen.ToString();
        var eco = EconomyManager.Instance;

        if (eco != null)
        {
            // ===== Boxes =====
            if (nameS) nameS.text = WithStock("Small Box", eco.boxStockS);
            if (nameM) nameM.text = WithStock("Medium Box", eco.boxStockM);
            if (nameL) nameL.text = WithStock("Large Box", eco.boxStockL);
            if (nameC) nameC.text = WithStock("Cold Box", eco.boxStockCold);
            if (nameWaterM) nameWaterM.text = WithStock("Waterproof M", eco.boxStockWaterM);
            if (nameWaterL) nameWaterL.text = WithStock("Waterproof L", eco.boxStockWaterL);

            // ===== Bubble (ถ้าสต๊อกจริงของเธอใช้เป็น uses ให้โชว์ uses) =====
            if (nameBubbleBasic) nameBubbleBasic.text = WithStock("Bubble Basic", eco.bubbleUsesBasic);
            if (nameBubbleStrong) nameBubbleStrong.text = WithStock("Bubble Strong", eco.bubbleUsesStrong);
            if (nameBubbleIce) nameBubbleIce.text = WithStock("Bubble Ice", eco.bubbleUsesIce);

            // ===== Tape (เทปของเธอเก็บเป็น uses) =====
            if (nameTapeRed) nameTapeRed.text = WithStock("Tape Red", eco.tapeUsesRed);
            if (nameTapeBlue) nameTapeBlue.text = WithStock("Tape Blue", eco.tapeUsesBlue);
            if (nameTapeGreen) nameTapeGreen.text = WithStock("Tape Green", eco.tapeUsesGreen);
        }
        if (eco && cashText)
            cashText.text = $"CASH : {eco.TotalFunds}$";

        // 🔹 อัปเดตยอดรวมแบบเรียลไทม์
        int total = CalculateCurrentTotalCost();
        if (totalCostText)
        {
            totalCostText.text = $"TOTAL : {total}$";

            // ถ้าอยากให้เปลี่ยนสีเวลาเงินไม่พอ:
            if (eco != null && !eco.CanAfford(total) && total > 0)
                totalCostText.color = Color.red;
            else
                totalCostText.color = Color.black;
        }

        if (messageText)
            messageText.text = string.Empty;
    }



    // ========= ปุ่ม + / - =========
    public void AddS(int delta) { qtyS = Mathf.Max(0, qtyS + delta); RefreshUI(); }
    public void AddM(int delta) { qtyM = Mathf.Max(0, qtyM + delta); RefreshUI(); }
    public void AddL(int delta) { qtyL = Mathf.Max(0, qtyL + delta); RefreshUI(); }

    public void AddC(int delta) { qtyC = Mathf.Max(0, qtyC + delta); RefreshUI(); }
    public void AddWaterM(int delta) { qtyWaterM = Mathf.Max(0, qtyWaterM + delta); RefreshUI(); }
    public void AddWaterL(int delta){ qtyWaterL = Mathf.Max(0, qtyWaterL + delta); RefreshUI(); }

    public void AddBubbleBasic(int delta) { qtyBubbleBasic = Mathf.Max(0, qtyBubbleBasic + delta); RefreshUI(); }
    public void AddBubbleStrong(int delta) { qtyBubbleStrong = Mathf.Max(0, qtyBubbleStrong + delta); RefreshUI(); }
    public void AddBubbleIce(int delta) { qtyBubbleIce = Mathf.Max(0, qtyBubbleIce + delta); RefreshUI(); }

    public void AddTapeRed(int delta) { qtyTapeRed = Mathf.Max(0, qtyTapeRed + delta); RefreshUI(); }
    public void AddTapeBlue(int delta) { qtyTapeBlue = Mathf.Max(0, qtyTapeBlue + delta); RefreshUI(); }
    public void AddTapeGreen(int delta) { qtyTapeGreen = Mathf.Max(0, qtyTapeGreen + delta); RefreshUI(); }


    string WithStock(string baseName, int stock)
    {
        return $"{baseName} (x{Mathf.Max(0, stock)})";
    }

    public void OnClickBuy()
    {
        var eco = EconomyManager.Instance;
        if (!eco) return;

        int totalCost = CalculateCurrentTotalCost();

        if (totalCost <= 0)
        {
            if (messageText) messageText.text = "เลือกของที่จะซื้อก่อน";
            return;
        }

        if (!eco.CanAfford(totalCost))
        {
            if (messageText) messageText.text = "เงินไม่พอ";
            Debug.Log("[Shop] Not enough money");
            return;
        }

        if (!eco.TrySpend(totalCost))
            return;

 

        // กล่อง
        eco.AddBox(BoxSizeSimple.Small, qtyS);
        eco.AddBox(BoxSizeSimple.Medium, qtyM);
        eco.AddBox(BoxSizeSimple.Large, qtyL);
        eco.AddBox(BoxSizeSimple.ColdBox, qtyC);
        eco.AddBox(BoxSizeSimple.WaterMedium, qtyWaterM);
        eco.AddBox(BoxSizeSimple.WaterLarge, qtyWaterL);
        // บับเบิล (ต้องไปเพิ่ม method เหล่านี้ใน EconomyManager เอง)
        eco.AddBubble(BubbleType.Basic, qtyBubbleBasic);
        eco.AddBubble(BubbleType.Strong, qtyBubbleStrong);
        eco.AddBubble(BubbleType.Ice, qtyBubbleIce);

        eco.AddTapeRoll(TapeColor.Red, qtyTapeRed);
        eco.AddTapeRoll(TapeColor.Blue, qtyTapeBlue);
        eco.AddTapeRoll(TapeColor.Green, qtyTapeGreen);

        ResetSelections();
        RefreshUI();

        if (messageText) messageText.text = "ซื้อสำเร็จ!";
    }


    public void OnClickClose()
    {
        Close();
    }
}
