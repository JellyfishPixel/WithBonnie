using UnityEngine;

public class TVInteract : MonoBehaviour, IInteractable
{
    public TVUI tvUI;

    [SerializeField] private AudioClip interactSound;
    public void Interact(PlayerInteractionSystem player,
        PlayerInteractionSystem.InteractionType type)
    {

        tvUI.OpenTV();
        AudioManager.Instance.PlaySFX(
    interactSound,
    transform.position
);
       
    }
}