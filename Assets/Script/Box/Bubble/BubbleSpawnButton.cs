using UnityEngine;

public class BubbleSpawnButton : MonoBehaviour, IInteractable
{
    public BubbleType bubbleType = BubbleType.Basic;
    private BoxBubble targetBubble;
    private BoxCore targetBox;
    [Header("Interact Sound")]
    [SerializeField] private AudioClip interactSound;
    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;

        var currentBox = BoxWorkArea.Instance.CurrentBox;
        if (!currentBox) return;

        if (targetBubble == null || targetBox != currentBox)
        {
            targetBox = currentBox;
            targetBubble = currentBox.GetComponentInChildren<BoxBubble>(true);
        }

        if (targetBubble == null)
        {
            Debug.LogWarning("[BubbleSpawnButton] BoxBubble not found.");
            return;
        }

        if (!BoxBubbleWorkflowService.TryAddBubble(currentBox, targetBubble, bubbleType))
            return;

        PlayInteractSound();
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
