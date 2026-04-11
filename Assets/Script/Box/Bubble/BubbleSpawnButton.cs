using UnityEngine;

public class BubbleSpawnButton : MonoBehaviour, IInteractable
{
    public BubbleType bubbleType = BubbleType.Basic;
    private BoxBubble targetBubble;
    [Header("Interact Sound")]
    [SerializeField] private AudioClip interactSound;
    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        // ใช้คลิกซ้ายเท่านั้น
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
       
        var eco = EconomyManager.Instance;

        if (!eco.HasBubbleStock(bubbleType))
        {
            AddSalesPopupUI.ShowMessage("No bubble left.\nPlease buy more at the shop.");
            return;
        }

        var currentBox = BoxWorkArea.Instance.CurrentBox;
        if (!currentBox) return;

        if (!currentBox.CheckStepOrWarn(BoxStep.ItemInside))
            return;

        if (!currentBox.CanUseBubbleType(bubbleType))
        {
            AddSalesPopupUI.ShowMessage("This bubble type can't be used with this box.");
            return;
        }



        if (targetBubble == null)
            targetBubble = currentBox.GetComponentInChildren<BoxBubble>(true);

        if (targetBubble == null)
        {
            Debug.LogWarning("[BubbleSpawnButton] BoxBubble not found.");
            return;
        }

        // 🔒 ถ้าใส่ bubble ไปแล้ว → ห้ามเปลี่ยนชนิด
        if (targetBubble.HasAnyBubble() &&
            currentBox.BubbleType != bubbleType)
        {
            AddSalesPopupUI.ShowMessage("Cannot change bubble type.");
            return;
        }

        // เช็คเต็มก่อนหัก stock
        if (targetBubble.IsFull())
        {
            
            AddSalesPopupUI.ShowMessage("Bubble is already full.");
            return;
        }

        // 🔽 หัก stock
        if (!eco.TryConsumeBubble(bubbleType))
            return;

        var shopUI = FindFirstObjectByType<BoxShopUI>();
        if (shopUI != null)
            shopUI.RefreshUI();

        // ✅ ตั้งประเภทก่อน
        currentBox.ApplyBubbleType(bubbleType);
        currentBox.hasIceBubble = (bubbleType == BubbleType.Ice);

      
        targetBubble.AddBubble();
        PlayInteractSound();
        if (targetBubble.IsFull())
        {
            GuideArrowManager.Instance?.NextTarget();
        }
    }
    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }

}
