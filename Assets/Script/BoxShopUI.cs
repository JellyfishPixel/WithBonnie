using StarterAssets;
using TMPro;
using UnityEngine;

public class BoxShopUI : MonoBehaviour
{
    [Header("Root Panel")]
    public GameObject rootPanel;      // Panel หลักของร้าน

    [Header("Price / unit")]
    public int priceS = 10;
    public int priceM = 15;
    public int priceL = 20;

    [Header("Qty Text")]
    public TMP_Text qtySText;
    public TMP_Text qtyMText;
    public TMP_Text qtyLText;

    [Header("Money Text")]
    public TMP_Text cashText;         // "CASH : 100$"

    [Header("Optional message")]
    public TMP_Text messageText;      // ไว้โชว์ "เงินไม่พอ" ฯลฯ

    // อ้างอิง Player ที่เปิดร้าน (จะใช้ enable/disable การขยับ)
    PlayerInteractionSystem currentPlayer;
    BoxShopTerminal currentTerminal;

    int qtyS, qtyM, qtyL;

    FirstPersonController fpc;

    public bool isOpen = false;
    public GameObject interactui;

    void Start()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
        isOpen = false;
    }

    public void Open(BoxShopTerminal terminal, PlayerInteractionSystem player)
    {
        // ถ้าร้านเปิดอยู่แล้ว ไม่ต้องทำอะไร (กัน ResetSelections ซ้ำ)
        if (isOpen) return;
        isOpen = true;
        interactui.SetActive(false);
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
        interactui.SetActive(true);
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
        qtyS = qtyM = qtyL = 0;
    }

    void RefreshUI()
    {
        if (qtySText) qtySText.text = qtyS.ToString();
        if (qtyMText) qtyMText.text = qtyM.ToString();
        if (qtyLText) qtyLText.text = qtyL.ToString();

        var eco = EconomyManager.Instance;
        if (eco && cashText)
        {
            // โชว์เงินรวมทั้งสองกระเป๋า
            cashText.text = $"CASH : {eco.TotalFunds}$";
        }

        if (messageText)
            messageText.text = string.Empty;
    }

    // ========= ปุ่ม + / - =========

    public void AddS(int delta) { qtyS = Mathf.Max(0, qtyS + delta); RefreshUI(); }
    public void AddM(int delta) { qtyM = Mathf.Max(0, qtyM + delta); RefreshUI(); }
    public void AddL(int delta) { qtyL = Mathf.Max(0, qtyL + delta); RefreshUI(); }

    // hook กับปุ่มใน Inspector:
    //  ปุ่ม S "-" => OnClick -> BoxShopUI.AddS -1
    //  ปุ่ม S "+" => OnClick -> BoxShopUI.AddS  1  เป็นต้น

    // ========= ปุ่ม Buy =========

    public void OnClickBuy()
    {
        var eco = EconomyManager.Instance;
        if (!eco) return;

        int totalCost =
            qtyS * priceS +
            qtyM * priceM +
            qtyL * priceL;

        if (totalCost <= 0)
        {
            if (messageText) messageText.text = "เลือกจำนวนกล่องก่อน";
            return;
        }

        if (!eco.CanAfford(totalCost))
        {
            if (messageText) messageText.text = "เงินไม่พอ";
            Debug.Log("[Shop] Not enough money");
            return;
        }

        // หักเงินรวม
        if (!eco.TrySpend(totalCost))
            return;

        // เพิ่มสต็อกกล่องตามจำนวนที่เลือก
        eco.AddBox(BoxSizeSimple.Small, qtyS);
        eco.AddBox(BoxSizeSimple.Medium, qtyM);
        eco.AddBox(BoxSizeSimple.Large, qtyL);

        ResetSelections();
        RefreshUI();

        if (messageText) messageText.text = "ซื้อสำเร็จ!";
    }

    // ========= ปุ่มปิด (กากบาท) =========

    public void OnClickClose()
    {
        Close();
    }
}
