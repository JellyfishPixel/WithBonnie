using UnityEngine;

public enum VehicleKind
{
    Car,
    Boat,
    Airplane
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour, IInteractable
{
    public static VehicleController ActiveVehicle { get; private set; }

    [Header("Vehicle")]
    public VehicleKind kind = VehicleKind.Car;
    public string vehicleName = "Vehicle";
    public KeyCode enterExitKey = KeyCode.E;

    [Header("Mount Points")]
    public Transform seatPoint;
    public Transform exitPoint;
    public Vector3 seatLocalOffset = new Vector3(0f, 1.1f, 0f);
    public Vector3 exitLocalOffset = new Vector3(1.8f, 0.2f, 0f);

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float reverseSpeed = 4f;
    public float turnSpeed = 90f;
    public float airplaneVerticalSpeed = 5f;
    public bool requireDriver = true;

    [Header("Boat Water")]
    public string waterTag = "Water";
    public float boatFloatHeight = 0.15f;
    public Vector3 boatWaterCheckExtents = new Vector3(1.5f, 1.5f, 1.5f);

    Rigidbody rb;
    PlayerInteractionSystem driver;
    PlayerMovementLocker movementLocker;
    Transform driverTransform;
    Transform originalDriverParent;
    Vector3 originalDriverScale;
    float waterSurfaceY;
    float lastWaterContactTime = -999f;

    bool IsOccupied => driver != null;
    public bool HasDriver(PlayerInteractionSystem interactor) => driver == interactor;
    bool IsBoatInWater => kind != VehicleKind.Boat || RefreshBoatWaterContact(transform.position);
    public bool IsDriving => IsOccupied;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = kind == VehicleKind.Car || kind == VehicleKind.Boat;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = GetComponent<Collider>();
        col.isTrigger = false;
    }

    void Update()
    {
        if (!IsOccupied)
            return;

        if (Input.GetKeyDown(enterExitKey))
            ExitVehicle();
    }

    void LateUpdate()
    {
        SyncDriverToSeat();
    }

    void FixedUpdate()
    {
        if (kind == VehicleKind.Boat)
            KeepBoatOnWater();

        if (requireDriver && !IsOccupied)
            return;

        Drive();
    }

    public void Interact(PlayerInteractionSystem interactor, PlayerInteractionSystem.InteractionType interactionType)
    {
        if (interactor == null)
            return;

        if (IsOccupied)
        {
            if (interactor == driver)
                ExitVehicle();

            return;
        }

        EnterVehicle(interactor);
    }

    void EnterVehicle(PlayerInteractionSystem interactor)
    {
        if (interactor.HeldObject != null)
        {
            AddSalesPopupUI.ShowMessage("Drop what you are holding before entering.");
            return;
        }

        if (kind == VehicleKind.Boat && !IsBoatInWater)
        {
            AddSalesPopupUI.ShowMessage("Boat must be in water.");
            return;
        }

        driver = interactor;
        driverTransform = interactor.transform;
        originalDriverParent = driverTransform.parent;
        originalDriverScale = driverTransform.localScale;
        movementLocker = interactor.GetComponent<PlayerMovementLocker>();
        ActiveVehicle = this;

        movementLocker?.Lock();
        interactor.LockMovement();

        driverTransform.SetPositionAndRotation(GetSeatPosition(), transform.rotation);
        driverTransform.localScale = originalDriverScale;

        AddSalesPopupUI.ShowSticky($"Driving {vehicleName}. Press E to exit.");
    }

