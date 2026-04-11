using UnityEngine;
using UnityEngine.UIElements;

public class TapeDispenser : MonoBehaviour, IInteractable
{
    public Material tapeMaterial;

    [Header("Tape Config")]
    public TapeColor tapeColor = TapeColor.Red;
    [SerializeField] private AudioClip interactSound;

    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }
    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
   
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
        var eco = EconomyManager.Instance;
        if (eco != null && !eco.HasTapeUse(tapeColor))
        {
            Debug.Log("[TapeDispenser] No tape left.");
            AddSalesPopupUI.ShowMessage("No tape left.\nPlease buy more tape rolls at the shop.");
            return;
        }
        if (BoxWorkArea.Instance.CurrentBox == null) return;
        var tape = BoxWorkArea.Instance.CurrentBox?.GetComponentInChildren<TapeDragScaler>();


        if (!tape)
        {
            Debug.LogWarning("[TapeDispenser] ไม่พบ TapeDragScaler");
            return;
        }
        var box = BoxWorkArea.Instance?.CurrentBox;
        if (!box.CheckStepOrWarn(BoxStep.Closed))
            return;
        if (BoxWorkArea.Instance.CurrentBox.LidsClosed == true)
        {
            tape.SelectDispenser(this);
            Debug.Log($"[TapeDispenser] Selected: {name}");
        }
        PlayInteractSound();
    }


    public Material GetMaterial()
    {
        return tapeMaterial;
    }
}
