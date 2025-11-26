using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ReceiptItem))]
public class ReceiptLabelSnap : MonoBehaviour, IInteractable
{
    [Header("Snap Preview")]
    public float previewDetectRadius = 0.12f;
    public bool snapAlwaysHorizontal = true;
    public Vector3 placementEulerOffset = Vector3.zero;

    [Header("Pick / Hold")]
    public float maxPickDistance = 3.5f;
    public string pickupTag = "Reciept";   // แท็กของลาเบล

    // runtime refs (ได้จาก PlayerInteractionSystem ตอน Interact)
    Camera cam;
    Transform holdPoint;
    Collider playerCollider;
    PlayerInteractionSystem holder;

    // receipt data
    ReceiptItem receipt;
    Rigidbody rb;
    readonly List<Collider> heldCols = new();

    bool isHeld = false;

    // preview state
    bool snappingPreview;
    SnapArea previewArea;
    SnapArea lastPreviewArea;
    Vector3 previewWorld;
    Quaternion previewRot;

    void Awake()
    {
        receipt = GetComponent<ReceiptItem>();
        rb = GetComponent<Rigidbody>();

        if (!receipt)
            Debug.LogError("[ReceiptLabelSnap] ต้องมี ReceiptItem อยู่บน object เดียวกัน");
    }

    void Update()
    {
        if (!isHeld) return;

        // อัปเดตกล่องปัจจุบันเผื่อเปลี่ยน
        var currentBox = BoxCore.Current;
        if (!currentBox)
        {
            CancelHold();
            return;
        }

        // อัปเดต snap preview
        UpdateSnapPreview();

        // ถ้าไม่ได้ snap กับพื้นที่วาง → ให้ใบเสร็จลอยที่ holdPoint
        if (!snappingPreview && holdPoint && cam)
        {
            transform.position = holdPoint.position;
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }

        // กดคลิกซ้ายเพื่อวางจริง เมื่ออยู่ใน preview ที่ valid
        if (Input.GetMouseButtonDown(0) && snappingPreview && previewArea != null)
        {
            FinalizePlace(previewArea, previewWorld, previewRot);

            // แจ้ง BoxCore ว่ามีลาเบลแล้ว (ถ้าคุณมี method นี้)
           // currentBox.NotifyLabelPasted();

            isHeld = false;
            holder = null;
        }
    }

    // =====================================================================
    // IInteractable: กด E ที่ลาเบล เพื่อ "เริ่มถือ"
    // =====================================================================
    public void Interact(PlayerInteractionSystem interactor)
    {
        if (isHeld)
        {
            // ถ้าต้องการให้กด E อีกครั้งเป็นการยกเลิกถือ → ใส่ CancelHold(); ตรงนี้ได้
            return;
        }

        // ต้องมี BoxCore ปัจจุบัน
        var currentBox = BoxCore.Current;
        if (!currentBox)
        {
            Debug.Log("ยังไม่มีกล่องปัจจุบันสำหรับแปะใบเสร็จ");
            return;
        }

        // ต้องปิดฝากล่อง + เทปเสร็จ (คุณจะผ่อนเงื่อนไขทีหลังได้)
        var tape = currentBox.GetComponentInChildren<TapeDragScaler>();
        if (!tape || !tape.isTapeDone)
        {
            Debug.Log("ยังลากเทปปิดกล่องไม่เสร็จ แปะลาเบลไม่ได้");
            return;
        }

        // เช็คแท็กเพื่อให้แน่ใจว่าเป็นใบเสร็จจริง ๆ
        if (!string.IsNullOrEmpty(pickupTag) && !CompareTag(pickupTag))
        {
            Debug.Log("object นี้ไม่ได้เป็นแท็ก Reciept");
            return;
        }

        // ถ้าแปะอยู่แล้ว (มี SnapArea เป็น parent) ไม่ให้หยิบออก
        if (transform.GetComponentInParent<SnapArea>() != null)
        {
            Debug.Log("ใบเสร็จแปะอยู่แล้ว หยิบออกไม่ได้");
            return;
        }

        // ตั้งค่าจาก PlayerInteractionSystem
        holder = interactor;
        cam = interactor.playerCamera ?? Camera.main;
        holdPoint = interactor.holdPoint;
        playerCollider = interactor.GetComponent<Collider>();

        BeginHold();
    }

    void BeginHold()
    {
        isHeld = true;

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = true;
        }

