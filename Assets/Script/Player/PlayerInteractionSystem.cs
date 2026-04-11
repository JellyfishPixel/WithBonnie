using StarterAssets;
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

    [Header("Third Person Sphere Interact")]
    public bool enableThirdPersonSphere = true;
    public float sphereInteractRadius = 1.2f;
    public LayerMask sphereInteractMask = ~0;
    IInteractable sphereInteractTarget;
    public AudioClip pickupSound;
    public AudioClip dropSound;

    struct ColState { public Collider col; public bool enabled; }
    struct LayerState { public Transform t; public int layer; }

    readonly List<ColState> colStates = new();
    readonly List<LayerState> layerStates = new();
    public static bool BlockWorldInput = false;
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
        if (BlockWorldInput)
            return;

        if (ItemDialogueManager.Instance != null &&
            ItemDialogueManager.Instance.IsShowing)
        {
            return;
        }

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
    bool TryGetSphereInteractable(out IInteractable interactable)
    {
        interactable = null;

        if (!enableThirdPersonSphere || interactRayMode != InteractRayMode.Player)
            return false;

        if (!playerRayOrigin)
            return false;

        Collider[] hits = Physics.OverlapSphere(
            playerRayOrigin.position,
            sphereInteractRadius,
            sphereInteractMask,
            QueryTriggerInteraction.Ignore
        );

        float closest = float.MaxValue;

        foreach (var col in hits)
        {
            var ia =
                col.GetComponent<IInteractable>() ??
                col.GetComponentInParent<IInteractable>();

            if (ia == null) continue;

            float dist = Vector3.Distance(
                playerRayOrigin.position,
                col.ClosestPoint(playerRayOrigin.position)
            );

            if (dist < closest)
            {
                closest = dist;
                interactable = ia;
            }
        }

        return interactable != null;
    }
    public InteractionType GetIntendedInteractionType(GameObject target)
    {
        if (target.GetComponent<IInteractable>() != null ||
            target.GetComponentInParent<IInteractable>() != null)
        {
            return InteractionType.Secondary;
        }

        return InteractionType.Primary;
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

            if (HeldObject != null)
            {
         
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
            return;
        }

        TryInteract(type.Value);
    }
    bool CanStoreHeldBox()
    {
        if (HeldObject == null) return false;

        var box = HeldObject.GetComponent<BoxCore>();
        if (!box) return false;

        if (box.Step != BoxStep.Labeled)
            return false;

        return true;
    }


    void TryInteract(InteractionType interactionType)
    {
        IInteractable target = null;

        // ===============================
        // FIRST PERSON → RAYCAST
        // ===============================
        if (interactRayMode == InteractRayMode.Camera)
        {
            if (TryGetInteractRay(out Ray ray))
            {
                if (Physics.Raycast(
                    ray, out var hit,
                    interactDistance,
                    interactMask,
                    QueryTriggerInteraction.Ignore))
                {
                    target =
                        hit.collider.GetComponent<IInteractable>() ??
                        hit.collider.GetComponentInParent<IInteractable>();
                }
            }
        }

        else if (interactRayMode == InteractRayMode.Player)
        {
            TryGetSphereInteractable(out target);
        }

        if (target == null) return;

        target.Interact(this, interactionType);

        var tps = GetComponent<ThirdPersonController>();
        if (tps) tps.ForceGround();
    }


    public bool TryGetInteractRay(out Ray ray)
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

        //var box = rb.GetComponent<BoxCore>();
        //if (box && BoxCore.Current == box)
        //{
        //    box.SetCurrent(false);
        //    Debug.Log("[Player] Clear CurrentBox because box is picked up");
        //}
        AudioManager.Instance.PlaySFX(pickupSound, transform.position);
        var box = rb.GetComponent<BoxCore>();
        if (box && box.Step == BoxStep.Labeled)
        {
           BoxWorkArea.Instance.ClearCurrentBox(box);
        }

        HeldObject = rb.gameObject;
        heldRb = rb;


        heldRb.constraints = RigidbodyConstraints.None;

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
        if (!box) return;

        Drop();

        AddSalesPopupUI.HideSticky();

        BoxInventory.Instance.StoreBox(box);
    }

    //public void TakeBoxFromInventorySlot(int slotIndex)
    //{
    //    if (HeldObject != null)
    //    {
    //        Debug.Log("[PlayerInteractionSystem] Already holding something");
    //        return;
    //    }

    //    if (BoxInventory.Instance == null || holdPoint == null)
    //        return;

    //    var core = BoxInventory.Instance.SpawnBoxFromSlot(slotIndex, holdPoint);
    //    if (!core) return;

    //    var rb = core.GetComponent<Rigidbody>();
    //    if (!rb)
    //    {
    //        Debug.LogError("[PlayerInteractionSystem] Spawned box has no Rigidbody");
    //        return;
    //    }

    //    Grab(rb);
    //}
    void Drop()
    {
        if (!HeldObject) return;

        HeldObject.transform.SetParent(originalParent, true);

        RestoreLayers();

        foreach (var s in colStates)
            if (s.col) s.col.enabled = s.enabled;
        colStates.Clear();
        AudioManager.Instance.PlaySFX(dropSound, transform.position);
        if (heldRb)
        {
            // 🔥 1) รีเซ็ต rotation ทุกแกน ยกเว้น Y
            Vector3 euler = HeldObject.transform.eulerAngles;
            HeldObject.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

            // 🔥 2) เปิดฟิสิกส์
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
            heldRb.detectCollisions = true;

            // 🔥 3) ล็อก X,Z ปล่อย Y
            heldRb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }

        HeldObject = null;
        heldRb = null;
        originalParent = null;
        layerStates.Clear();

        AddSalesPopupUI.HideSticky();
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
    void OnDrawGizmosSelected()
    {
        if (!playerRayOrigin) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerRayOrigin.position, sphereInteractRadius);
    }

}
