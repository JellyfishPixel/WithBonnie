using UnityEngine;
using UnityEngine.UI;

public class InventoryBottomSlotUI : MonoBehaviour
{
    public Image icon;
    public Button button;
    public Button pinButton;
    public GameObject pinnedIndicator;
    public Image pinIcon;
    public Color pinActiveColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color pinInactiveColor = Color.white;
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
            UpdatePinVisual(false, false);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // ✅ สำคัญมาก: ให้เลือก slot ว่าง
                controller.SelectSlot(slotIndex);
            });

            if (pinButton != null)
            {
                pinButton.onClick.RemoveAllListeners();
                pinButton.interactable = false;
            }

            return;
        }

        // ---------- FILLED ----------
        icon.sprite = slot.itemData.icon;
        UpdatePinVisual(true, BoxInventory.Instance != null && BoxInventory.Instance.IsSlotPinned(index));

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            controller.SelectSlot(slotIndex);
        });

        if (pinButton != null)
        {
            pinButton.onClick.RemoveAllListeners();
            pinButton.interactable = true;
            pinButton.onClick.AddListener(() =>
            {
                if (BoxInventory.Instance == null)
                    return;

                BoxInventory.Instance.TogglePinSlot(slotIndex);
                controller.RefreshAll();
                controller.SelectSlot(slotIndex);
            });
        }
    }

    void UpdatePinVisual(bool canPin, bool isPinned)
    {
        if (pinnedIndicator != null)
            pinnedIndicator.SetActive(canPin && isPinned);

        if (pinButton != null)
            pinButton.gameObject.SetActive(canPin);

        if (pinIcon != null)
            pinIcon.color = isPinned ? pinActiveColor : pinInactiveColor;
    }
}