        // ignore collision กับตัว player
        heldCols.Clear();
        GetComponentsInChildren(true, heldCols);
        if (playerCollider)
        {
            for (int i = 0; i < heldCols.Count; i++)
                Physics.IgnoreCollision(heldCols[i], playerCollider, true);
        }
    }

    void CancelHold()
    {
        if (!isHeld) return;

        if (playerCollider)
        {
            heldCols.Clear();
            GetComponentsInChildren(true, heldCols);
            for (int i = 0; i < heldCols.Count; i++)
                Physics.IgnoreCollision(heldCols[i], playerCollider, false);
        }

        isHeld = false;
        holder = null;
        snappingPreview = false;
        previewArea = null;

        if (lastPreviewArea)
        {
            lastPreviewArea.ShowGrid(false);
            lastPreviewArea = null;
        }
    }

    // =====================================================================
    // SNAP PREVIEW LOGIC (ย้ายมาจากสคริปต์เก่าแทบทั้งดุ้น)
    // =====================================================================
    void UpdateSnapPreview()
    {
        snappingPreview = false;
        previewArea = null;
        if (!isHeld) return;

        var hits = Physics.OverlapSphere(
            transform.position,
            previewDetectRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            lastPreviewArea = null;
            return;
        }

        float bestDist = float.MaxValue;
        SnapArea bestArea = null;
        BoxCollider bestCol = null;

        foreach (var c in hits)
        {
            var a = c.GetComponentInParent<SnapArea>();
            if (!a) continue;

            var box = a.area ? a.area : a.GetComponent<BoxCollider>();
            if (!box) continue;

            float d = (transform.position - box.bounds.center).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestArea = a;
                bestCol = box;
            }
        }

        if (!bestArea || !bestCol)
        {
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            lastPreviewArea = null;
            return;
        }

        if (!cam)
        {
            Debug.LogWarning("[ReceiptLabelSnap] cam is null.");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 samplePoint;

        if (bestCol.Raycast(ray, out var hitOnArea, maxPickDistance))
        {
            samplePoint = hitOnArea.point;
        }
        else
        {
            var t = bestCol.transform;
            Plane topPlane = new Plane(t.up, t.TransformPoint(new Vector3(0, bestCol.size.y * 0.5f, 0)));
            if (!topPlane.Raycast(ray, out float enter))
            {
                if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
                lastPreviewArea = null;
                return;
            }
            samplePoint = ray.GetPoint(enter);
        }

        Transform tf = bestCol.transform;
        Vector3 local = tf.InverseTransformPoint(samplePoint);
        Vector3 half = bestCol.bounds.extents;

        local.y = +half.y; // ผิวบน

        float mx = bestArea.margin + receipt.halfSizeXZ.x;
        float mz = bestArea.margin + receipt.halfSizeXZ.y;

        local.x = Mathf.Clamp(local.x, -half.x + mx, half.x - mx);
        local.z = Mathf.Clamp(local.z, -half.z + mz, half.z - mz);

        if (bestArea.gridStep > 0.0001f)
        {
            local.x = Mathf.Round(local.x / bestArea.gridStep) * bestArea.gridStep;
            local.z = Mathf.Round(local.z / bestArea.gridStep) * bestArea.gridStep;
        }

        previewWorld = tf.TransformPoint(local + new Vector3(0, receipt.surfaceOffset, 0));
        Quaternion baseRot = snapAlwaysHorizontal
            ? Quaternion.LookRotation(tf.forward, tf.up)
            : transform.rotation;
        previewRot = baseRot * Quaternion.Euler(placementEulerOffset);

        transform.SetPositionAndRotation(previewWorld, previewRot);

        snappingPreview = true;
        previewArea = bestArea;

        if (lastPreviewArea != previewArea)
        {
            if (lastPreviewArea) lastPreviewArea.ShowGrid(false);
            previewArea.ShowGrid(true);
            lastPreviewArea = previewArea;
        }
    }

    // =====================================================================
    // PLACE / FINALIZE
    // =====================================================================
    void FinalizePlace(SnapArea area, Vector3 worldPos, Quaternion rot)
    {
        var box = area.area ? area.area : area.GetComponent<BoxCollider>();
        if (!box) return;

        transform.SetPositionAndRotation(worldPos, rot);
        transform.SetParent(area.transform, true);

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (playerCollider)
        {
            heldCols.Clear();
            GetComponentsInChildren(true, heldCols);
            for (int i = 0; i < heldCols.Count; i++)
                Physics.IgnoreCollision(heldCols[i], playerCollider, false);
        }

        area.ShowGrid(false);
        if (lastPreviewArea && lastPreviewArea != area)
            lastPreviewArea.ShowGrid(false);

        snappingPreview = false;
        previewArea = null;
        lastPreviewArea = null;
        heldCols.Clear();
    }
}
