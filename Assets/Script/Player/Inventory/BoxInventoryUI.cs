using StarterAssets;
using UnityEngine;

public class BoxInventoryUI : MonoBehaviour
{
    [Header("Root Panel")]
    public GameObject panel;
    StarterAssetsInputs starterInputs;
    PlayerMovementLocker movementLocker;
    bool isOpen;

    [Header("Slot UIs (?????? 3 ????)")]
    public BoxInventorySlotUI[] slotUIs;
    GameObject currentPage;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.Alpha1;
    [Header("Main Pages")]
    public GameObject pageInventory;
    public GameObject pageStock;



    void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    void OnDisable()
    {
        UILockManager.Release(this);
        isOpen = false;
    }

    void Update()
    {

        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen)
                CloseMenu();
            else
                OpenMenu();
        }

    }

    public void OpenMenu()
    {
        if (isOpen) return;
        isOpen = true;

        if (panel) panel.SetActive(true);


        UILockManager.Instance.PushLock(this, UILockOptions.Menu);
        RefreshVisibleSlots();
    }

    public void RefreshVisibleSlots()
    {

        var inv = BoxInventory.Instance;
        if (inv == null || slotUIs == null) return;

        int uiIndex = 0;


        for (int slotIndex = 0; slotIndex < inv.SlotCount; slotIndex++)
        {
            var slot = inv.GetSlot(slotIndex);
            if (slot == null || !slot.hasBox)
                continue;

            if (uiIndex >= slotUIs.Length)
                break;

            var ui = slotUIs[uiIndex];
            if (ui == null) continue;

            ui.gameObject.SetActive(true);
            ui.Refresh(slot, slotIndex);

            uiIndex++;
        }

        for (int i = uiIndex; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].gameObject.SetActive(false);
        }
    }


    public void CloseMenu()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panel) panel.SetActive(false);

        UILockManager.Instance.PopLock(this);

    }
    void ShowPage(GameObject target)
    {
        if (currentPage == target) return;

        if (pageInventory) pageInventory.SetActive(false);
        if (pageStock) pageStock.SetActive(false);

        if (target)
        {
            target.SetActive(true);
            currentPage = target;
        }
    }

    public void OpenInventoryPage()
    {

        ShowPage(pageInventory);

    }

    public void OpenStockPage()
    {

        ShowPage(pageStock);
    }

}


