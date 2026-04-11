using UnityEngine;

public class InventoryDetailUI : MonoBehaviour
{
    public BoxInventorySlotUI mainSlotUI;

    // ✅ รับ slot มาแสดงอย่างเดียว
    public void Refresh(BoxInventory.BoxSlot slot)
    {
        if (slot == null || !slot.hasBox)
        {
            mainSlotUI.Refresh(null, -1);
            return;
        }

        mainSlotUI.Refresh(slot, 0);
    }
}
