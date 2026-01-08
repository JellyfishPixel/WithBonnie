using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionSystem : MonoBehaviour
{
    public enum InteractionType
    {
        Primary,    // ใช้, คุย, เปิด
        Secondary,  // สำรอง
        Alternate   // พิเศษ
    }
    public enum InteractRayMode
    {
        Camera,   // First Person
        Player    // Third Person
    }

    [Header("Interact Keys")]
    public KeyCode[] primaryInteractKeys;   // E, Mouse0
    public KeyCode[] secondaryInteractKeys; // F
    [Header("Interact")]
    public float interactDistance = 3f;
    public LayerMask interactMask = ~0;

    [Header("Camera")]
    public Camera playerCamera; // fallback เท่านั้น

    public Camera currentCamera;
    [Header("Interact Ray")]
    public InteractRayMode interactRayMode = InteractRayMode.Camera;

    // ใช้ตอน Third Person (เช่น chest / center)
    public Transform playerRayOrigin;



    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.Mouse0;
    public float pickupDistance = 4f;
    public string pickableTag = "pickable";

    [Header("Hold Settings")]
    public Transform holdPoint;
    public string holdLayerName = "holdLayer";
    public float scrollYawSpeed = 160f;

    [Header("Box Inventory")]
    public KeyCode storeBoxKey = KeyCode.E;

    bool isMovementLocked = false;

    // ---------- held state ----------
    public GameObject HeldObject { get; private set; }
    Rigidbody heldRb;
    Transform originalParent;
    Quaternion targetLocalRot;

    struct ColState { public Collider col; public bool enabled; }
    struct LayerState { public Transform t; public int layer; }

    readonly List<ColState> colStates = new();
    readonly List<LayerState> layerStates = new();

    int holdLayer = -1;
    void Awake()
    {
        // fallback เท่านั้น
        if (!playerCamera)
            playerCamera = Camera.main;

        // ตั้งค่าเริ่มต้นให้ currentCamera
        if (!currentCamera)
            currentCamera = playerCamera;

        if (!holdPoint && currentCamera)
        {
            var go = new GameObject("HoldPoint");
            holdPoint = go.transform;
            holdPoint.SetParent(currentCamera.transform, false);
            holdPoint.localPosition = new Vector3(0, 0, 1.0f);
            holdPoint.localRotation = Quaternion.identity;
        }

        holdLayer = LayerMask.NameToLayer(holdLayerName);
    }

    void Update()
    {
        if (isMovementLocked) return;

        HandleInteractInput();
        HandlePickupInput();
        HandleHoldRotation();
    }


    void LateUpdate()
    {
        if (HeldObject && holdPoint)
        {
            HeldObject.transform.localPosition = Vector3.zero;
            HeldObject.transform.localRotation = targetLocalRot;
        }
    }
    bool IsAnyKeyDown(KeyCode[] keys)
    {
        foreach (var k in keys)
            if (Input.GetKeyDown(k))
                return true;
        return false;
    }
    void HandlePickupInput()
    {
        if (!Input.GetKeyDown(pickupKey)) return;

        if (HeldObject == null)
            TryPickup();
        else
            Drop();
    }

    void HandleInteractInput()
    {
        InteractionType? type = null;

        if (IsAnyKeyDown(primaryInteractKeys))
            type = InteractionType.Primary;
        else if (IsAnyKeyDown(secondaryInteractKeys))
            type = InteractionType.Secondary;

        if (type == null) return;
        if (type == InteractionType.Secondary)
        {
            // ต้องถือกล่องอยู่
            if (HeldObject != null)
            {
                // เช็คว่ากล้องกำลังเล็งกล่องที่เสร็จแล้วหรือไม่
                if (CanStoreHeldBox())
                {
                    StoreHeldBoxToInventory();
                }
                else
                {
                    Debug.Log("กล่องยังไม่เสร็จ เก็บไม่ได้");
                }
                return;
            }
        }
        if (ItemDialogueManager.Instance != null &&
            ItemDialogueManager.Instance.IsShowing)
        {
            ItemDialogueManager.Instance.SkipTypingOrNext();
            return;
        }

        TryInteract(type.Value);
    }
    bool CanStoreHeldBox()
    {
        if (HeldObject == null) return false;

        var box = HeldObject.GetComponent<BoxCore>();
        if (!box) return false;

        // ✅ แค่เช็คว่าเสร็จแล้วพอ
        if (box.Step != BoxStep.Labeled)
            return false;

        return true;
    }


    void TryInteract(InteractionType interactionType)
    {
        if (!TryGetInteractRay(out Ray ray)) return;

        if (!Physics.Raycast(ray, out var hit, interactDistance, interactMask,
            QueryTriggerInteraction.Ignore))
            return;

        var interactable =
            hit.collider.GetComponent<IInteractable>() ??
            hit.collider.GetComponentInParent<IInteractable>();

        interactable?.Interact(this, interactionType);

    }

    bool TryGetInteractRay(out Ray ray)
    {
        ray = default;

        switch (interactRayMode)
        {
            case InteractRayMode.Camera:
                if (!currentCamera) return false;
                ray = new Ray(
                    currentCamera.transform.position,
                    currentCamera.transform.forward
                );
                return true;

            case InteractRayMode.Player:
                if (!playerRayOrigin) return false;
                ray = new Ray(
                    playerRayOrigin.position,
                    playerRayOrigin.forward
                );
                return true;
        }

        return false;
    }
    void TryPickup()
    {
        if (!TryGetInteractRay(out Ray ray) || !holdPoint) return;

        int mask = ~LayerMask.GetMask(holdLayerName);

        if (!Physics.Raycast(ray, out var hit, pickupDistance, mask,
            QueryTriggerInteraction.Ignore))
            return;

        if (!hit.transform.CompareTag(pickableTag))
            return;

        var rb = hit.rigidbody ?? hit.transform.GetComponent<Rigidbody>();
        if (!rb) return;

        Grab(rb);
    }

    void Grab(Rigidbody rb)
    {
        HeldObject = rb.gameObject;
        heldRb = rb;
        originalParent = HeldObject.transform.parent;

        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;

        heldRb.useGravity = false;
        heldRb.detectCollisions = false;
        heldRb.isKinematic = true;

        colStates.Clear();
        foreach (var c in HeldObject.GetComponentsInChildren<Collider>(true))
        {
            colStates.Add(new ColState { col = c, enabled = c.enabled });
            c.enabled = false;
        }

        CacheAndSetLayerRecursive(HeldObject.transform, holdLayer);

        HeldObject.transform.SetParent(holdPoint, true);
        HeldObject.transform.localPosition = Vector3.zero;
        HeldObject.transform.localRotation = Quaternion.identity;
        targetLocalRot = Quaternion.identity;
    }

    public void StoreHeldBoxToInventory()
    {
        if (HeldObject == null) return;
        if (BoxInventory.Instance == null) return;

        var box = HeldObject.GetComponent<BoxCore>();
        if (!box)
        {
            Debug.Log("[PlayerInteractionSystem] Held object is not a BoxCore");
            return;
        }

        // ก่อนเก็บต้อง Drop เพื่อคืนค่า Rigidbody/Collider
        Drop();

        if (!BoxInventory.Instance.StoreBox(box))
        {
            Debug.Log("[PlayerInteractionSystem] Cannot store box (inventory full?)");
        }
    }

    public void TakeBoxFromInventorySlot(int slotIndex)
    {
        if (HeldObject != null)
        {
            Debug.Log("[PlayerInteractionSystem] Already holding something");
            return;
        }

        if (BoxInventory.Instance == null || holdPoint == null)
            return;

        var core = BoxInventory.Instance.SpawnBoxFromSlot(slotIndex, holdPoint);
        if (!core) return;

        var rb = core.GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("[PlayerInteractionSystem] Spawned box has no Rigidbody");
            return;
        }

        // ใช้ระบบ Grab เดิมให้มาถือในมือเลย
        Grab(rb);
    }

    void Drop()
    {
        if (!HeldObject) return;

        HeldObject.transform.SetParent(originalParent, true);

        if (currentCamera)
        {
            Vector3 up = Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(currentCamera.transform.forward, up).normalized;
            HeldObject.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        RestoreLayers();

        foreach (var s in colStates)
            if (s.col) s.col.enabled = s.enabled;
        colStates.Clear();

        if (heldRb)
        {
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
            heldRb.detectCollisions = true;
        }

        HeldObject = null;
        heldRb = null;
        originalParent = null;
        layerStates.Clear();
    }


    void HandleHoldRotation()
    {
        if (!HeldObject) return;

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0005f)
            targetLocalRot =
                Quaternion.AngleAxis(wheel * scrollYawSpeed, Vector3.up) * targetLocalRot;
    }



    void CacheAndSetLayerRecursive(Transform root, int newLayer)
    {
        layerStates.Clear();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            layerStates.Add(new LayerState { t = t, layer = t.gameObject.layer });
            if (newLayer >= 0)
                t.gameObject.layer = newLayer;
        }
    }

    void RestoreLayers()
    {
        foreach (var s in layerStates)
            if (s.t) s.t.gameObject.layer = s.layer;
    }

    public void LockMovement()
    {
        isMovementLocked = true;
        var controller = GetComponent<CharacterController>();
        if (controller) controller.enabled = false;
    }

    public void UnlockMovement()
    {
        isMovementLocked = false;
        var controller = GetComponent<CharacterController>();
        if (controller) controller.enabled = true;
    }

    public bool IsMovementLocked()
    {
        return isMovementLocked;
    }

    public void SetCurrentCamera(Camera cam)
    {
        if (cam == null)
        {
            Debug.LogWarning("[PlayerInteractionSystem] SetCurrentCamera called with null");
            return;
        }

        currentCamera = cam;
    }
    public Camera GetCurrentCamera()
    {
        return currentCamera;
    }

}
