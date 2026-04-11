using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TapeDragScaler : MonoBehaviour
{
    bool isTutorialPlaying = false;
    Coroutine tutorialCo;

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


    public GameObject cube;
    [Header("Tape SFX")]
    [SerializeField] private AudioClip tapeDragLoop;
    [SerializeField] private float tapeVolume = 1f;

    private AudioSource tapeAudio;
    public void SelectDispenser(TapeDispenser dispenser)
    {
        if (dispenser == null) return;

        // 🔥 กล่องที่ Area บอกว่าเป็น current
        var currentBox = BoxWorkArea.Instance.CurrentBox;
        if (!currentBox) return;

        // 🔥 กล่องที่ TapeDragScaler ตัวนี้สังกัดอยู่
        var myBox = GetComponentInParent<BoxCore>();

        // ❌ ถ้าไม่ใช่กล่อง current → ห้ามทำอะไร
        if (myBox != currentBox)
            return;

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

        if (currentBox.IsFinsihedClose)
        {
            cube.SetActive(true);
            StartTapeTutorial();
        }
    }

    void StartTapeTutorial()
    {
        if (isTutorialPlaying) return;

        isTutorialPlaying = true;
        tutorialCo = StartCoroutine(TapeTutorialLoop());
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
        StartCoroutine(FixTapeNextFrame());
        baseLocalScale = tapeObject.transform.localScale;
        parentForScale = tapeObject.transform.parent;
        tapeAudio = gameObject.AddComponent<AudioSource>();
        tapeAudio.loop = true;
        tapeAudio.spatialBlend = 1f; // 3D sound
        tapeAudio.playOnAwake = false;
        tapeAudio.volume = tapeVolume;
        tapeObject.SetActive(false);
        tapeObject.SetActive(false);

        // reset transform แบบนิ่ง
        tapeObject.transform.localPosition = Vector3.zero;
        tapeObject.transform.localRotation = Quaternion.identity;
        tapeObject.transform.localScale = baseLocalScale;
        cube.SetActive(false);
        var box = BoxWorkArea.Instance.CurrentBox;
        box = null;
    }
    IEnumerator FixTapeNextFrame()
    {
        yield return null;

        tapeObject.transform.localScale = baseLocalScale;
    }
    void Update()
    {
        var box = BoxWorkArea.Instance.CurrentBox;
        if (box == null || !box.IsFinsihedClose) return;


        if (interactionSystem.IsMovementLocked())
            return;

        // ===== เลือกสีเทป =====
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
                    SelectDispenser(dispenser);

                    // ▶ เริ่ม tutorial preview หลังเลือกสี
                    StartTapeTutorial();
                    return;
                }
            }
        }


        if (!HasSelectedDispenser())
        {
            //if (Input.GetMouseButtonDown(0))
            //{
            //    AddSalesPopupUI.ShowMessage("Please select tape color before taping.");
            //}
            return;
        }

        // ===== เริ่มลากเทปจริง =====
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = GetMouseWorldPositionAtY(tapeStart.position.y);

            Vector3 guideDir = (tapeEnd.position - tapeStart.position).normalized;
            Vector3 tip = tapeStart.position + guideDir * lastWorldLength;

            if (Vector3.Distance(mouseWorld, tip) < dragTolerance)
            {
                // ❌ หยุด tutorial ทันทีเมื่อผู้เล่นเริ่มลาก
                StopTapeTutorial();

                isDragging = true;
                tapeVisible = false;
                dragStartPoint = mouseWorld;
                if (tapeDragLoop != null)
                {
                    tapeAudio.clip = tapeDragLoop;
                    tapeAudio.Play();
                }
            }
        }

        // ===== ลากเทป =====
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

        // ===== ปล่อยเมาส์ (จบการลาก) =====
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            tapeAudio.Stop();
            if (tapeVisible)
                lastWorldLength = currentWorldLength;

            isDragging = false;
            tapeVisible = false;

            if (lastWorldLength == 0f)
            {
                tapeObject.SetActive(false);
                return;
            }

            // ===== ปิดเทปสำเร็จ =====
            isTapeDone = true;

            if (selectedDispenser != null && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.TryConsumeTapeUse(selectedDispenser.tapeColor);
                var shopUI = FindFirstObjectByType<BoxShopUI>();
                if (shopUI) shopUI.RefreshUI();
            }

            box.NotifyTapeDone();

            // tutorial object ไม่ต้องใช้แล้ว
            cube.SetActive(false);
        }
    }



    IEnumerator TapeTutorialLoop()
    {
        float fullLen = Vector3.Distance(tapeStart.position, tapeEnd.position);
        float speed = 0.8f;

        while (isTutorialPlaying)
        {
            // ลากออก
            yield return AnimateCubeScale(0f, fullLen, speed);
            yield return new WaitForSeconds(0.25f);

            // (ถ้าอยากให้ดูวน) ย่อกลับ
            yield return AnimateCubeScale(fullLen, 0f, speed * 0.6f);
            yield return new WaitForSeconds(0.2f);
        }
    }
    IEnumerator AnimateCubeScale(float from, float to, float duration)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float len = Mathf.Lerp(from, to, t);
            SetCubePreview(len);
            yield return null;
        }

        SetCubePreview(to);
    }
    void SetCubePreview(float worldLength)
    {
        if (!cube) return;

        Vector3 dir = (tapeEnd.position - tapeStart.position).normalized;

        cube.transform.rotation =
            Quaternion.LookRotation(dir) *
            Quaternion.Euler(0, 90, 0);

        float parentX = cube.transform.parent
            ? cube.transform.parent.lossyScale.x
            : 1f;

        Vector3 s = cube.transform.localScale;
        s.x = worldLength / Mathf.Max(0.0001f, parentX);
        cube.transform.localScale = s;

        if (pivotAtCenter)
            cube.transform.position =
                tapeStart.position + dir * (worldLength * 0.5f);
        else
            cube.transform.position = tapeStart.position;
    }
    void StopTapeTutorial()
    {
        if (!isTutorialPlaying) return;

        isTutorialPlaying = false;

        if (tutorialCo != null)
            StopCoroutine(tutorialCo);

        tutorialCo = null;
        cube.SetActive(false);
    }


    /// <summary>
    /// เซ็ตความยาวเทปด้วยหน่วย "โลกจริง" และคงความหนา/ความกว้างตาม baseLocalScale
    /// </summary>
    void SetTapeScaleWorld(float worldLength)
    {
        currentWorldLength = worldLength;

        tapeObject.transform.localScale = baseLocalScale;
        Vector3 dir = (tapeEnd.position - tapeStart.position).normalized;

        // หมุนให้แกน +X ของเทปชี้ไปทางปลาย
        tapeObject.transform.rotation =
    Quaternion.LookRotation(dir) *
    Quaternion.Euler(0, 90, 0);

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
