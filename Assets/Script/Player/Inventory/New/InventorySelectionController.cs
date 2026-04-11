using UnityEngine;

public class InventorySelectionController : MonoBehaviour
{
    public static InventorySelectionController Instance;
    public BoxInventorySlotUI detailUI;
    public InventoryBottomSlotUI[] bottomSlots;

    int currentSlotIndex = -1;

    void OnEnable()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        var inv = BoxInventory.Instance;
        if (inv == null) return;

        // ---- bottom slots ----
        for (int i = 0; i < bottomSlots.Length; i++)
        {
            bottomSlots[i].BindSlot(i, this);
        }

        // ---- default detail ----
        var nearest = inv.GetNearestSlot();
        if (nearest != null)
            ShowDetail(nearest);
        else
            detailUI.Refresh(null, -1);
    }

    public void SelectSlot(int index)
    {
        currentSlotIndex = index;

        var slot = BoxInventory.Instance.GetSlot(index);


        if (slot == null || !slot.hasBox || slot.itemData == null)
        {
            detailUI.displayMode =
                BoxInventorySlotUI.InventorySlotDisplayMode.Inventory_Detail;


            detailUI.Refresh(null, -1);
            return;
        }

        ShowDetail(slot);
    }

    void ShowDetail(BoxInventory.BoxSlot slot)
    {
        detailUI.displayMode =
            BoxInventorySlotUI.InventorySlotDisplayMode.Inventory_Detail;

        detailUI.Refresh(slot, currentSlotIndex);
    }
}
