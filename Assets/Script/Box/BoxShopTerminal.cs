using UnityEngine;

public class BoxShopTerminal : MonoBehaviour, IInteractable
{
    [Header("Reference")]
    public BoxShopUI shopUI;

    [SerializeField] private AudioClip interactSound;

    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }
    public void Interact(PlayerInteractionSystem player,
                         PlayerInteractionSystem.InteractionType type)
    {

 
        if (!shopUI)
        {
            Debug.LogWarning("[BoxShopTerminal] shopUI not assigned");
            return;
        }

        shopUI.Open(this, player);
        PlayInteractSound();
    }

}