    void ExitVehicle()
    {
        if (!IsOccupied)
            return;

        Transform exitingDriver = driverTransform;
        PlayerInteractionSystem exitingInteraction = driver;
        PlayerMovementLocker exitingLocker = movementLocker;

        exitingDriver.SetParent(originalDriverParent, true);
        exitingDriver.localScale = originalDriverScale;
        exitingDriver.SetPositionAndRotation(GetExitPosition(), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

        exitingLocker?.Unlock();
        exitingInteraction.UnlockMovement();
        CameraModeManager.Instance?.ResetActiveControllerOneFrame();

        driver = null;
        movementLocker = null;
        driverTransform = null;
        originalDriverParent = null;
        originalDriverScale = Vector3.one;
        if (ActiveVehicle == this)
            ActiveVehicle = null;

        AddSalesPopupUI.HideSticky();
    }

    public void RequestExit(PlayerInteractionSystem interactor)
    {
        if (interactor == null || interactor != driver)
            return;

        ExitVehicle();
    }

    void Drive()
    {
        if (kind == VehicleKind.Boat && !RefreshBoatWaterContact(transform.position))
        {
            rb.useGravity = true;
            return;
        }

        float forwardInput = Input.GetAxisRaw("Vertical");
        float turnInput = Input.GetAxisRaw("Horizontal");

        float speed = forwardInput >= 0f ? moveSpeed : reverseSpeed;
        Vector3 move = transform.forward * (forwardInput * speed * Time.fixedDeltaTime);

        if (kind == VehicleKind.Airplane)
        {
            if (Input.GetKey(KeyCode.Space))
                move += Vector3.up * (airplaneVerticalSpeed * Time.fixedDeltaTime);

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                move += Vector3.down * (airplaneVerticalSpeed * Time.fixedDeltaTime);
        }

        if (kind == VehicleKind.Boat)
        {
            rb.useGravity = false;
            Vector3 candidatePosition = rb.position + move;

            if (!TryGetWaterSurfaceY(candidatePosition, out float candidateSurfaceY))
            {
                move = Vector3.zero;
            }
            else
            {
                move.y = candidateSurfaceY + boatFloatHeight - rb.position.y;
            }
        }

        Quaternion turn = Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
        rb.MovePosition(rb.position + move);
    }

    void OnTriggerEnter(Collider other)
    {
        RegisterWaterContact(other);
    }

    void OnTriggerStay(Collider other)
    {
        RegisterWaterContact(other);
    }

    void OnCollisionStay(Collision collision)
    {
        RegisterWaterContact(collision.collider);
    }

    void RegisterWaterContact(Collider other)
    {
        if (kind != VehicleKind.Boat || !IsWaterCollider(other))
            return;

        lastWaterContactTime = Time.time;
        waterSurfaceY = other.bounds.max.y;
    }

    void KeepBoatOnWater()
    {
        if (!RefreshBoatWaterContact(transform.position))
        {
            rb.useGravity = !IsOccupied;
            return;
        }

        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 position = rb.position;
        position.y = waterSurfaceY + boatFloatHeight;
        rb.MovePosition(position);
    }

    bool RefreshBoatWaterContact(Vector3 checkPosition)
    {
        if (kind != VehicleKind.Boat)
            return true;

        if (!TryGetWaterSurfaceY(checkPosition, out float surfaceY))
            return false;

        lastWaterContactTime = Time.time;
        waterSurfaceY = surfaceY;
        return true;
    }

    bool TryGetWaterSurfaceY(Vector3 checkPosition, out float surfaceY)
    {
        surfaceY = 0f;

        Collider[] hits = Physics.OverlapBox(
            checkPosition,
            boatWaterCheckExtents,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (!IsWaterCollider(hit))
                continue;

            surfaceY = hit.bounds.max.y;
            return true;
        }

        return false;
    }

    bool IsWaterCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag(waterTag))
            return true;

        Transform parent = other.transform.parent;
        while (parent != null)
        {
            if (parent.CompareTag(waterTag))
                return true;

            parent = parent.parent;
        }

        return false;
    }

    public void PrepareForSceneTransition()
    {
        if (!IsOccupied)
            return;

        DontDestroyOnLoad(gameObject);

        if (driverTransform != null)
            driverTransform.SetParent(null, true);
    }

    public void PlaceDrivenVehicleAt(Transform spawn)
    {
        if (!IsOccupied || spawn == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        ReattachDriverToSeat();
    }

    void ReattachDriverToSeat()
    {
        if (!IsOccupied || driverTransform == null)
            return;

        driverTransform.SetParent(null, true);
        driverTransform.localScale = originalDriverScale == Vector3.zero ? Vector3.one : originalDriverScale;
        SyncDriverToSeat();
    }

    void SyncDriverToSeat()
    {
        if (!IsOccupied || driverTransform == null)
            return;

        driverTransform.SetPositionAndRotation(GetSeatPosition(), transform.rotation);
    }

    Vector3 GetSeatPosition()
    {
        return seatPoint != null
            ? seatPoint.position
            : transform.TransformPoint(seatLocalOffset);
    }

    Vector3 GetExitPosition()
    {
        return exitPoint != null
            ? exitPoint.position
            : transform.TransformPoint(exitLocalOffset);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetSeatPosition(), 0.25f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetExitPosition(), 0.25f);

        if (kind == VehicleKind.Boat)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boatWaterCheckExtents * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
