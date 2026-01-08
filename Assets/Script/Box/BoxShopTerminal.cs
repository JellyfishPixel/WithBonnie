using UnityEngine;

public class BoxShopTerminal : MonoBehaviour, IInteractable
{
    [Header("Reference")]
    public BoxShopUI shopUI;
    public void Interact(PlayerInteractionSystem player,
                         PlayerInteractionSystem.InteractionType type)
    {
        // Mouse0 เท่านั้น
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
        if (!shopUI)
        {
            Debug.LogWarning("[BoxShopTerminal] shopUI not assigned");
            return;
        }

        shopUI.Open(this, player);
    }

    public void NotifyShopClosed()
    {
        // เผื่ออนาคตอยากเล่นอนิเมชันตอนปิดร้าน ฯลฯ
    }
}
