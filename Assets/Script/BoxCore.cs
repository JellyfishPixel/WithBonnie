using UnityEngine;

public enum BoxStep
{
    Empty,
    ItemInside,
    BubbleDone,
    Closed,
    Taped,      // ✅ เทปเรียบร้อย
    Labeled     // ✅ แปะลาเบลแล้ว (กล่องพร้อมยก)
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class BoxCore : MonoBehaviour
{
    [Header("Item Detection")]
    public string pickableTag = "pickable";   // แท็กของของข้างในกล่อง
    public Collider itemArea;

    [Header("Pickup Settings")]
    [Tooltip("แท็กที่จะใช้กับกล่อง เมื่อพร้อมให้ผู้เล่นยกได้")]
    public string boxPickupTag = "pickable";

    public static BoxCore Current { get; private set; }

    [Header("Lids")]
    public SmoothLidClose leftLid;
    public SmoothLidClose rightLid;

    [Header("Debug (Read Only)")]
    [SerializeField] private BoxStep step = BoxStep.Empty;
    [SerializeField] private bool hasItem = false;
    [SerializeField] private bool bubbleFull = false;
    [SerializeField] private bool lidsClosed = false;
    [SerializeField] private bool tapeDone = false;
    [SerializeField] private bool labelDone = false;

    public BoxStep Step => step;
    public bool HasItem => hasItem;
    public bool BubbleFull => bubbleFull;
    public bool LidsClosed => lidsClosed;
    public bool TapeDone => tapeDone;
    public bool LabelDone => labelDone;

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
        if (Current == this)
            Current = null;
    }

    public void SetAsCurrent()
    {
        Current = this;
    }

    void Update()
    {
        // อัปเดตฝาปิด
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
        {
            if (col.CompareTag(pickableTag))
                return true;
        }
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

    /// <summary>
    /// เรียกจาก TapeDragScaler เมื่อเทปทำเสร็จ
    /// </summary>
    public void NotifyTapeDone()
    {
        tapeDone = true;
        if (step < BoxStep.Taped)
            step = BoxStep.Taped;

        Debug.Log("[BoxCore] Tape done.");
    }

    /// <summary>
    /// เรียกจากสคริปต์ลาเบล เมื่อแปะลาเบลเสร็จ
    /// </summary>
    public void NotifyLabelPasted()
    {
        labelDone = true;
        if (step < BoxStep.Labeled)
            step = BoxStep.Labeled;

        Debug.Log("[BoxCore] Label pasted → box is now pickable.");

        MakeBoxPickable();
    }

    /// <summary>
    /// ให้กล่องพร้อมถูกยก: เปิดฟิสิกส์ + เปลี่ยน tag
    /// </summary>
    void MakeBoxPickable()
    {
        // เปิดฟิสิกส์ให้กล่องตก/ถูกยกได้
        rb.isKinematic = false;
        rb.useGravity = true;

        // เปลี่ยนแท็กให้ไปอยู่ในระบบ pickup ตามที่คุณใช้
        if (!string.IsNullOrEmpty(boxPickupTag))
            gameObject.tag = boxPickupTag;

        // ถ้าคุณมีระบบ BoxSpawner ที่ต้อง spawn กล่องใหม่ สามารถไปเคลียร์ flag ตรงนั้นเพิ่มได้
        // เช่น:
        // var spawner = FindAnyObjectByType<BoxSpawner>();
        // if (spawner) spawner.hasSpawnedBox = false;
    }

    #endregion
}
