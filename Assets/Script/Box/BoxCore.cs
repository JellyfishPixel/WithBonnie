using System.Collections;
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
public class BoxCore : MonoBehaviour, IAbandonable
{
    [Header("Item Detection")]
    public string pickableTag = "pickable";
    public Collider itemArea;
    [SerializeField] private bool isAbandoned = false;

    [Header("Pickup Settings")]
    [Tooltip("แท็กที่จะใช้กับกล่อง เมื่อพร้อมให้ผู้เล่นยกได้")]
    public string boxPickupTag = "pickable";

    [SerializeField] private bool labelSpawned = false;

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
    [SerializeField] private AudioClip interactSound;

    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }

    [Header("Delivery Runtime")]
    public bool deliveredSuccessfully = false;

    public void MarkDelivered()
    {
        deliveredSuccessfully = true;
    }


    void Reset()
    {
        itemArea = GetComponent<Collider>();
        if (itemArea) itemArea.isTrigger = true;
    }

    void Awake()
    {

        rb = GetComponent<Rigidbody>();
        if (!itemArea) itemArea = GetComponent<Collider>();
        if (itemArea) itemArea.isTrigger = true;

        //rb.isKinematic = true;
        //rb.useGravity = false;

        step = BoxStep.Empty;
        UpdateBoxTag();
    }

    //public void SetCurrent(bool value)
    //{
    //    if (value)
    //    {
    //        Current = this;
    //    }
    //    else if (Current == this)
    //    {
    //        Current = null;
    //    }
    //}

    private void Start()
    {
        //ownerNPC = FindFirstObjectByType<NPC>();
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
    void OnDestroy()
    {

        if (currentItemInstance == null)
            return;

        var area = FindFirstObjectByType<BoxWorkArea>();
        if (area)
        {
            area.ClearCurrentBox(this);
        }


        if (!deliveredSuccessfully && !isAbandoned)
        {
            int penalty = Mathf.Max(0, currentItemData?.baseReward ?? 0);

            if (penalty > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(-penalty);
            }
            if (currentItemInstance.ownerNPC != null)
            {
                currentItemInstance.ownerNPC.ForceExitAndClearItem();
            }


        }


    }



    void OnCollisionEnter(Collision collision)
    {
        if (!currentItemInstance) return;
        if (step < BoxStep.Closed) return; 

        float v = collision.relativeVelocity.magnitude;
        float g = 9.81f;
        float approxHeight = (v * v) / (2f * g);

        int divisor = GetTotalDamageDivisor();
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

    public void OnAbandoned()
    {
        if (isAbandoned) return;
        if (deliveredSuccessfully) return;

        if (currentItemInstance == null)
        {
            isAbandoned = true;
            Destroy(gameObject);
            return;
        }


        isAbandoned = true;

        if (currentItemData != null && GameManager.Instance != null)
        {
            int penalty = Mathf.Max(0, currentItemData.baseReward);
            GameManager.Instance.AddMoney(-penalty);
        }

        if (currentItemInstance.ownerNPC != null)
        {
            currentItemInstance.ownerNPC.ForceExitAndClearItem();
        }

        Destroy(gameObject);
    }


    void OnTriggerEnter(Collider other)
    {
        if (!itemArea) return;
        if (!other.CompareTag(pickableTag)) return;

        var current = BoxWorkArea.Instance?.CurrentBox;

        if (current != null && current != this)
        {
            AddSalesPopupUI.ShowMessage(
                "Please pack items in the work area box."
            );

            var itemInstReject = other.GetComponentInParent<DeliveryItemInstance>();
            RejectItem(other, itemInstReject);

            return;
        }
        var itemInst = other.GetComponentInParent<DeliveryItemInstance>();
        if (itemInst && itemInst.data)
        {
            if (!CanAccept(itemInst.data))
            {

                AddSalesPopupUI.ShowMessage("Oops! This item doesn’t like this box.");
                RejectItem(other, itemInst);
                return;
            }

            
            hasItem = true;
            if (step < BoxStep.ItemInside)
                step = BoxStep.ItemInside;

            currentItemInstance = itemInst;
            currentItemData = itemInst.data;
        }

        UpdateBoxTag();
    }
    void RejectItem(Collider itemCol, DeliveryItemInstance itemInst)
    {
        Rigidbody rb = itemCol.attachedRigidbody;
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // ถุยออกด้านหน้า
            Vector3 dir = (itemCol.transform.position - transform.position).normalized;
            rb.AddForce(dir * 2.5f, ForceMode.Impulse);
        }

        // กัน trigger loop
        Physics.IgnoreCollision(itemCol, itemArea, true);
        StartCoroutine(ReenableCollision(itemCol));
    }

    IEnumerator ReenableCollision(Collider col)
    {
        yield return new WaitForSeconds(0.3f);
        if (col && itemArea)
            Physics.IgnoreCollision(col, itemArea, false);
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
        if (!hasItem && step == BoxStep.ItemInside)
        {
            step = BoxStep.Empty;
            currentItemInstance = null;
            currentItemData = null;
        }

        if (other.CompareTag(pickableTag))
        {
            hasItem = IsPickableStillInside();

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
    public void ApplyQualityDamage(float rawDamage)
    {
        if (!CurrentItemInstance) return;

        // 🔹 ใช้ระบบ protection เดิมของคุณ
        int divisor = Mathf.Max(1, GetTotalDamageDivisor());
        float finalDmg = rawDamage / divisor;

        float oldQ = CurrentItemInstance.currentQuality;
        CurrentItemInstance.currentQuality =
            Mathf.Clamp(oldQ - finalDmg, 0f, 100f);

        Debug.Log($"[BoxCore] Quality {oldQ:F1} → {CurrentItemInstance.currentQuality:F1} (dmg={finalDmg:F1})");
    }

    public bool CanUseBubbleType(BubbleType type)
    {
        //if (type == BubbleType.Basic) return false;

        if (!HasItem)
        {
            Debug.Log("[BoxCore] ยังไม่มีของในกล่อง ใส่บับเบิลไม่ได้");
            return false;
        }

        if (type == BubbleType.Ice && boxType != BoxKind.ColdBox)
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
        if (tapeDone) return;

        tapeDone = true;

        if (step < BoxStep.Taped)
            step = BoxStep.Taped;
        GuideArrowManager.Instance?.NextTarget();
        if (!labelSpawned)
        {
            labelSpawned = true;
            LabelSpawner.Instance?.PrintLabel(this);
        }

        AddSalesPopupUI.ShowMessage("Get the label and paste it on the box.");
    }


    public void NotifyLabelPasted()
    {
        labelDone = true;
        if (step < BoxStep.Labeled)
            step = BoxStep.Labeled;
        

        PackItemsIntoBox();
        MakeBoxPickable();
        if (currentItemInstance != null &&
            currentItemInstance.ownerNPC != null)
        {
            currentItemInstance.ownerNPC.HandleBoxStored();
        }


        if (GameManager.Instance != null && currentItemInstance != null)
        {
            GameManager.Instance.RegisterNewDelivery(this, currentItemInstance);
        }
    }


    void MakeBoxPickable()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        AddSalesPopupUI.ShowSticky("Holding box then press E to store in inventory.");
        PlayInteractSound();
        if (!string.IsNullOrEmpty(boxPickupTag))
            gameObject.tag = boxPickupTag;
    }

    public bool CheckStepOrWarn(BoxStep requiredStep)
    {
        if (step < requiredStep)
        {
            string msg = GetStepMessage(requiredStep);
            AddSalesPopupUI.ShowMessage(msg);
            return false;
        }

        return true;
    }

    string GetStepMessage(BoxStep required)
    {
        switch (required)
        {
            case BoxStep.ItemInside:
                return "Put item in the box first.";

            case BoxStep.BubbleDone:
                return "Add bubble wrap first.";

            case BoxStep.Closed:
                return "Close the box first.";

            case BoxStep.Taped:
                return "Tape the box first.";

            case BoxStep.Labeled:
                return "Paste the label first.";
        }

        return "Complete previous step first.";
    }
}
