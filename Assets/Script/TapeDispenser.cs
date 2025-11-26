using UnityEngine;

public class TapeDispenser : MonoBehaviour, IInteractable
{
    public Material tapeMaterial;

    public void Interact(PlayerInteractionSystem interactor)
    {
        var tape = FindFirstObjectByType<TapeDragScaler>();

        if (!tape)
        {
            Debug.LogWarning("[TapeDispenser] ไม่พบ TapeDragScaler");
            return;
        }

        tape.SelectDispenser(this);
        Debug.Log($"[TapeDispenser] Selected: {name}");
    }

    public Material GetMaterial()
    {
        return tapeMaterial;
    }
}
