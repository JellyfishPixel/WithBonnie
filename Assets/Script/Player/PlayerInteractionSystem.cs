using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionSystem : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Interact")]
    public KeyCode interactKey = KeyCode.Mouse0;
    public float interactDistance = 3f;

    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.Mouse0;   // คลิกซ้าย
    public float pickupDistance = 4f;
    public string pickableTag = "pickable";

    [Header("Hold Settings")]
    public Transform holdPoint;
    public string holdLayerName = "holdLayer";
    public float scrollYawSpeed = 160f;

    [Header("Box Inventory")]
    public KeyCode storeBoxKey = KeyCode.E;   // กด E ตอนถือกล่อง = เก็บเข้าตัว

    bool isMovementLocked = false;
    // ---------- held state ----------
    public GameObject HeldObject { get; private set; }
    Rigidbody heldRb;
    Transform originalParent;
    Quaternion targetLocalRot;

    bool prevKinematic, prevUseGravity, prevDetectCollisions;
    RigidbodyInterpolation prevInterp;
    CollisionDetectionMode prevCdm;

    struct ColState { public Collider col; public bool enabled; }
    struct LayerState { public Transform t; public int layer; }

    readonly List<ColState> colStates = new();
    readonly List<LayerState> layerStates = new();

    int holdLayer = -1;

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;

        if (!holdPoint && playerCamera)
        {
            var go = new GameObject("HoldPoint");
            holdPoint = go.transform;
            holdPoint.SetParent(playerCamera.transform, false);
            holdPoint.localPosition = new Vector3(0, 0, 1.0f);
            holdPoint.localRotation = Quaternion.identity;
        }

        if (!string.IsNullOrEmpty(holdLayerName))
        {
            holdLayer = LayerMask.NameToLayer(holdLayerName);
            if (holdLayer < 0)
                Debug.LogWarning($"[PlayerInteractionSystem] Layer '{holdLayerName}' ยังไม่มีใน Project Settings > Tags and Layers");
        }
    }

    void Update()
    {
        if (isMovementLocked) return;

            if (Input.GetKeyDown(storeBoxKey))
        {
            if (HeldObject != null)
                StoreHeldBoxToInventory();
            else
                TryInteract(); 
        }

        if (Input.GetKeyDown(interactKey))
        {
            // ถ้ามีไดอะล็อกอยู่ → ใช้กดข้าม/ต่อแทน
            if (ItemDialogueManager.Instance != null && ItemDialogueManager.Instance.IsShowing)
            {
                ItemDialogueManager.Instance.SkipTypingOrNext();
                return;
            }

            // ปกติ: raycast หา IInteractable แล้วเรียก Interact(player)
            TryInteract();
        }

        if (Input.GetKeyDown(pickupKey))
        {
            if (HeldObject == null) TryPickup();
            else Drop();
        }

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

    #region Interact

    void TryInteract()
    {
        if (!playerCamera) return;

        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            var interactable = hit.collider.GetComponent<IInteractable>() ??
                               hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
                interactable.Interact(this);
        }
    }

    #endregion

    #region Pickup

    void TryPickup()
    {
        if (!playerCamera || !holdPoint) return;

        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out var hit, pickupDistance, ~0, QueryTriggerInteraction.Ignore))
            return;

        if (!hit.transform.CompareTag(pickableTag))
            return;

        var rb = hit.rigidbody ? hit.rigidbody : hit.transform.GetComponent<Rigidbody>();
        if (!rb) return;

        Grab(rb);
    }

    void Grab(Rigidbody rb)
    {
        HeldObject = rb.gameObject;
        heldRb = rb;
        originalParent = HeldObject.transform.parent;

        prevKinematic = heldRb.isKinematic;
        prevUseGravity = heldRb.useGravity;
        prevDetectCollisions = heldRb.detectCollisions;
        prevInterp = heldRb.interpolation;
        prevCdm = heldRb.collisionDetectionMode;

        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;

        heldRb.useGravity = false;
        heldRb.detectCollisions = false;
        heldRb.interpolation = RigidbodyInterpolation.None;
        heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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

        // เอาออกจากมือ กลับไป parent เดิม
        HeldObject.transform.SetParent(originalParent, true);

        // ให้กล่องตั้งตรงก่อนปล่อย
        if (playerCamera)
        {

            Vector3 up = Vector3.up;


            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward; 

            forward.Normalize();
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
            heldRb.interpolation = RigidbodyInterpolation.Interpolate;
            heldRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            heldRb.angularVelocity = Vector3.zero;
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
            targetLocalRot = Quaternion.AngleAxis(wheel * scrollYawSpeed, Vector3.up) * targetLocalRot;
    }

    #endregion

    #region Layer helpers

    void CacheAndSetLayerRecursive(Transform root, int newLayer)
    {
        layerStates.Clear();
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
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

    #endregion

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
}
