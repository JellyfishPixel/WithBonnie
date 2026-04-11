using UnityEngine;

[RequireComponent(typeof(SmoothLidClose))]
public class BoxLidInteractable : MonoBehaviour, IInteractable
{
    public BoxCore box;

    SmoothLidClose lid;
    [Header("Interact Sound")]
    [SerializeField] private AudioClip interactSound;
    void Awake()
    {
        lid = GetComponent<SmoothLidClose>();
        if (!box) box = GetComponentInParent<BoxCore>();
    }

    public void Interact(PlayerInteractionSystem interactor, PlayerInteractionSystem.InteractionType type)
    {
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;

        if (box == null || lid == null) return;
        if (!box.CanCloseLid()) return;
        if (lid.isClosed) return;
        if (!box.CheckStepOrWarn(BoxStep.BubbleDone))
            return;
        lid.CloseLid();
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
