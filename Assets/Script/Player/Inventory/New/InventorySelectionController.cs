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
        var registry = FindFirstObjectByType<DestinationRegistry>();
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var preferred = inv.GetPreferredQuestSlot(currentScene, registry);
        if (preferred != null)
        {
            int preferredIndex = inv.PinnedSlotIndex >= 0
                ? inv.PinnedSlotIndex
                : FindSlotIndex(preferred);

            currentSlotIndex = preferredIndex;
            ShowDetail(preferred);
        }
        else
        {
            currentSlotIndex = -1;
            detailUI.Refresh(null, -1);
        }
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

    int FindSlotIndex(BoxInventory.BoxSlot target)
    {
        var inv = BoxInventory.Instance;
        if (inv == null || target == null)
            return -1;

        for (int i = 0; i < inv.SlotCount; i++)
        {
            if (inv.GetSlot(i) == target)
                return i;
        }

        return -1;
    }
}
