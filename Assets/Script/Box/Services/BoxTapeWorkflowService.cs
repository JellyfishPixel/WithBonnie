using UnityEngine;

public static class BoxTapeWorkflowService
{
    public static bool TrySelectTape(BoxCore box, TapeDispenser dispenser, TapeDragScaler tapeDrag)
    {
        if (box == null || dispenser == null || tapeDrag == null)
            return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy != null && !economy.HasTapeUse(dispenser.tapeColor))
        {
            AddSalesPopupUI.ShowMessage("No tape left.\nPlease buy more tape rolls at the shop.");
            return false;
        }

        if (!box.CheckStepOrWarn(BoxStep.Closed))
            return false;

        if (!box.LidsClosed)
            return false;

        tapeDrag.SetSelectedDispenser(dispenser);
        return true;
    }

    public static void CompleteTape(BoxCore box, TapeDispenser dispenser)
    {
        if (box == null || dispenser == null)
            return;

        box.RememberTape(dispenser.tapeColor, dispenser.GetMaterial());

        EconomyManager economy = EconomyManager.Instance;
        if (economy != null)
        {
            economy.TryConsumeTapeUse(dispenser.tapeColor);
            var shopUI = Object.FindFirstObjectByType<BoxShopUI>();
            if (shopUI != null)
                shopUI.RefreshUI();
        }

        box.NotifyTapeDone();
    }
}
