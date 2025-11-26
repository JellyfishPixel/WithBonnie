// PlayerInteractionSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionSystem : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Interact Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3f;

    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.Mouse0;     // คลิกซ้าย: หยิบ/วาง
    public float pickupDistance = 4f;
    public string pickableTag = "pickable";

    [Tooltip("จุดที่ของจะถูกถือไว้ (สร้าง Empty วางหน้ากล้อง/หน้าอก แล้วลากมาใส่)")]
    public Transform holdPoint;

    [Header("Hold Layer (Render)")]
    [Tooltip("เลเยอร์ที่ใช้เวลาถือของ เช่น 'holdItem' (ใช้กับกล้อง overlay ได้)")]
    public string holdLayerName = "holdItem";

    [Header("Rotate While Holding")]
    public float scrollYawSpeed = 160f;

    // ===== Runtime state =====
    public GameObject HeldObject { get; private set; }
    Rigidbody heldRb;
    Transform originalParent;
    Quaternion targetLocalRot;

    // Physics backup
    bool prevKinematic, prevUseGravity, prevDetectCollisions;
    RigidbodyInterpolation prevInterp;
    CollisionDetectionMode prevCdm;

    // Collider states
    struct ColState { public Collider col; public bool enabled; }
    readonly List<ColState> colStates = new List<ColState>();

    // Layer states
    struct LayerState { public Transform t; public int layer; }
    readonly List<LayerState> layerStates = new List<LayerState>();

    int holdLayer = -1;

    void Awake()
    {
        if (!playerCamera)
            playerCamera = Camera.main;

        // เตรียม holdPoint อัตโนมัติถ้าไม่ได้เซ็ต
        if (!holdPoint && playerCamera)
        {
            GameObject go = new GameObject("HoldPoint (auto)");
            holdPoint = go.transform;
            holdPoint.SetParent(playerCamera.transform, false);
            holdPoint.localPosition = new Vector3(0, 0, 1.0f);
            holdPoint.localRotation = Quaternion.identity;
        }

        // หา layer สำหรับถือของ
        if (!string.IsNullOrEmpty(holdLayerName))
        {
            holdLayer = LayerMask.NameToLayer(holdLayerName);
            if (holdLayer < 0)
            {
                Debug.LogWarning($"[PlayerInteractionSystem] Layer '{holdLayerName}' ยังไม่มีใน Project Settings > Tags and Layers");
            }
        }
    }

    void Update()
    {
        HandleInteractInput();
        HandlePickupInput();
        HandleHoldRotation();
    }

    void LateUpdate()
    {
        // ล็อกตำแหน่งของที่ถือไว้ที่ holdPoint
        if (HeldObject != null && holdPoint != null)
        {
            HeldObject.transform.localPosition = Vector3.zero;
            HeldObject.transform.localRotation = targetLocalRot;
        }
    }

    #region Interact

    void HandleInteractInput()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (!playerCamera) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // หาจาก collider ก่อน
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                // ถ้าไม่มีบน collider ลองหาบน parent
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                interactable.Interact(this);
            }
        }
    }

    #endregion

    #region Pickup

    void HandlePickupInput()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (HeldObject == null)
                TryPickup();
            else
                Drop();
        }
    }

    void TryPickup()
    {
        if (!playerCamera || !holdPoint) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance, ~0, QueryTriggerInteraction.Ignore))
            return;

        // ต้องเป็น tag pickable เท่านั้น
        if (!hit.transform.CompareTag(pickableTag))
            return;

        // หาร่างหลักที่มี Rigidbody
        Rigidbody rb = hit.rigidbody ? hit.rigidbody : hit.transform.GetComponent<Rigidbody>();
        if (!rb) return;

        Grab(rb);
    }

    void Grab(Rigidbody rb)
    {
        HeldObject = rb.gameObject;
        heldRb = rb;
        originalParent = HeldObject.transform.parent;

        // เก็บค่าฟิสิกส์เดิม
        prevKinematic = heldRb.isKinematic;
        prevUseGravity = heldRb.useGravity;
        prevDetectCollisions = heldRb.detectCollisions;
        prevInterp = heldRb.interpolation;
        prevCdm = heldRb.collisionDetectionMode;

        // เคลียร์ velocity ก่อน
#if UNITY_6000_0_OR_NEWER
        heldRb.linearVelocity = Vector3.zero;
#else
        heldRb.velocity = Vector3.zero;
#endif
        heldRb.angularVelocity = Vector3.zero;

        // เซ็ตค่าฟิสิกส์ขณะถือ
        heldRb.useGravity = false;
        heldRb.detectCollisions = false;
        heldRb.interpolation = RigidbodyInterpolation.None;
        heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        heldRb.isKinematic = true;

        // ปิดคอลลิเดอร์ทั้งหมด แล้วเก็บสถานะเดิม
        colStates.Clear();
        foreach (var col in HeldObject.GetComponentsInChildren<Collider>(true))
        {
            colStates.Add(new ColState { col = col, enabled = col.enabled });
            col.enabled = false;
        }

        // ย้ายเลเยอร์ทั้ง hierarchy ไปเลเยอร์ holdItem (ถ้ามี)
        CacheAndSetLayerRecursive(HeldObject.transform, holdLayer);

        // ผูกกับ holdPoint หน้า player
        HeldObject.transform.SetParent(holdPoint, true);
        HeldObject.transform.position = holdPoint.position;
        HeldObject.transform.rotation = holdPoint.rotation;

        targetLocalRot = HeldObject.transform.localRotation;
    }

    void Drop()
    {
        if (HeldObject == null) return;

        // ปลดจาก holdPoint
        HeldObject.transform.SetParent(originalParent, true);

        // คืนเลเยอร์เดิม
        RestoreLayers();

        // คืน collider เดิม
        foreach (var s in colStates)
        {
            if (s.col) s.col.enabled = s.enabled;
        }
        colStates.Clear();

        // คืนค่าฟิสิกส์เดิม
        if (heldRb != null)
        {
            heldRb.isKinematic = prevKinematic;
            heldRb.useGravity = prevUseGravity;
            heldRb.detectCollisions = prevDetectCollisions;
            heldRb.interpolation = prevInterp;
            heldRb.collisionDetectionMode = prevCdm;
        }

        HeldObject = null;
        heldRb = null;
        originalParent = null;
        layerStates.Clear();
    }

    void HandleHoldRotation()
    {
        if (HeldObject == null) return;

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0005f)
        {
            targetLocalRot = Quaternion.AngleAxis(wheel * scrollYawSpeed, Vector3.up) * targetLocalRot;
        }
    }

    #endregion

    #region Layer Helpers

    void CacheAndSetLayerRecursive(Transform root, int newLayer)
    {
        layerStates.Clear();

        if (newLayer < 0)
        {
            // ไม่มีเลเยอร์ holdItem ให้เปลี่ยน ก็แค่ cache เฉย ๆ
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                layerStates.Add(new LayerState { t = t, layer = t.gameObject.layer });
            }
            return;
        }

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            layerStates.Add(new LayerState { t = t, layer = t.gameObject.layer });
            t.gameObject.layer = newLayer;
        }
    }

    void RestoreLayers()
    {
        foreach (var s in layerStates)
        {
            if (s.t) s.t.gameObject.layer = s.layer;
        }
    }

    #endregion
}
