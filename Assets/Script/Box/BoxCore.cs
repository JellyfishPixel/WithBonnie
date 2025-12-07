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
    public NPC ownerNPC;

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
    public BubbleType BubbleType => bubbleType;
    public bool HasIceBubble => bubbleType == BubbleType.Ice;
    [Header("WATER PROTECTION")]
    public bool isWaterproofBox = false;
    public bool IsWaterproof => isWaterproofBox;

    Rigidbody rb;

    [Header("Box Type")]
    public BoxKind boxType = BoxKind.Small;

    [Header("FALL DAMAGE (BOX)")]
    [Tooltip("ตัวหารดาเมจเวลาตกทั้งกล่อง (2=ครึ่ง)")]
    public int boxDamageDivisor = 2;
    [Header("Protection (Box + Bubble)")]
    [Tooltip("ตัวหารดาเมจรวมจากกล่อง + บับเบิล (1 = ไม่เซฟเลย)")]
    [SerializeField] private int totalDamageDivisor = 1;

    [Tooltip("เปอร์เซ็นต์การเซฟดาเมจโดยรวม (0–100%)")]
    [SerializeField, Range(0f, 100f)] private float protectionPercent = 0f;

    [Header("COLD BOX")]
    public bool isColdBox = false;   // กล่องเย็นไหม

    [Header("Bubble Protection")]
    [Tooltip("ประเภทบับเบิลที่ใช้กับกล่องนี้")]
    public BubbleType bubbleType = BubbleType.Basic;
    [Header("Bubble Effects")]
    [Tooltip("บับเบิลน้ำแข็งถูกใช้กับกล่องนี้หรือไม่ (ให้เครื่องบับเบิลเป็นคนตั้งค่า)")]
    public bool hasIceBubble = false;


    [Tooltip("บับเบิลน้ำแข็ง: เพิ่มเวลา deadline ได้ ถ้ากล่องนี้เป็นกล่องเย็น")]
    public int extraDeadlineDaysWithIce = 1;   // ไว้ให้ GameManager ใช้ตอนคิดเวลา

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

    private void Start()
    {
        ownerNPC = FindFirstObjectByType<NPC>();
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

    void OnCollisionEnter(Collision collision)
    {
        if (!currentItemInstance) return;
        if (step < BoxStep.Closed) return; // คิดเฉพาะปิดกล่องแล้ว

        float v = collision.relativeVelocity.magnitude;
        float g = 9.81f;
        float approxHeight = (v * v) / (2f * g);

        int divisor = GetTotalDamageDivisor();

        // เพิ่มการป้องกันตามประเภทบับเบิล
        switch (bubbleType)
        {
            case BubbleType.Basic:
                divisor *= 2;  // ประมาณโดนดาเมจครึ่งหนึ่ง
                break;
            case BubbleType.Strong:
                divisor *= 3;  // ประมาณโดนดาเมจ 1/3
                break;
            case BubbleType.Ice:
                divisor *= 3;  // เท่า Strong แต่มีผลพิเศษกับเวลา (ไปทำใน GameManager)
                break;
        }

        currentItemInstance.ApplyFallHeight(approxHeight, divisor);
    }

    public int GetTotalDamageDivisor()
    {
        int divisor = Mathf.Max(1, boxDamageDivisor);

        switch (bubbleType)
        {
            case BubbleType.Basic:
                divisor *= 2;
                break;
            case BubbleType.Strong:
            case BubbleType.Ice:
                divisor *= 3;
                break;

            default:
                break;
        }

        return Mathf.Max(1, divisor);
    }

    public float GetProtection01()
    {
        int d = GetTotalDamageDivisor();
        if (d <= 1) return 0f;
        return 1f - 1f / (float)d;
    }

    // เรียกทุกครั้งที่ค่ากล่อง/บับเบิลเปลี่ยน เพื่ออัปเดต debug
    public void RecalculateProtectionDebug()
    {
        totalDamageDivisor = GetTotalDamageDivisor();
        protectionPercent = GetProtection01() * 100f;
    }

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

    public bool CanUseBubbleType(BubbleType type)
    {
        if (type == BubbleType.Basic) return false;

        if (!HasItem)
        {
            Debug.Log("[BoxCore] ยังไม่มีของในกล่อง ใส่บับเบิลไม่ได้");
            return false;
        }

        // Ice bubble ใช้ได้กับกล่องเย็นเท่านั้น
        if (type == BubbleType.Ice && !isColdBox)
        {
            Debug.Log("[BoxCore] Ice bubble ใช้ได้เฉพาะ ColdBox เท่านั้น");
            return false;
        }

        // ขั้นตอนกล่อง: ต้องอยู่ในช่วงใส่บับเบิลได้
        if (!CanAddBubble())
            return false;

        return true;
    }

    public void ApplyBubbleType(BubbleType type)
    {
        bubbleType = type;
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
        LabelSpawner.Instance?.PrintLabel();
    }

    public void NotifyLabelPasted()
    {
        labelDone = true;
        if (step < BoxStep.Labeled)
            step = BoxStep.Labeled;
        

        PackItemsIntoBox();
        MakeBoxPickable();
        if (ownerNPC != null)
            ownerNPC.HandleBoxStored();

        if (GameManager.Instance != null && currentItemInstance != null)
        {
            GameManager.Instance.RegisterNewDelivery(this, currentItemInstance);
        }
    }


    void MakeBoxPickable()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        if (!string.IsNullOrEmpty(boxPickupTag))
            gameObject.tag = boxPickupTag;
    }
}
