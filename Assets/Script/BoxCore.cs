using UnityEngine;

public enum BoxStep
{
    Empty,
    ItemInside,
    BubbleDone,
    Closed
    // ถ้าจะเพิ่ม Tape / Label / Ready ค่อยต่อได้ทีหลัง
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class BoxCore : MonoBehaviour
{
    [Header("Item Detection")]
    public string pickableTag = "pickable";
    public Collider itemArea;         
    public static BoxCore Current { get; private set; }
    [Header("Lids")]
    public SmoothLidClose leftLid;
    public SmoothLidClose rightLid;

    [Header("Debug (Read Only)")]
    [SerializeField] private BoxStep step = BoxStep.Empty;
    [SerializeField] private bool hasItem = false;
    [SerializeField] private bool bubbleFull = false;
    [SerializeField] private bool lidsClosed = false;

    public BoxStep Step => step;
    public bool HasItem => hasItem;
    public bool BubbleFull => bubbleFull;
    public bool LidsClosed => lidsClosed;

    public bool IsFinsihedClose => lidsClosed;   // เผื่อใช้กับเทปเดิม

    Rigidbody rb;

    void Reset()
    {
        itemArea = GetComponent<Collider>();
        if (itemArea) itemArea.isTrigger = true;
    }

    void Awake()
    {
        Current = this;
        rb = GetComponent<Rigidbody>();
        if (!itemArea) itemArea = GetComponent<Collider>();
        if (itemArea) itemArea.isTrigger = true;

        rb.isKinematic = true;
        rb.useGravity = false;

        step = BoxStep.Empty;
    }

    void OnDestroy()
    {
        // ถ้ากล่องนี้โดนลบ และมันคือ Current อยู่ ให้เคลียร์
        if (Current == this)
            Current = null;
    }

    // หรือถ้าคุณอยากสลับ current box ด้วยตัวเองทีหลัง
    public void SetAsCurrent()
    {
        Current = this;
    }
    void Update()
    {
        if (leftLid && rightLid)
        {
            lidsClosed = leftLid.isClosed && rightLid.isClosed;

            if (lidsClosed && step < BoxStep.Closed)
                step = BoxStep.Closed;
        }
    }

    #region Item detection

    void OnTriggerEnter(Collider other)
    {
        if (!itemArea) return;
        if (other.CompareTag(pickableTag))
        {
            hasItem = true;
            if (step < BoxStep.ItemInside)
                step = BoxStep.ItemInside;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!itemArea) return;
        if (other.CompareTag(pickableTag))
        {
            hasItem = IsPickableStillInside();
            if (!hasItem && step >= BoxStep.ItemInside)
                step = BoxStep.Empty;
        }
    }

    bool IsPickableStillInside()
    {
        Bounds b = itemArea.bounds;
        Collider[] contents = Physics.OverlapBox(b.center, b.extents, Quaternion.identity);
        foreach (var col in contents)
            if (col.CompareTag(pickableTag))
                return true;
        return false;
    }

    #endregion

    #region Step Rules API (ให้สคริปต์อื่นเรียก)

    public bool CanAddBubble()
    {
        if (!hasItem)
        {
            Debug.Log("ยังไม่มีของในกล่อง ใส่บับเบิ้ลไม่ได้");
            return false;
        }
        if (step != BoxStep.ItemInside && step != BoxStep.BubbleDone)
        {
            Debug.Log("สเตปกล่องไม่ถูกต้องสำหรับการใส่บับเบิ้ล");
            return false;
        }
        return true;
    }

    public void NotifyBubbleFull()
    {
        bubbleFull = true;
        if (step < BoxStep.BubbleDone)
            step = BoxStep.BubbleDone;
    }

    public bool CanCloseLid()
    {
        if (!hasItem)
        {
            Debug.Log("ยังไม่มีของในกล่อง ปิดฝาไม่ได้");
            return false;
        }
        if (!bubbleFull)
        {
            Debug.Log("ต้องใส่บับเบิ้ลครบก่อนปิดฝา");
            return false;
        }
        if (step >= BoxStep.Closed)
        {
            Debug.Log("กล่องปิดฝาไปแล้ว");
            return false;
        }
        return true;
    }

    #endregion
}
