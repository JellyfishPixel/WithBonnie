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
    [Header("Identity")]
    public BoxKind boxType = BoxKind.Small;
    public bool isWaterproofBox = false;
    public bool isColdBox = false;
    public string boxPickupTag = "pickable";

    [Header("Input Detection")]
    public string pickableTag = "pickable";
    public Collider itemArea;

    [Header("Lids")]
    public SmoothLidClose leftLid;
    public SmoothLidClose rightLid;

    [Header("Protection")]
    [Min(1)] public int boxDamageDivisor = 2;
    public BubbleType bubbleType = BubbleType.Basic;
    public bool hasIceBubble = false;
    public int extraDeadlineDaysWithIce = 1;

    [Header("Feedback")]
    [SerializeField] AudioClip interactSound;

    [Header("Debug")]
    [SerializeField] BoxStep step = BoxStep.Empty;
    [SerializeField] bool hasItem;
    [SerializeField] bool bubbleStarted;
    [SerializeField] bool bubbleFilled;
    [SerializeField] bool lidsClosed;
    [SerializeField] bool tapeApplied;
    [SerializeField] bool labelApplied;
    [SerializeField] bool labelRequested;
    [SerializeField] bool packedForCarry;
    [SerializeField] bool storedInInventory;
    [SerializeField] bool deliveredSuccessfully;
    [SerializeField] bool abandoned;
    [SerializeField] bool preventInventoryStore;

    [SerializeField] DeliveryItemData currentItemData;
    [SerializeField] DeliveryItemInstance currentItemInstance;
    [SerializeField] GameObject sourceBoxPrefab;
    [SerializeField] GameObject usedLabelPrefab;
    [SerializeField] Material usedTapeMaterial;
    [SerializeField] TapeColor usedTapeColor;
    [SerializeField] bool payloadStoredAsData;

    Rigidbody body;

    public BoxStep Step => step;
    public bool HasItem => hasItem;
    public bool BubbleFull => bubbleFilled;
    public bool LidsClosed => lidsClosed;
    public bool TapeDone => tapeApplied;
    public bool LabelDone => labelApplied;
    public bool IsFinsihedClose => lidsClosed;
    public BubbleType BubbleType => bubbleType;
    public bool HasIceBubble => hasIceBubble;
    public bool IsWaterproof => isWaterproofBox;
    public DeliveryItemData CurrentItemData => currentItemData;
    public DeliveryItemInstance CurrentItemInstance => currentItemInstance;
    public bool PreventInventoryStore => preventInventoryStore;

    void Reset()
    {
        itemArea = GetComponent<Collider>();
        if (itemArea != null)
            itemArea.isTrigger = true;
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (itemArea == null)
            itemArea = GetComponent<Collider>();

        if (itemArea != null)
            itemArea.isTrigger = true;

        EvaluateProgress();
        UpdatePickupTag();
    }

    void Update()
    {
        EvaluateProgress();
    }

    void OnDestroy()
    {
        var workArea = FindFirstObjectByType<BoxWorkArea>();
        if (workArea != null)
            workArea.ClearCurrentBox(this);

        if (ShouldPenalizeOnDestroy())
            ApplyFailurePenalty();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (currentItemInstance == null || step < BoxStep.Closed)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        float estimatedHeight = (impactSpeed * impactSpeed) / (2f * 9.81f);
        currentItemInstance.ApplyFallHeight(estimatedHeight, GetTotalDamageDivisor());
    }

    void OnTriggerEnter(Collider other)
    {
        if (itemArea == null || step >= BoxStep.Closed)
            return;

        if (!other.CompareTag(pickableTag))
            return;

        var activeBox = BoxWorkArea.Instance != null ? BoxWorkArea.Instance.CurrentBox : null;
        if (activeBox != null && activeBox != this)
        {
            AddSalesPopupUI.ShowMessage("Please pack items in the work area box.");
            BounceRejectedItem(other);
            return;
        }

        DeliveryItemInstance item = other.GetComponentInParent<DeliveryItemInstance>();
        if (item == null || item.data == null)
            return;

        if (!CanAccept(item.data))
        {
            AddSalesPopupUI.ShowMessage("Oops! This item doesn't like this box.");
            BounceRejectedItem(other);
            return;
        }

        AssignItem(item);
    }

    void OnTriggerExit(Collider other)
    {
        if (itemArea == null || step >= BoxStep.Closed)
            return;

        if (!other.CompareTag(pickableTag))
            return;

        if (currentItemInstance != null && other.transform.IsChildOf(currentItemInstance.transform))
            ClearCurrentItemIfNeeded();
    }

    void AssignItem(DeliveryItemInstance item)
    {
        currentItemInstance = item;
        currentItemData = item.data;
        hasItem = true;
        EvaluateProgress();
        UpdatePickupTag();
    }

    void ClearCurrentItemIfNeeded()
    {
        bool itemStillInside = currentItemInstance != null && IsColliderInside(itemArea, currentItemInstance.GetComponent<Collider>());
        if (itemStillInside)
            return;

        currentItemInstance = null;
        currentItemData = null;
        hasItem = false;
        bubbleStarted = false;
        bubbleFilled = false;
        EvaluateProgress();
        UpdatePickupTag();
    }

    static bool IsColliderInside(Collider area, Collider target)
    {
        if (area == null || target == null)
            return false;

        return area.bounds.Intersects(target.bounds);
    }

    void BounceRejectedItem(Collider itemCollider)
    {
        Rigidbody itemBody = itemCollider.attachedRigidbody;
        if (itemBody != null)
        {
            Vector3 pushDirection = (itemCollider.transform.position - transform.position).normalized;
            itemBody.linearVelocity = Vector3.zero;
            itemBody.angularVelocity = Vector3.zero;
            itemBody.AddForce(pushDirection * 2.5f, ForceMode.Impulse);
        }

        StartCoroutine(RestoreCollisionAfterReject(itemCollider));
    }

    IEnumerator RestoreCollisionAfterReject(Collider itemCollider)
    {
        if (itemArea != null && itemCollider != null)
            Physics.IgnoreCollision(itemCollider, itemArea, true);

        yield return new WaitForSeconds(0.3f);

        if (itemArea != null && itemCollider != null)
            Physics.IgnoreCollision(itemCollider, itemArea, false);
    }

    void EvaluateProgress()
    {
        hasItem = currentItemData != null && (currentItemInstance != null || payloadStoredAsData);
        lidsClosed = AreLidsClosed();

        if (!hasItem)
        {
            step = BoxStep.Empty;
            return;
        }

        if (!bubbleFilled)
        {
            step = BoxStep.ItemInside;
            return;
        }

        if (!lidsClosed)
        {
            step = BoxStep.BubbleDone;
            return;
        }

        if (!tapeApplied)
        {
            step = BoxStep.Closed;
            return;
        }

        if (!labelApplied)
        {
            step = BoxStep.Taped;
            return;
        }

        step = BoxStep.Labeled;
    }

    bool AreLidsClosed()
    {
        if (leftLid == null || rightLid == null)
            return false;

        return leftLid.isClosed && rightLid.isClosed;
    }

    void UpdatePickupTag()
    {
        if (labelApplied)
        {
            if (!string.IsNullOrEmpty(boxPickupTag))
                gameObject.tag = boxPickupTag;
            return;
        }

        if (bubbleStarted)
            gameObject.tag = "Box";
    }

    bool ShouldPenalizeOnDestroy()
    {
        if (currentItemInstance == null)
            return false;

        if (deliveredSuccessfully || abandoned || storedInInventory)
            return false;

        return true;
    }

    void ApplyFailurePenalty()
    {
        int penalty = Mathf.Max(0, currentItemData != null ? currentItemData.baseReward : 0);
        if (penalty > 0 && GameManager.Instance != null)
            GameManager.Instance.ApplyPenalty(penalty);

        if (currentItemInstance != null && currentItemInstance.ownerNPC != null)
            currentItemInstance.ownerNPC.ForceExitAndClearItem();
    }

    public void MarkDelivered()
    {
        deliveredSuccessfully = true;
    }

    public void MarkStoredInInventory()
    {
        storedInInventory = true;
    }

    public void SetPreventInventoryStore(bool value)
    {
        preventInventoryStore = value;
    }

    public void PrepareForInventoryStorage(Transform storageParent)
    {
        storedInInventory = true;
        payloadStoredAsData = currentItemData != null;
        packedForCarry = payloadStoredAsData;

        StripPackedContents();
        FreezeShellForStorage();

        if (storageParent != null)
            transform.SetParent(storageParent, true);

        gameObject.SetActive(false);
    }

    public void RememberSourcePrefab(GameObject prefab)
    {
        sourceBoxPrefab = prefab;
    }

    public void RememberTape(TapeColor tapeColor, Material tapeMaterial)
    {
        usedTapeColor = tapeColor;
        usedTapeMaterial = tapeMaterial;
    }

    public void RememberLabelPrefab(GameObject labelPrefab)
    {
        usedLabelPrefab = labelPrefab;
    }

    public bool CanAccept(DeliveryItemData data)
    {
        if (data == null || data.allowedBoxTypes == null || data.allowedBoxTypes.Length == 0)
            return true;

        foreach (BoxKind allowed in data.allowedBoxTypes)
        {
            if (allowed == boxType)
                return true;
        }

        return false;
    }

    public bool CanAddBubble()
    {
        return step == BoxStep.ItemInside || step == BoxStep.BubbleDone;
    }

    public bool CanUseBubbleType(BubbleType type)
    {
        if (!hasItem || !CanAddBubble())
            return false;

        if (type == BubbleType.Ice && boxType != BoxKind.ColdBox)
            return false;

        return true;
    }

    public void ApplyBubbleType(BubbleType type)
    {
        bubbleType = type;
        hasIceBubble = type == BubbleType.Ice;
    }

    public void NotifyBubbleStarted()
    {
        bubbleStarted = true;
        EvaluateProgress();
        UpdatePickupTag();
    }

    public void NotifyBubbleFull()
    {
        bubbleStarted = true;
        bubbleFilled = true;
        EvaluateProgress();
        UpdatePickupTag();
    }

    public bool CanCloseLid()
    {
        return hasItem && bubbleFilled && !lidsClosed;
    }

    public void NotifyTapeDone()
    {
        if (tapeApplied)
            return;

        tapeApplied = true;
        EvaluateProgress();
        GuideArrowManager.Instance?.NextTarget();

        if (!labelRequested)
        {
            labelRequested = true;
            LabelSpawner.Instance?.PrintLabel(this);
        }

        AddSalesPopupUI.ShowMessage("Get the label and paste it on the box.");
    }

    public void NotifyLabelPasted()
    {
        if (labelApplied)
            return;

        labelApplied = true;
        SealPackedItem();
        EvaluateProgress();
        MakeCarryReady();

        if (currentItemInstance != null && currentItemInstance.ownerNPC != null)
            currentItemInstance.ownerNPC.HandleBoxStored();
    }

    void SealPackedItem()
    {
        if (packedForCarry || itemArea == null)
            return;

        Collider[] contents = Physics.OverlapBox(itemArea.bounds.center, itemArea.bounds.extents, Quaternion.identity);
        foreach (Collider content in contents)
        {
            if (!content.CompareTag(pickableTag))
                continue;

            content.transform.SetParent(transform, true);

            Rigidbody itemBody = content.attachedRigidbody;
            if (itemBody != null)
            {
                itemBody.isKinematic = true;
                itemBody.useGravity = false;
            }

            foreach (Renderer renderer in content.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            foreach (Collider childCollider in content.GetComponentsInChildren<Collider>(true))
            {
                if (childCollider != itemArea)
                    childCollider.enabled = false;
            }
        }

        packedForCarry = true;
    }

    void MakeCarryReady()
    {
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
        }

        UpdatePickupTag();
        AddSalesPopupUI.ShowSticky("Holding box then press E to store in inventory.");
        PlayInteractSound();
    }

    void PlayInteractSound()
    {
        if (interactSound == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFX(interactSound, transform.position);
    }

    public PackedBoxData CreatePackedData()
    {
        if (currentItemData == null || currentItemInstance == null)
            return null;

        int deadlineDays = currentItemInstance.CalculateEffectiveDeadlineDays(
            currentItemData.deliveryLimitDays,
            isColdBox || boxType == BoxKind.ColdBox,
            hasIceBubble);

        var data = new PackedBoxData
        {
            boxPrefab = sourceBoxPrefab,
            labelPrefab = usedLabelPrefab,
            tapeMaterial = usedTapeMaterial,
            tapeColor = usedTapeColor,
            boxType = boxType,
            bubbleType = bubbleType,
            itemData = currentItemData,
            destinationId = currentItemData.destinationId,
            ownerNPCName = ResolveOwnerName(),
            address = currentItemData.address,
            information = currentItemData.information,
            itemQuality = currentItemInstance.currentQuality,
            remainingDays = deadlineDays,
            protectionDivisor = GetTotalDamageDivisor(),
            protectionPercent = GetProtection01() * 100f,
            isWaterproof = isWaterproofBox,
            hasIceBubble = hasIceBubble
        };

        data.RefreshState();
        return data;
    }

    string ResolveOwnerName()
    {
        if (currentItemInstance != null &&
            currentItemInstance.ownerNPC != null &&
            currentItemInstance.ownerNPC.data != null)
        {
            return currentItemInstance.ownerNPC.data.npcName;
        }

        return "Unknown";
    }

    public void ApplyPackedData(PackedBoxData package, DeliveryItemInstance runtimeItem = null)
    {
        if (package == null)
            return;

        boxType = package.boxType;
        isColdBox = boxType == BoxKind.ColdBox;
        isWaterproofBox = package.isWaterproof;
        sourceBoxPrefab = package.boxPrefab;
        usedLabelPrefab = package.labelPrefab;
        usedTapeMaterial = package.tapeMaterial;
        usedTapeColor = package.tapeColor;
        bubbleType = package.bubbleType;
        hasIceBubble = package.hasIceBubble;
        currentItemData = package.itemData;
        currentItemInstance = runtimeItem;
        payloadStoredAsData = package.itemData != null;

        if (currentItemInstance != null)
        {
            currentItemInstance.data = package.itemData;
            currentItemInstance.currentQuality = package.itemQuality;
            currentItemInstance.isDamaged = package.isDamaged;
            currentItemInstance.isBroken = package.isBroken;
        }

        hasItem = package.itemData != null;
        bubbleStarted = hasItem;
        bubbleFilled = hasItem;
        tapeApplied = hasItem;
        labelApplied = hasItem;
        labelRequested = hasItem;
        packedForCarry = hasItem;
        storedInInventory = false;
        deliveredSuccessfully = false;
        abandoned = false;

        if (hasItem)
        {
            MakeCarryReady();
            RestorePackedVisuals();
        }

        EvaluateProgress();
    }

    public void RestoreStoredShell(PackedBoxData package, Vector3 position, Quaternion rotation)
    {
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);

        ApplyPackedData(package, null);

        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    void RestorePackedVisuals()
    {
        RestoreTapeVisual();
        RestoreLabelVisual();
    }

    void StripPackedContents()
    {
        if (currentItemInstance != null)
        {
            Destroy(currentItemInstance.gameObject);
            currentItemInstance = null;
        }

        var boxBubble = GetComponentInChildren<BoxBubble>(true);
        if (boxBubble != null && boxBubble.bubbleObject != null)
            boxBubble.bubbleObject.SetActive(false);
    }

    void FreezeShellForStorage()
    {
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    void RestoreTapeVisual()
    {
        if (usedTapeMaterial == null)
            return;

        var tape = GetComponentInChildren<TapeDragScaler>(true);
        if (tape == null)
            return;

        tape.RestoreFinishedTape(usedTapeMaterial, usedTapeColor);
    }

    void RestoreLabelVisual()
    {
        if (usedLabelPrefab == null)
            return;

        if (GetComponentInChildren<ReceiptLabelSnap>(true) != null)
            return;

        var snapArea = GetComponentInChildren<SnapArea>(true);
        if (snapArea == null)
            return;

        GameObject label = Instantiate(usedLabelPrefab, snapArea.transform.position, snapArea.transform.rotation, snapArea.transform);
        var rb = label.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var snap = label.GetComponent<ReceiptLabelSnap>();
        if (snap != null)
            snap.enabled = false;

        label.transform.localPosition = Vector3.zero;
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
        }

        return Mathf.Max(1, divisor);
    }

    public float GetProtection01()
    {
        int divisor = GetTotalDamageDivisor();
        return divisor <= 1 ? 0f : 1f - 1f / divisor;
    }

    public void RecalculateProtectionDebug()
    {
        EvaluateProgress();
    }

    public void ApplyQualityDamage(float rawDamage)
    {
        if (currentItemInstance == null)
            return;

        float finalDamage = rawDamage / GetTotalDamageDivisor();
        currentItemInstance.ApplyDamage(finalDamage);
    }

    public void OnAbandoned()
    {
        if (abandoned || deliveredSuccessfully)
            return;

        abandoned = true;

        if (currentItemData != null && GameManager.Instance != null)
            GameManager.Instance.ApplyPenalty(Mathf.Max(0, currentItemData.baseReward));

        if (currentItemInstance != null && currentItemInstance.ownerNPC != null)
            currentItemInstance.ownerNPC.ForceExitAndClearItem();

        Destroy(gameObject);
    }

    public bool CheckStepOrWarn(BoxStep requiredStep)
    {
        EvaluateProgress();
        if (step >= requiredStep)
            return true;

        AddSalesPopupUI.ShowMessage(GetStepMessage(requiredStep));
        return false;
    }

    string GetStepMessage(BoxStep requiredStep)
    {
        switch (requiredStep)
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
            default:
                return "Complete previous step first.";
        }
    }
}
