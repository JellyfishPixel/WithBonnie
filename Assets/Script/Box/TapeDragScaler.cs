using UnityEngine;

public class TapeDragScaler : MonoBehaviour
{
    public Transform tapeStart;
    public Transform tapeEnd;
    public GameObject tapeObject;

    [Header("Drag")]
    public float dragTolerance = 0.2f;
    public float startDragThreshold = 0.12f;

    [Header("Pivot")]
    public bool pivotAtCenter = false;

    private bool isDragging = false;
    private bool tapeVisible = false;


    private float lastWorldLength = 0f;
    private float currentWorldLength = 0f;

    private Vector3 dragStartPoint;
    [SerializeField] private TapeDispenser selectedDispenser = null;

    public bool isTapeDone;
    [SerializeField] private PlayerInteractionSystem interactionSystem;


    private Vector3 baseLocalScale;
    private Transform parentForScale;

    BoxCore currentBox;

    public GameObject cube;
    public void SelectDispenser(TapeDispenser dispenser)
    {
        if (dispenser == null) return;

        var eco = EconomyManager.Instance;
        if (eco != null && !eco.HasTapeUse(dispenser.tapeColor))
        {
            Debug.Log("[TapeDragScaler] No tape left for this color.");
            AddSalesPopupUI.ShowMessage("No tape left.\nPlease buy more tape rolls at the shop.");
            selectedDispenser = null;
            cube.SetActive(false);
            return;
        }

        selectedDispenser = dispenser;
        Debug.Log($"[TapeDragScaler] Dispenser selected: {dispenser.name}");

        if (currentBox != null && currentBox.IsFinsihedClose)
            cube.SetActive(true);
    }

    bool HasSelectedDispenser()
    {
        return selectedDispenser != null;
    }


    void Start()
    {
        if (!tapeObject) { enabled = false; return; }
        if (!interactionSystem)
            interactionSystem = FindAnyObjectByType<PlayerInteractionSystem>();

        if (!interactionSystem)
        {
            Debug.LogError("[TapeDragScaler] PlayerInteractionSystem not found");
            enabled = false;
            return;
        }
        baseLocalScale = tapeObject.transform.localScale;
        parentForScale = tapeObject.transform.parent;

        tapeObject.SetActive(false);
        SetTapeScaleWorld(0f);
        cube.SetActive(false);
        currentBox = FindAnyObjectByType<BoxCore>();
    }

