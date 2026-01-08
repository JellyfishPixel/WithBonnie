using UnityEngine;

public class TapeDispenser : MonoBehaviour, IInteractable
{
    public Material tapeMaterial;

    [Header("Tape Config")]
    public TapeColor tapeColor = TapeColor.Red;

    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        // Mouse0 เท่านั้น
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
        var eco = EconomyManager.Instance;
        if (eco != null && !eco.HasTapeUse(tapeColor))
        {
            Debug.Log("[TapeDispenser] No tape left.");
            AddSalesPopupUI.ShowMessage("No tape left.\nPlease buy more tape rolls at the shop.");
            return;
        }

        var tape = FindFirstObjectByType<TapeDragScaler>();

        if (!tape)
        {
            Debug.LogWarning("[TapeDispenser] ไม่พบ TapeDragScaler");
            return;
        }

        if (BoxCore.Current.LidsClosed == true)
        {
            tape.SelectDispenser(this);
            Debug.Log($"[TapeDispenser] Selected: {name}");
        }
        //else
        //{
        //    AddSalesPopupUI.ShowMessage("Close lids before drag tape");
        //}
    }


    public Material GetMaterial()
    {
        return tapeMaterial;
    }
}
