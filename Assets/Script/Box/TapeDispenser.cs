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
        var box = BoxWorkArea.Instance != null ? BoxWorkArea.Instance.CurrentBox : null;
        if (box == null) return;

        var tape = box.GetComponentInChildren<TapeDragScaler>();

        if (!tape)
        {
            Debug.LogWarning("[TapeDispenser] ไม่พบ TapeDragScaler");
            return;
        }

        if (!BoxTapeWorkflowService.TrySelectTape(box, this, tape))
            return;

        Debug.Log($"[TapeDispenser] Selected: {name}");
        PlayInteractSound();
    }


    public Material GetMaterial()
    {
        return tapeMaterial;
    }
}
