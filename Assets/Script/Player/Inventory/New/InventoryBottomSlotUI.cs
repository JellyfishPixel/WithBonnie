using UnityEngine;
using UnityEngine.UI;

public class InventoryBottomSlotUI : MonoBehaviour
{
    public Image icon;
    public Button button;
    public Sprite emptyIcon;

    int slotIndex;
    InventorySelectionController controller;

    public void BindSlot(int index, InventorySelectionController c)
    {
        slotIndex = index;
        controller = c;

        var slot = BoxInventory.Instance.GetSlot(index);

        // ---------- EMPTY ----------
        if (slot == null || !slot.hasBox || slot.itemData == null)
        {
            icon.sprite = emptyIcon;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // ✅ สำคัญมาก: ให้เลือก slot ว่าง
                controller.SelectSlot(slotIndex);
            });

            return;
        }

        // ---------- FILLED ----------
        icon.sprite = slot.itemData.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            controller.SelectSlot(slotIndex);
        });
    }

}
