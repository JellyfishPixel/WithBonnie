using UnityEngine;
using System.Collections.Generic;

public class InteractOutlineHighlighter : MonoBehaviour
{
    [Header("Reference")]
    public PlayerInteractionSystem interactionSystem;

    [Header("Outline")]
    public Material outlineMaterial;

    Renderer currentRenderer;
    Material[] originalMats;

    void Update()
    {
        if (!interactionSystem) return;

        if (!TryGetHighlightTarget(out Renderer hitRenderer))
        {
            ClearOutline();
            return;
        }

        if (currentRenderer == hitRenderer)
            return;

        ClearOutline();
        ApplyOutline(hitRenderer);
    }

    bool TryGetHighlightTarget(out Renderer renderer)
    {

        renderer = null;
        if (interactionSystem.interactRayMode == PlayerInteractionSystem.InteractRayMode.Player)
        {

            return false;
        }
 
        if (interactionSystem.HeldObject != null)
            return false;

        if (!interactionSystem.TryGetInteractRay(out Ray ray))
            return false;


        float maxDistance = Mathf.Max(
            interactionSystem.interactDistance,
            interactionSystem.pickupDistance
        );

        int outlineMask =
    interactionSystem.interactMask &
    ~LayerMask.GetMask("PickableNoOutline");

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, outlineMask,
            QueryTriggerInteraction.Ignore))
            return false;

        // ---------- 1️⃣ Interactable ----------
        var interactable =
            hit.collider.GetComponent<IInteractable>() ??
            hit.collider.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            renderer =
                hit.collider.GetComponent<Renderer>() ??
                hit.collider.GetComponentInParent<Renderer>();
            return renderer != null;
        }

        // ---------- 2️⃣ Pickable ----------
        if (hit.transform.CompareTag(interactionSystem.pickableTag))
        {
            // pickup ต้องอยู่ใน pickupDistance
            if (hit.distance > interactionSystem.pickupDistance)
                return false;

            renderer =
                hit.collider.GetComponent<Renderer>() ??
                hit.collider.GetComponentInParent<Renderer>();
            return renderer != null;
        }

        return false;
    }

    void ApplyOutline(Renderer r)
    {
        currentRenderer = r;
        originalMats = r.materials;

        var mats = new List<Material>(originalMats)
        {
            outlineMaterial
        };

        r.materials = mats.ToArray();
    }

    public void ClearOutline()
    {
        if (!currentRenderer) return;

        currentRenderer.materials = originalMats;
        currentRenderer = null;
        originalMats = null;
    }
}
