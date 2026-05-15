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

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
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
        if (!other.CompareTag("Player"))
            return;

        DeliveryConfirmUI.Instance?.ForceHide();
        AddSalesPopupUI.HideSticky();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (spawnedBoxForTest == null)
            return;

        PlayerInteractionSystem interaction = other.GetComponentInParent<PlayerInteractionSystem>();
        if (interaction == null)
            return;

        if (interaction.HeldObject != spawnedBoxForTest.gameObject)
            return;

        if (Input.GetKeyDown(interaction.storeBoxKey))
        {
            PlaceHeldBoxForDelivery(interaction);
        }
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
