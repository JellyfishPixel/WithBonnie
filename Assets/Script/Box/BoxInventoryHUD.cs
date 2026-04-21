using UnityEngine;

public class BoxInventoryHUD : MonoBehaviour
{
    [Header("UI")]
    public BoxInventorySlotUI hudSlot;
    void Start()
    {
        RefreshHUD();
    }
    void Update()
    {
        if (!BoxInventory.Instance || hudSlot == null)
            return;

        var registry = FindFirstObjectByType<DestinationRegistry>();
        string currentScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        var slot = BoxInventory.Instance
            .GetPreferredQuestSlot(currentScene, registry);

        if (slot == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        hudSlot.displayMode =
            BoxInventorySlotUI.InventorySlotDisplayMode.HUD_Small;

        hudSlot.Refresh(slot, -1);
    }
    public void RefreshHUD()
    {
        if (!BoxInventory.Instance || hudSlot == null)
            return;

        if (!BoxInventory.Instance.HasAnyBox())
        {
            gameObject.SetActive(false);
            return;
        }

        var registry = FindFirstObjectByType<DestinationRegistry>();
        string currentScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        var slot = BoxInventory.Instance
            .GetPreferredQuestSlot(currentScene, registry);

        if (slot == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        hudSlot.displayMode =
            BoxInventorySlotUI.InventorySlotDisplayMode.HUD_Small;

        hudSlot.Refresh(slot, -1);
    }
}
