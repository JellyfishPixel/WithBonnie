using UnityEngine;

public interface IInteractable
{
    void Interact(
        PlayerInteractionSystem interactor,
        PlayerInteractionSystem.InteractionType interactionType
    );
}

