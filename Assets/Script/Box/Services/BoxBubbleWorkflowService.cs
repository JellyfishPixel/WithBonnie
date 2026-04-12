using UnityEngine;

public static class BoxBubbleWorkflowService
{
    public static bool TryAddBubble(BoxCore box, BoxBubble bubbleVisual, BubbleType bubbleType)
    {
        if (box == null || bubbleVisual == null)
            return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy == null || !economy.HasBubbleStock(bubbleType))
        {
            AddSalesPopupUI.ShowMessage("No bubble left.\nPlease buy more at the shop.");
            return false;
        }

        if (!box.CheckStepOrWarn(BoxStep.ItemInside))
            return false;

        if (!box.CanUseBubbleType(bubbleType))
        {
            AddSalesPopupUI.ShowMessage("This bubble type can't be used with this box.");
            return false;
        }

        if (bubbleVisual.HasAnyBubble() && box.BubbleType != bubbleType)
        {
            AddSalesPopupUI.ShowMessage("Cannot change bubble type.");
            return false;
        }

        if (bubbleVisual.IsFull())
        {
            AddSalesPopupUI.ShowMessage("Bubble is already full.");
            return false;
        }

        if (!economy.TryConsumeBubble(bubbleType))
            return false;

        box.ApplyBubbleType(bubbleType);
        bubbleVisual.AddBubbleUnit(bubbleType);
        box.NotifyBubbleStarted();

        if (bubbleVisual.IsFull())
        {
            box.NotifyBubbleFull();
            GuideArrowManager.Instance?.NextTarget();
        }

        var shopUI = Object.FindFirstObjectByType<BoxShopUI>();
        if (shopUI != null)
            shopUI.RefreshUI();

        return true;
    }
}
