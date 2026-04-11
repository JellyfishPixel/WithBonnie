using UnityEngine;

public class InteractIconUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInteractionSystem interactionSystem;
    public SpriteRenderer spriteRenderer;
    public Camera worldCamera;

    [Header("Icons")]
    public Sprite mouseIcon; // pickup
    public Sprite keyEIcon;  // interact

    [Header("World Offset")]
    public Vector3 worldOffset = new Vector3(0, 0.25f, 0);
    [Header("Depth Offset")]
    public float forwardOffset = 0.15f; // ปรับค่านี้

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.enabled = false;

        if (!worldCamera)
            worldCamera = Camera.main;
    }

    void Update()
    {
        // ❌ ถือของ / dialogue
        if (!interactionSystem ||
            interactionSystem.HeldObject != null ||
            (ItemDialogueManager.Instance && ItemDialogueManager.Instance.IsShowing))
        {
            Hide();
            return;
        }

        // 🔥 ใช้กล้องปัจจุบันเสมอ
        worldCamera = interactionSystem.GetCurrentCamera();
        if (!worldCamera)
        {
            Hide();
            return;
        }

        if (!TryGetTarget(out Transform target, out Sprite icon))
        {
            Hide();
            return;
        }

        ShowAt(target, icon);
    }


    bool TryGetTarget(out Transform target, out Sprite icon)
    {
        target = null;
        icon = null;

        // =====================================================
        // 1️⃣ RAYCAST → PRIMARY → MOUSE
        // =====================================================
        if (interactionSystem.TryGetInteractRay(out Ray ray))
        {
            float maxDist = Mathf.Max(
                interactionSystem.interactDistance,
                interactionSystem.pickupDistance
            );

            if (Physics.Raycast(
                ray, out RaycastHit hit, maxDist,
                interactionSystem.interactMask,
                QueryTriggerInteraction.Ignore))
            {
                target = hit.transform;

                // 🔥 Ray = Mouse เสมอ
                icon = mouseIcon;
                return true;
            }
        }

        // =====================================================
        // 2️⃣ SPHERE / COLLIDER → SECONDARY → E
        // =====================================================
        if (interactionSystem.enableThirdPersonSphere &&
            interactionSystem.interactRayMode ==
            PlayerInteractionSystem.InteractRayMode.Player)
        {
            Collider[] hits = Physics.OverlapSphere(
                interactionSystem.playerRayOrigin.position,
                interactionSystem.sphereInteractRadius,
                interactionSystem.sphereInteractMask,
                QueryTriggerInteraction.Ignore
            );

            float closest = float.MaxValue;
            Transform bestTf = null;

            foreach (var col in hits)
            {
                var ia =
                    col.GetComponent<IInteractable>() ??
                    col.GetComponentInParent<IInteractable>();

                if (ia == null) continue;

                float d = Vector3.Distance(
                    interactionSystem.playerRayOrigin.position,
                    col.ClosestPoint(interactionSystem.playerRayOrigin.position)
                );

                if (d < closest)
                {
                    closest = d;
                    bestTf = (ia as Component).transform;
                }
            }

            if (bestTf != null)
            {
                target = bestTf;

                // 🔥 Collider = E
                icon = keyEIcon;
                return true;
            }
        }

        return false;
    }

    Vector3 GetIconWorldPosition(Transform target)
    {
        // 1) จุดบนสุดของ object
        var rend = target.GetComponentInChildren<Renderer>();
        Vector3 basePos = rend
            ? rend.bounds.center + Vector3.up * rend.bounds.extents.y
            : target.position + worldOffset;

        // 2) ดันออกมาทางกล้อง
        var cam = interactionSystem.GetCurrentCamera();
        if (cam)
        {
            Vector3 toCam = (basePos - cam.transform.position).normalized;
            basePos += toCam * forwardOffset;
        }

        return basePos;
    }

    void ShowAt(Transform target, Sprite icon)
    {
        spriteRenderer.sprite = icon;
        spriteRenderer.enabled = true;

        transform.position = GetIconWorldPosition(target);

        var cam = interactionSystem.GetCurrentCamera();
        if (cam)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - cam.transform.position
            );
        }
    }

    void Hide()
    {
        spriteRenderer.enabled = false;
    }


}
