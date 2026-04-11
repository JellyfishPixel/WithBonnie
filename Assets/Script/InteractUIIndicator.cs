using UnityEngine;

public class InteractUIIndicator : MonoBehaviour
{
    [Header("Auto Reference")]
    public PlayerInteractionSystem interactionSystem;

    [Header("Sprite UI")]
    public SpriteRenderer spriteRenderer;
    public Sprite primarySprite;    // 🔵 Primary (E)
    public Sprite secondarySprite;  // 🟡 Secondary (F)

    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public int renderOrder = 1000;

    void Awake()
    {
        if (!interactionSystem)
            interactionSystem = FindFirstObjectByType<PlayerInteractionSystem>();

        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer)
        {
            spriteRenderer.sortingOrder = renderOrder;
            spriteRenderer.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (!interactionSystem || !spriteRenderer)
        {
            Hide();
            return;
        }

        if (interactionSystem.HeldObject != null)
        {
            Hide();
            return;
        }

        if (ItemDialogueManager.Instance != null &&
            ItemDialogueManager.Instance.IsShowing)
        {
            Hide();
            return;
        }

        if (!TryGetTarget(out Transform target, out PlayerInteractionSystem.InteractionType type))
        {
            Hide();
            return;
        }

        SetSprite(type);
        ShowOn(target);
    }

    bool TryGetTarget(
        out Transform target,
        out PlayerInteractionSystem.InteractionType interactionType)
    {
        target = null;
        interactionType = PlayerInteractionSystem.InteractionType.Primary;

        if (!interactionSystem.TryGetInteractRay(out Ray ray))
            return false;

        float maxDistance = Mathf.Max(
            interactionSystem.interactDistance,
            interactionSystem.pickupDistance
        );

        int mask =
            interactionSystem.interactMask &
            ~LayerMask.GetMask("PickableNoOutline");

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask,
            QueryTriggerInteraction.Ignore))
            return false;

        // 1️⃣ Interactable (Primary)
        var interactable =
            hit.collider.GetComponent<IInteractable>() ??
            hit.collider.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            var mb = interactable as MonoBehaviour;
            if (mb != null)
            {
                target = mb.transform;
                interactionType = PlayerInteractionSystem.InteractionType.Primary;
                return true;
            }
        }

        // 2️⃣ Pickable / Secondary
        if (hit.transform.CompareTag(interactionSystem.pickableTag) &&
            hit.distance <= interactionSystem.pickupDistance)
        {
            target = hit.transform;
            interactionType = PlayerInteractionSystem.InteractionType.Secondary;
            return true;
        }

        return false;
    }

    void SetSprite(PlayerInteractionSystem.InteractionType type)
    {
        if (!spriteRenderer) return;

        spriteRenderer.sprite =
            type == PlayerInteractionSystem.InteractionType.Secondary
            ? secondarySprite
            : primarySprite;
    }

    void ShowOn(Transform target)
    {
        if (!spriteRenderer.gameObject.activeSelf)
            spriteRenderer.gameObject.SetActive(true);

        spriteRenderer.transform.position = target.position + offset;

        var cam = interactionSystem.GetCurrentCamera();
        if (cam)
        {
            spriteRenderer.transform.rotation =
                Quaternion.LookRotation(
                    spriteRenderer.transform.position - cam.transform.position
                );
        }
    }

    void Hide()
    {
        if (spriteRenderer.gameObject.activeSelf)
            spriteRenderer.gameObject.SetActive(false);
    }
}
