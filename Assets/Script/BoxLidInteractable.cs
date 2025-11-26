using UnityEngine;

[RequireComponent(typeof(SmoothLidClose))]
public class BoxLidInteractable : MonoBehaviour, IInteractable
{
    public BoxCore box;

    SmoothLidClose lid;

    void Awake()
    {
        lid = GetComponent<SmoothLidClose>();
        if (!box) box = GetComponentInParent<BoxCore>();
    }

    public void Interact(PlayerInteractionSystem interactor)
    {
        if (box == null || lid == null) return;

        // ???????????: ????????? + ???????????????
        if (!box.CanCloseLid()) return;

        if (lid.isClosed) return;   // ????????????????????
        lid.CloseLid();
    }
}
