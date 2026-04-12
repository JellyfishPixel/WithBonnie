public static class BoxLabelWorkflowService
{
    public static bool CanStartLabelPlacement(BoxCore box)
    {
        if (box == null)
            return false;

        return box.CheckStepOrWarn(BoxStep.Taped);
    }

    public static void CompleteLabelPlacement(BoxCore box)
    {
        if (box == null)
            return;

        box.NotifyLabelPasted();
    }
}
