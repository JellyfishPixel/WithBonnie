using UnityEngine;

public enum BoxStep
{
    Empty,
    ItemInside,
    BubbleDone,
    Closed,
    Taped,
    Labeled
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class BoxCore : MonoBehaviour
{
    [Header("Item Detection")]
    public string pickableTag = "pickable";
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
    [SerializeField] private bool bubbleStarted = false;

    public BoxStep Step => step;
    public bool HasItem => hasItem;
    public bool BubbleFull => bubbleFull;
    public bool LidsClosed => lidsClosed;
    public bool TapeDone => tapeDone;
    public bool LabelDone => labelDone;

    public bool IsFinsihedClose => lidsClosed;

    Rigidbody rb;

    [Header("Box Type")]
    public BoxKind boxType = BoxKind.Small;

    [Header("FALL DAMAGE (BOX)")]
    [Tooltip("ตัวหารดาเมจเวลาตกทั้งกล่อง (2=ครึ่ง)")]
    public int boxDamageDivisor = 2;

    [Header("COLD BOX")]
    public bool isColdBox = false;   // กล่องเย็นไหม

    [SerializeField] private DeliveryItemData currentItemData;
    [SerializeField] private DeliveryItemInstance currentItemInstance;

    public DeliveryItemData CurrentItemData => currentItemData;
    public DeliveryItemInstance CurrentItemInstance => currentItemInstance;

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
        UpdateBoxTag();
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

    // ========= ดาเมจตอนกล่องตก =========
    void OnCollisionEnter(Collision collision)
    {
        if (!currentItemInstance) return;
        if (step < BoxStep.Closed) return; // คิดเฉพาะปิดกล่องแล้ว

        float v = collision.relativeVelocity.magnitude;
        float g = 9.81f;
        float approxHeight = (v * v) / (2f * g);

        // กล่องช่วยลดดาเมจ → divisor เช่น 2 = ครึ่งหนึ่ง
        currentItemInstance.ApplyFallHeight(approxHeight, Mathf.Max(1, boxDamageDivisor));
    }

    // ========= จัดการ TAG ตามสถานะ =========
    void UpdateBoxTag()
    {

        if (labelDone || step == BoxStep.Labeled)
        {
            if (!string.IsNullOrEmpty(boxPickupTag))
                gameObject.tag = boxPickupTag;
            return;
        }
        if (bubbleStarted || bubbleFull || step >= BoxStep.BubbleDone)
        {
            gameObject.tag = "Box";
            return;
        }

    }

    // ========= ตรวจของเข้า/ออกกล่อง =========
    void OnTriggerEnter(Collider other)
    {
        if (!itemArea) return;
        if (!other.CompareTag(pickableTag)) return;

        hasItem = true;
        if (step < BoxStep.ItemInside)
            step = BoxStep.ItemInside;

        var itemInst = other.GetComponentInParent<DeliveryItemInstance>();
        if (itemInst && itemInst.data)
        {
            if (!CanAccept(itemInst.data))
            {
                Debug.LogWarning($"[BoxCore] Item {itemInst.data.itemName} ใช้กับกล่อง {boxType} ไม่ได้");
                return;
            }

            currentItemInstance = itemInst;
            currentItemData = itemInst.data;
            // ❌ ไม่ต้องเปลี่ยน tag เป็น "Box" ตรงนี้แล้ว
        }

        UpdateBoxTag();
    }

    public bool CanAccept(DeliveryItemData data)
    {
        if (data == null || data.allowedBoxTypes == null || data.allowedBoxTypes.Length == 0)
            return true;

        foreach (var allowed in data.allowedBoxTypes)
            if (allowed == boxType) return true;

        return false;
    }

    void OnTriggerExit(Collider other)
    {
        if (!itemArea) return;
        if (other.CompareTag(pickableTag))
        {
            hasItem = IsPickableStillInside();

            // ถ้าไม่มีของแล้ว และยัง "ไม่เคย" ใส่บับเบิล → reset เป็น Empty
            // ถ้าเคยใส่บับเบิลแล้ว (step >= BubbleDone) → ห้ามย้อนกลับไป Empty
            if (!hasItem && step == BoxStep.ItemInside)
            {
                step = BoxStep.Empty;
                currentItemInstance = null;
                currentItemData = null;
            }

            UpdateBoxTag();
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

    // ========= แพ็คของเข้ากล่อง (หลังแปะลาเบล) =========
    void PackItemsIntoBox()
    {
        if (!itemArea) return;

        Bounds b = itemArea.bounds;
        Collider[] contents = Physics.OverlapBox(b.center, b.extents, Quaternion.identity);

        foreach (var col in contents)
        {
            if (!col.CompareTag(pickableTag))
                continue;

            col.transform.SetParent(this.transform, true);

            var itemRb = col.attachedRigidbody;
            if (itemRb)
            {
                itemRb.isKinematic = true;
                itemRb.useGravity = false;
            }

            foreach (var r in col.GetComponentsInChildren<Renderer>())
                r.enabled = false;

            foreach (var c in col.GetComponentsInChildren<Collider>())
            {
                if (c != itemArea)
                    c.enabled = false;
            }

            Debug.Log($"[BoxCore] Packed item into box: {col.name}");
        }
    }

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
    public void NotifyBubbleStarted()
    {
        bubbleStarted = true;
        UpdateBoxTag(); 
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

    public void NotifyTapeDone()
    {
        tapeDone = true;
        if (step < BoxStep.Taped)
            step = BoxStep.Taped;

        Debug.Log("[BoxCore] Tape done.");
        // ยังไม่ต้องเปลี่ยน tag เพราะใช้กฎเดียวกับ BubbleDone (ห้ามยก)
        UpdateBoxTag();
    }

    public void NotifyLabelPasted()
    {
        labelDone = true;
        if (step < BoxStep.Labeled)
            step = BoxStep.Labeled;

        Debug.Log("[BoxCore] Label pasted → box is now pickable.");
        PackItemsIntoBox();
        MakeBoxPickable();   // จะตั้ง tag = boxPickupTag
    }

    void MakeBoxPickable()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        if (!string.IsNullOrEmpty(boxPickupTag))
            gameObject.tag = boxPickupTag;
    }
}
