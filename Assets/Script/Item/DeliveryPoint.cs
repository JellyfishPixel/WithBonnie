using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    public string destinationId;
    [Header("Auto Spawn Test")]
    public bool autoSpawnBoxOnEnter = true;
    public Transform autoSpawnPoint;
    public bool hideConfirmUIWhenAutoSpawn = true;
    [SerializeField] string holdObjectTag = "holdobject";
    [SerializeField] string groundTag = "Ground";
    [SerializeField] BoxInventory boxInventory;
    [SerializeField] GameManager gameManager;

    BoxCore spawnedBoxForTest;
    int spawnedSlotIndex = -1;

     string successMessage = "Delivery successful!!";
     string noBoxMessage = "You don't have any items for this destination.";
    [SerializeField] private AudioClip interactSound;

    void Awake()
    {
        CacheDependencies();
    }

    void CacheDependencies()
    {
        if (boxInventory == null)
            boxInventory = BoxInventory.Instance;

        if (gameManager == null)
            gameManager = GameManager.Instance;
    }

    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        if (interactor == null)
            return;

        if (type != PlayerInteractionSystem.InteractionType.Primary &&
            type != PlayerInteractionSystem.InteractionType.Secondary)
            return;

        if (spawnedBoxForTest != null)
        {
            if (interactor.HeldObject == spawnedBoxForTest.gameObject)
            {
                PlaceHeldBoxForDelivery(interactor);
                return;
            }

            Rigidbody rb = spawnedBoxForTest.GetComponent<Rigidbody>();
            if (rb != null && interactor.ForceHold(rb))
                AddSalesPopupUI.ShowSticky("Press E to deliver.");

            return;
        }

        if (TryDeliverHeldBox(interactor))
            return;

        if (interactor.HeldObject != null)
        {
            ShowMessage(noBoxMessage);
            return;
        }

        if (autoSpawnBoxOnEnter && TryAutoSpawnBoxFromInventory(ResolvePlayerCollider(interactor)))
            return;

        bool hasItem = HasItemToDeliver();
        if (!hasItem)
        {
            DeliveryConfirmUI.Instance?.ForceHide();
            ShowMessage(noBoxMessage);
            return;
        }

        if (DeliveryConfirmUI.Instance != null)
            DeliveryConfirmUI.Instance.Show(this, true);
        else
            ConfirmDelivery();

    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractionSystem interaction = GetInteractionFrom(other);
        if (interaction == null)
            return;

        if (autoSpawnBoxOnEnter && TryAutoSpawnBoxFromInventory(other))
        {
            if (hideConfirmUIWhenAutoSpawn && DeliveryConfirmUI.Instance != null)
                DeliveryConfirmUI.Instance.ForceHide();

            return;
        }

        bool hasItem = HasItemToDeliver();
        if (!hasItem)
        {
            DeliveryConfirmUI.Instance?.ForceHide();
            ShowMessage(noBoxMessage);
            return;
        }

        if (DeliveryConfirmUI.Instance != null)
            DeliveryConfirmUI.Instance.Show(this, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (GetInteractionFrom(other) == null)
            return;

        DeliveryConfirmUI.Instance?.ForceHide();
        AddSalesPopupUI.HideSticky();
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerInteractionSystem interaction = GetInteractionFrom(other);
        if (interaction == null)
            return;

        if (Input.GetKeyDown(interaction.storeBoxKey) &&
            spawnedBoxForTest == null &&
            TryDeliverHeldBox(interaction))
        {
            return;
        }

        if (spawnedBoxForTest == null)
            return;

        if (interaction.HeldObject != spawnedBoxForTest.gameObject)
            return;

        if (Input.GetKeyDown(interaction.storeBoxKey))
        {
            PlaceHeldBoxForDelivery(interaction);
        }
    }

    PlayerInteractionSystem GetInteractionFrom(Collider other)
    {
        if (other == null)
            return null;

        return other.GetComponentInParent<PlayerInteractionSystem>();
    }

    Collider ResolvePlayerCollider(PlayerInteractionSystem interaction)
    {
        if (interaction == null)
            return null;

        Collider playerCollider = interaction.GetComponent<Collider>();
        if (playerCollider != null)
            return playerCollider;

        return interaction.GetComponentInChildren<Collider>();
    }

    bool TryDeliverHeldBox(PlayerInteractionSystem interaction)
    {
        if (interaction == null || interaction.HeldObject == null)
            return false;

        BoxCore heldBox = interaction.HeldObject.GetComponent<BoxCore>();
        if (heldBox == null)
            return false;

        DeliveryItemData itemData = heldBox.CurrentItemData;
        if (itemData == null || itemData.destinationId != destinationId)
            return false;

        if (heldBox.Step != BoxStep.Labeled)
        {
            heldBox.CheckStepOrWarn(BoxStep.Labeled);
            return true;
        }

        Vector3 placePosition = (autoSpawnPoint != null ? autoSpawnPoint.position : transform.position) + Vector3.up * 0.15f;
        Quaternion placeRotation = autoSpawnPoint != null ? autoSpawnPoint.rotation : transform.rotation;
        GameObject dropped = interaction.DropHeldAt(placePosition, placeRotation);
        if (dropped == null)
            return true;

        BoxCore deliveredBox = dropped.GetComponent<BoxCore>();
        if (deliveredBox == null)
            return true;

        float quality = deliveredBox.CurrentItemInstance != null
            ? deliveredBox.CurrentItemInstance.currentQuality
            : itemData.baseQuality;

        int effectiveLimit = DeliveryCalculationService.CalculateEffectiveDeadlineDays(
            itemData,
            itemData.deliveryLimitDays,
            deliveredBox.boxType == BoxKind.ColdBox,
            deliveredBox.hasIceBubble && deliveredBox.boxType == BoxKind.ColdBox);

        int reward = DeliveryCalculationService.CalculateReward(
            itemData,
            quality,
            0,
            0,
            effectiveLimit,
            quality <= itemData.brokenThreshold);

        deliveredBox.MarkDelivered();

        CacheDependencies();

        if (gameManager != null)
        {
            gameManager.AddMoney(reward);
            gameManager.MarkDeliveredByDestination(destinationId);
        }

        if (reward > 0)
            AddSalesPopupUI.ShowNotice(reward, true);

        ShowMessage(successMessage);
        PlayInteractSound();
        AddSalesPopupUI.HideSticky();

        Destroy(deliveredBox.gameObject);
        return true;
    }

    bool TryAutoSpawnBoxFromInventory(Collider playerCollider)
    {
        if (spawnedBoxForTest != null)
            return true;

        CacheDependencies();

        if (boxInventory == null)
            return false;

        Transform spawnTarget = ResolveSpawnTarget(playerCollider);
        if (spawnTarget == null)
            return false;

        int slotIndex = boxInventory.FindSlotByDestination(destinationId);
        if (slotIndex < 0)
            return false;

        spawnedSlotIndex = slotIndex;
        spawnedBoxForTest = boxInventory.SpawnBoxFromSlot(
            slotIndex,
            spawnTarget,
            clearSlot: false);

        if (spawnedBoxForTest == null)
        {
            ShowMessage("Auto-spawn failed. Check the saved box prefab/components.");
            return false;
        }

        if (!spawnedBoxForTest.gameObject.activeInHierarchy)
        {
            ShowMessage("Auto-spawn created an inactive box. Cancelling test spawn.");
            Destroy(spawnedBoxForTest.gameObject);
            spawnedBoxForTest = null;
            return false;
        }

        PlayerInteractionSystem interaction = playerCollider != null
            ? playerCollider.GetComponentInParent<PlayerInteractionSystem>()
            : null;

        if (interaction != null)
        {
            Rigidbody rb = spawnedBoxForTest.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Transform holdTarget = ResolveHoldTarget(playerCollider);
                spawnedBoxForTest.SetPreventInventoryStore(true);
                interaction.ForceHoldAt(rb, holdTarget != null ? holdTarget : spawnTarget);
                AddSalesPopupUI.ShowSticky("Press E to deliver.");
            }
        }

        ShowMessage($"Spawned delivery box for trigger test at {spawnedBoxForTest.transform.position}.");
        return true;
    }

    Transform ResolveSpawnTarget(Collider playerCollider)
    {
        if (!string.IsNullOrEmpty(holdObjectTag))
        {
            GameObject taggedHoldObject = GameObject.FindWithTag(holdObjectTag);
            if (taggedHoldObject != null)
                return taggedHoldObject.transform;
        }

        if (playerCollider != null)
        {
            PlayerInteractionSystem interaction = playerCollider.GetComponentInParent<PlayerInteractionSystem>();
            if (interaction != null && interaction.holdPoint != null)
                return interaction.holdPoint;
        }

        if (autoSpawnPoint != null)
            return autoSpawnPoint;

        autoSpawnPoint = transform;
        return autoSpawnPoint;
    }

    Transform ResolveHoldTarget(Collider playerCollider)
    {
        if (!string.IsNullOrEmpty(holdObjectTag))
        {
            GameObject taggedHoldObject = GameObject.FindWithTag(holdObjectTag);
            if (taggedHoldObject != null)
                return taggedHoldObject.transform;
        }

        if (playerCollider != null)
        {
            PlayerInteractionSystem interaction = playerCollider.GetComponentInParent<PlayerInteractionSystem>();
            if (interaction != null && interaction.holdPoint != null)
                return interaction.holdPoint;
        }

        return null;
    }

    void PlaceHeldBoxForDelivery(PlayerInteractionSystem interaction)
    {
        Vector3 desiredPosition = spawnedBoxForTest != null
            ? spawnedBoxForTest.transform.position
            : (autoSpawnPoint != null ? autoSpawnPoint.position : transform.position);
        Quaternion placeRotation = spawnedBoxForTest != null
            ? spawnedBoxForTest.transform.rotation
            : (autoSpawnPoint != null ? autoSpawnPoint.rotation : transform.rotation);
        Vector3 placePosition = desiredPosition + Vector3.up * 0.15f;

        GameObject dropped = interaction.DropHeldAt(placePosition, placeRotation);
        if (dropped == null)
            return;

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (spawnedBoxForTest != null)
        {
            spawnedBoxForTest.SetPreventInventoryStore(false);
            spawnedBoxForTest.MarkDelivered();
        }

        int reward = 0;
        CacheDependencies();

        bool ok = boxInventory != null &&
                  boxInventory.TryDeliverSlot(spawnedSlotIndex, out reward, destroyShell: false);

        if (!ok)
        {
            ShowMessage(noBoxMessage);
            AddSalesPopupUI.HideSticky();
            return;
        }

        if (gameManager != null)
            gameManager.MarkDeliveredByDestination(destinationId);

        if (reward > 0 && gameManager != null)
        {
            gameManager.AddMoney(reward);
            AddSalesPopupUI.ShowNotice(reward, true);
        }

        ShowMessage(successMessage);
        PlayInteractSound();
        AddSalesPopupUI.HideSticky();

        spawnedBoxForTest = null;
        spawnedSlotIndex = -1;
    }

    public void ConfirmDelivery()
    {
        CacheDependencies();

        if (boxInventory == null)
        {
            ShowMessage(noBoxMessage);
            return;
        }

        int reward;
        bool ok = boxInventory.TryDeliverFromInventory(destinationId, out reward);

        if (!ok)
        {
            ShowMessage(noBoxMessage);
            return;
        }

        if (gameManager != null)
        {
            gameManager.MarkDeliveredByDestination(destinationId);
        }

        if (reward > 0)
        {
            if (gameManager != null)
            {
                gameManager.AddMoney(reward);

                // 💰 popup เงิน
                AddSalesPopupUI.ShowNotice(reward, true);
            }
        }

        ShowMessage(successMessage);
        PlayInteractSound();
    }

    void PlayInteractSound()
    {
        if (interactSound == null) return;
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }

    void ShowMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        Debug.Log($"[DeliveryPoint] {msg}");
    }

    public bool HasItemToDeliver()
    {
        CacheDependencies();

        if (boxInventory == null) return false;

        int reward;
        return boxInventory.TryCheckHasItem(destinationId, out reward);
    }
}
