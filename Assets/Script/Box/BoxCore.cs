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

    [Header("Damage Protection")]
    [Tooltip("1 = แรงเต็ม, 0.3 = กล่องช่วยรับแรงไป 70% ของของข้างใน")]
    public float innerItemDamageMultiplier = 0.3f;

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

    void OnCollisionEnter(Collision collision)
    {
        if (!currentItemInstance) return;

        // ตัวอย่าง: คิดดาเมจเฉพาะกล่องที่ปิดฝาแล้ว
        if (step < BoxStep.Closed) return;

        float impact = collision.relativeVelocity.magnitude;
        currentItemInstance.ApplyImpactFromContainer(impact, innerItemDamageMultiplier);
    }



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
            gameObject.tag = "Box";
        }
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
    }


    public void NotifyLabelPasted()
    {
        labelDone = true;
        if (step < BoxStep.Labeled)
            step = BoxStep.Labeled;

        Debug.Log("[BoxCore] Label pasted → box is now pickable.");
        PackItemsIntoBox();
        MakeBoxPickable();
    }

    void MakeBoxPickable()
    {
       
        rb.isKinematic = false;
        rb.useGravity = true;

        
        if (!string.IsNullOrEmpty(boxPickupTag))
            gameObject.tag = boxPickupTag;

        // ถ้าคุณมีระบบ BoxSpawner ที่ต้อง spawn กล่องใหม่ สามารถไปเคลียร์ flag ตรงนั้นเพิ่มได้
        // เช่น:
        // var spawner = FindAnyObjectByType<BoxSpawner>();
        // if (spawner) spawner.hasSpawnedBox = false;
    }

}