    void Update()
    {
        if (!currentBox) return;

        // กล่องต้องปิดฝาก่อนถึงจะใช้เทปได้
        if (!currentBox.IsFinsihedClose) return;

        if (interactionSystem.IsMovementLocked())
            return;

        // ---- คลิกเลือก TapeDispenser ----
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = interactionSystem.GetCurrentCamera();
            if (!cam) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                var dispenser = hit.collider.GetComponent<TapeDispenser>();
                if (dispenser != null)
                {
                    // ให้ไปผ่านฟังก์ชันที่เช็คสต็อกแล้ว
                    SelectDispenser(dispenser);
                    // ถ้าเทปหมด SelectDispenser จะไม่เซ็ต selectedDispenser
                }
            }
        }

        // ---- บังคับต้องเลือกสีเทปก่อนถึงจะเริ่มลากได้ ----
        if (!HasSelectedDispenser())
        {
            // ผู้เล่นพยายามจะเริ่มลากโดยไม่เลือกสี → เตือนครั้งที่คลิก
            if (Input.GetMouseButtonDown(0))
            {
                AddSalesPopupUI.ShowMessage("Please select tape color before taping.");
            }
            return;
        }

        // ---- ด้านล่างคือตัว logic เดิมสำหรับลากเทป ----

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = GetMouseWorldPositionAtY(tapeStart.position.y);

            Vector3 guideDir = (tapeEnd.position - tapeStart.position).normalized;
            Vector3 tip = tapeStart.position + guideDir * lastWorldLength;

            if (Vector3.Distance(mouseWorld, tip) < dragTolerance)
            {
                isDragging = true;
                tapeVisible = false;
                dragStartPoint = mouseWorld;
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 mouseWorld = GetMouseWorldPositionAtY(tapeStart.position.y);

            Vector3 guideVec = (tapeEnd.position - tapeStart.position);
            float guideLen = guideVec.magnitude;
            Vector3 guideDir = guideVec.normalized;

            Vector3 tip = tapeStart.position + guideDir * lastWorldLength;
            float dragDist = Vector3.Dot((mouseWorld - tip), guideDir);

            if (!tapeVisible && dragDist > startDragThreshold)
            {
                tapeObject.SetActive(true);
                tapeVisible = true;

                // ใส่วัสดุจาก dispenser ที่เลือก (ตอนนี้มั่นใจว่าไม่ null แล้ว)
                var mat = selectedDispenser.GetMaterial();
                var r = tapeObject.GetComponentInChildren<Renderer>();
                if (r && mat) r.material = mat;
            }

            if (tapeVisible)
            {
                float projected = Vector3.Dot((mouseWorld - tapeStart.position), guideDir);
                float newLen = Mathf.Clamp(projected, 0f, guideLen);
                newLen = Mathf.Max(newLen, lastWorldLength);

                SetTapeScaleWorld(newLen);
            }
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            if (tapeVisible)
            {
                lastWorldLength = currentWorldLength;
            }
            isDragging = false;
            tapeVisible = false;

            if (lastWorldLength == 0f) tapeObject.SetActive(false);
            if (lastWorldLength > 0f)
            {
                isTapeDone = true;

                // หัก 1 ครั้งจากสีที่เลือก (ตอนนี้มั่นใจว่า selectedDispenser != null แล้ว)
                if (selectedDispenser != null && EconomyManager.Instance != null)
                {
                    var eco = EconomyManager.Instance;
                    bool ok = eco.TryConsumeTapeUse(selectedDispenser.tapeColor);
                    if (!ok)
                    {
                        Debug.LogWarning("[TapeDragScaler] Tape finished but no stock left in eco.");
                    }
                }

                if (BoxCore.Current != null)
                    BoxCore.Current.NotifyTapeDone();

                GameObject.Destroy(cube);
            }
        }
    }




    /// <summary>
    /// เซ็ตความยาวเทปด้วยหน่วย "โลกจริง" และคงความหนา/ความกว้างตาม baseLocalScale
    /// </summary>
    void SetTapeScaleWorld(float worldLength)
    {
        currentWorldLength = worldLength;

        Vector3 dir = (tapeEnd.position - tapeStart.position).normalized;

        // หมุนให้แกน +X ของเทปชี้ไปทางปลาย
        tapeObject.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);

        // แปลง worldLength -> localScale.x โดยชดเชยสเกลของพาเรนต์
        float parentX = (parentForScale != null) ? parentForScale.lossyScale.x : 1f;
        float localX = worldLength / Mathf.Max(0.0001f, parentX);

        // ล็อค Y/Z ให้เท่ากับสเกลตั้งต้นเสมอ (กันความหนา/กว้างเพี้ยน)
        Vector3 s = baseLocalScale;
        s.x = localX;
        tapeObject.transform.localScale = s;

        // วางตำแหน่ง: pivot ที่ปลายเริ่ม หรือกึ่งกลาง
        if (pivotAtCenter)
            tapeObject.transform.position = tapeStart.position + dir * (worldLength * 0.5f);
        else
            tapeObject.transform.position = tapeStart.position;
    }

    Vector3 GetMouseWorldPositionAtY(float yLevel)
    {
        Camera cam = interactionSystem.GetCurrentCamera();
        if (!cam) return tapeStart.position;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, yLevel, 0));
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return tapeStart.position;
    }

}
