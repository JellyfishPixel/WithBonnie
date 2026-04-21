using UnityEngine;

public class HUDNearestSlotUI : MonoBehaviour
{
    public BoxInventorySlotUI slotUI;
    public void ShowHUDTemporarily()
    {
        gameObject.SetActive(true);
    }
    void Update()
    {
        var registry = FindFirstObjectByType<DestinationRegistry>();
        string currentScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        var bestSlot =
            BoxInventory.Instance.GetPreferredQuestSlot(currentScene, registry);

        if (bestSlot == null)
        {
            slotUI.gameObject.SetActive(false);
            return;
        }

        slotUI.gameObject.SetActive(true);
        slotUI.displayMode = BoxInventorySlotUI.InventorySlotDisplayMode.HUD_Small;
        slotUI.Refresh(bestSlot, 0);
    }
}
