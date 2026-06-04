using UnityEngine;
using StarterAssets;
using System.Collections;
using static PlayerInteractionSystem;
using Unity.Cinemachine;

public enum CameraMode
{
    FirstPerson,
    ThirdPerson
}

public class CameraModeManager : MonoBehaviour
{
    public static CameraModeManager Instance { get; private set; }
    [Header("Cinemachine")]
    public CinemachineBrain cinemachineBrain;

    [Header("Cameras")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;
    public Transform firstPersonCameraRoot;
    public Transform thirdPersonCameraRoot;
    [Header("Controllers")]
    public FirstPersonController firstPersonController;
    public ThirdPersonController thirdPersonController;
    public StarterAssetsInputs starterInput;

    [Header("Visual")]
    public GameObject characterVisual;
    public GameObject player;
    public GameObject InteractPoint;

    [Header("Startup Mode")]
    [SerializeField] private CameraMode startMode = CameraMode.FirstPerson;
    [Header("Interaction")]
    public PlayerInteractionSystem interactionSystem;

    public CinemachineInputAxisController cinemachineInput;
    bool isUILocked;
    public CameraMode CurrentMode { get; private set; }
    bool lockMode;
    [Header("Effects")]
    public GameObject jumpDust1;
    public GameObject jumpDust2;

    public void LockMode(bool value)
    {
        lockMode = value;
    }
    void UpdateDustState(CameraMode mode)
    {
        bool enableDust = (mode == CameraMode.ThirdPerson);

        if (jumpDust1)
            jumpDust1.SetActive(enableDust);

        if (jumpDust2)
            jumpDust2.SetActive(enableDust);
    }

    void EnsureReferences()
    {
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player");

        if (!player)
            return;

        if (!thirdPersonController)
            thirdPersonController = player.GetComponent<ThirdPersonController>();

        if (!firstPersonController)
            firstPersonController = player.GetComponent<FirstPersonController>();

        if (!starterInput)
            starterInput = player.GetComponent<StarterAssetsInputs>();

        if (!interactionSystem)
            interactionSystem = player.GetComponent<PlayerInteractionSystem>();

        if (!cinemachineBrain && Camera.main)
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
    }

    void ConfigureCinemachineBrain()
    {
        if (!cinemachineBrain)
            return;

        cinemachineBrain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        cinemachineBrain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
    }

    void CopyFirstPersonSettingsToMotor()
    {
        if (!firstPersonController || !thirdPersonController)
            return;

        thirdPersonController.FirstPersonMoveSpeed = firstPersonController.MoveSpeed;
        thirdPersonController.FirstPersonSprintSpeed = firstPersonController.SprintSpeed;
        thirdPersonController.FirstPersonRotationSpeed = firstPersonController.RotationSpeed;
        thirdPersonController.FirstPersonTopClamp = firstPersonController.TopClamp;
        thirdPersonController.FirstPersonBottomClamp = firstPersonController.BottomClamp;
    }
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ❌ อย่าตั้ง CurrentMode ที่นี่
    }

    void Start()
    {
        EnsureReferences();
        ConfigureCinemachineBrain();

        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoading) // ⭐ เพิ่ม flag
            return;

        CurrentMode = (startMode == CameraMode.FirstPerson)
            ? CameraMode.ThirdPerson
            : CameraMode.FirstPerson;

        SetMode(startMode);
    }


    public void SetMode(CameraMode mode)
    {
        EnsureReferences();
        ConfigureCinemachineBrain();

        if (CurrentMode == mode &&
            thirdPersonController &&
            thirdPersonController.PerspectiveMode == mode)
        {
            return;
        }

        ResetInput();

        // ===== สลับกล้อง =====
        bool isFP = mode == CameraMode.FirstPerson;
        if (firstPersonCamera)
            firstPersonCamera.gameObject.SetActive(isFP);

        if (thirdPersonCamera)
            thirdPersonCamera.gameObject.SetActive(!isFP);

        // ===== Visual =====
        if (characterVisual)
            characterVisual.SetActive(!isFP);

        if (InteractPoint)
            InteractPoint.SetActive(isFP);

        if (interactionSystem)
        {
            if (isFP)
            {
                if (firstPersonCamera)
                    interactionSystem.SetCurrentCamera(firstPersonCamera);

                interactionSystem.interactRayMode = InteractRayMode.Camera;
            }
            else
            {
                if (thirdPersonCamera)
                    interactionSystem.SetCurrentCamera(thirdPersonCamera);

                interactionSystem.interactRayMode = InteractRayMode.Player;
            }
        }

        ResetFPCameraRoot();
        ResetTPCameraRootAndState();
        CopyFirstPersonSettingsToMotor();

        // ใช้ ThirdPersonController เป็น motor ตัวเดียว และปิด FirstPersonController ไว้เสมอ
        if (firstPersonController)
            firstPersonController.enabled = false;

        if (thirdPersonController)
        {
            thirdPersonController.enabled = true;
            thirdPersonController.SetPerspectiveMode(mode, firstPersonCameraRoot);
        }

        CurrentMode = mode;

        if (!isFP)
            ResetThirdPersonOrbit();

        ResetCharacterController();
        ResetCharacterVisual();
        UpdateDustState(mode);
        Debug.Log($"[CameraMode] {mode}");
    }

    void ResetInput()
    {
        if (!starterInput) return;
        starterInput.move = Vector2.zero;
        starterInput.look = Vector2.zero;
        starterInput.jump = false;
        starterInput.sprint = false;
    }

    void ResetFPCameraRoot()
    {
        if (!firstPersonCameraRoot || !player) return;

        firstPersonCameraRoot.SetParent(player.transform, false);
        firstPersonCameraRoot.localPosition = new Vector3(0, 1.2f, 0);

        // ⭐ เก็บ pitch เดิมของ camera root
        Vector3 euler = firstPersonCameraRoot.localEulerAngles;
        firstPersonCameraRoot.localRotation =
            Quaternion.Euler(euler.x, 0f, 0f);
    }


    void ResetTPCameraRootAndState()
    {
        if (!thirdPersonCameraRoot || !player) return;

        if (thirdPersonCameraRoot.parent != player.transform)
            thirdPersonCameraRoot.SetParent(player.transform, false);

        thirdPersonCameraRoot.localPosition = Vector3.zero;

        // ThirdPersonController now owns the target yaw. Keep only the rig anchor clean here.
        thirdPersonCameraRoot.localRotation = Quaternion.identity;
    }

    void ResetThirdPersonOrbit()
    {
        if (!thirdPersonCamera)
            return;

        var orbital = thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbital)
        {
            // 0 degrees is the back of the tracked target for CinemachineOrbitalFollow.
            orbital.HorizontalAxis.Value = 0f;
            orbital.VerticalAxis.Value = orbital.VerticalAxis.Center;
            orbital.RadialAxis.Value = orbital.RadialAxis.Center;
        }

        var cinemachineCamera = thirdPersonCamera.GetComponent<CinemachineCamera>();
        if (cinemachineCamera)
            cinemachineCamera.PreviousStateIsValid = false;
    }

    void ResetThirdPersonCameraTransform()
    {
        if (!thirdPersonCamera)
            return;

        Transform t = thirdPersonCamera.transform;
        Vector3 localEuler = t.localEulerAngles;
        t.localRotation = Quaternion.Euler(localEuler.x, 0f, localEuler.z);
    }


    void ResetCharacterController()
    {
        if (!player) return;

        var cc = player.GetComponent<CharacterController>();
        if (!cc) return;

        bool wasEnabled = cc.enabled;

        // ปิดก่อน
        cc.enabled = false;

        // รีเซ็ต center เฉย ๆ
        cc.center = new Vector3(0, cc.height / 2f, 0);

        // เปิดกลับ
        cc.enabled = wasEnabled;
    }

    public void ResetActiveControllerOneFrame()
    {
        StartCoroutine(ResetControllerRoutine());
    }

    private IEnumerator ResetControllerRoutine()
    {
        EnsureReferences();
        ResetInput();
        ResetCameraRigForCurrentMode();
        ResetCharacterController();
        ResetCharacterVisual();
        ApplyModeToController();

        yield return null; // ⏸ 1 frame

        ResetInput();
        ResetCameraRigForCurrentMode();
        ApplyModeToController();
    }

    void ResetCameraRigForCurrentMode()
    {
        ResetFPCameraRoot();
        ResetTPCameraRootAndState();

        if (CurrentMode == CameraMode.ThirdPerson)
            CleanupAfterFirstPerson();
    }

    void ApplyModeToController()
    {
        if (firstPersonController)
            firstPersonController.enabled = false;

        if (!thirdPersonController)
            return;

        CopyFirstPersonSettingsToMotor();
        thirdPersonController.enabled = true;
        thirdPersonController.SetPerspectiveMode(CurrentMode, firstPersonCameraRoot);

        if (CurrentMode == CameraMode.ThirdPerson)
            ResetThirdPersonOrbit();
    }
    public void ApplyRotation(
        Vector3? playerEuler,
        Vector2? cameraLook)
    {
        EnsureReferences();

        if (playerEuler.HasValue && player)
        {
            player.transform.rotation =
                Quaternion.Euler(0, playerEuler.Value.y, 0);
        }

        if (cameraLook.HasValue)
        {
            SetCameraLook(cameraLook.Value);
        }
        else if (thirdPersonController && player)
        {
            float pitch = CurrentMode == CameraMode.FirstPerson
                ? thirdPersonController.GetLookAngles().y
                : 0f;

            thirdPersonController.SetLookAngles(pitch, player.transform.eulerAngles.y);
        }

        if (CurrentMode == CameraMode.ThirdPerson)
            ResetThirdPersonOrbit();
    }

    void CleanupAfterFirstPerson()
    {
        if (!firstPersonCameraRoot || !player) return;

        if (firstPersonCameraRoot.parent != player.transform)
            firstPersonCameraRoot.SetParent(player.transform, false);

        firstPersonCameraRoot.localPosition = new Vector3(0, 1.2f, 0);
        firstPersonCameraRoot.localRotation = Quaternion.identity;
    }



    void SetCameraLook(Vector2 look)
    {
        if (!thirdPersonController) return;

        thirdPersonController.SetLookAngles(
            look.y, // pitch
            look.x  // yaw
        );

        if (CurrentMode == CameraMode.ThirdPerson)
            ResetThirdPersonOrbit();
    }

    void ResetCharacterVisual()
    {
        if (!characterVisual || !player) return;

        // parent ต้องถูก
        if (characterVisual.transform.parent != player.transform)
            characterVisual.transform.SetParent(player.transform, false);

        characterVisual.transform.localPosition = Vector3.zero;
        characterVisual.transform.localRotation = Quaternion.identity;
    }
    public CameraSaveData Capture()
    {
        EnsureReferences();

        return new CameraSaveData
        {
            mode = CurrentMode,
            playerYaw = player ? player.transform.eulerAngles.y : 0f,
            look = thirdPersonController ? thirdPersonController.GetLookAngles() : Vector2.zero
        };
    }

    public void Restore(CameraSaveData d)
    {
        SetMode(d.mode);
        ApplyRotation(
            new Vector3(0, d.playerYaw, 0),
            d.look
        );
    }
    public void SetCameraInputLocked(bool locked)
    {
        isUILocked = locked;

        if (starterInput)
            starterInput.look = Vector2.zero;
    }

    public void SetUILock(bool lockCamera, bool showCursor)
    {
        SetCameraInputLocked(lockCamera);

        if (cinemachineInput)
            cinemachineInput.enabled = !lockCamera;

        if (starterInput)
            starterInput.cursorLocked = !showCursor;

        Cursor.lockState = showCursor
            ? CursorLockMode.None
            : CursorLockMode.Locked;
        Cursor.visible = showCursor;
    }

    void LateUpdate()
    {
        if (!isUILocked) return;

        if (starterInput)
            starterInput.look = Vector2.zero;

        if (characterVisual && player)
        {
            characterVisual.transform.localPosition = Vector3.zero;
            characterVisual.transform.localRotation = Quaternion.identity;
        }
    }
    //void HardResetThirdPersonCamera()
    //{
    //    if (!thirdPersonController || !player) return;

    //    float playerYaw = player.transform.eulerAngles.y;

    //    // รีเซ็ตค่า look ภายใน controller
    //    thirdPersonController.SetLookAngles(0f, playerYaw);

    //    // รีเซ็ต Cinemachine state
    //    var vcam = thirdPersonCamera.GetComponent<CinemachineCamera>();
    //    if (vcam != null)
    //    {
    //        vcam.PreviousStateIsValid = false;
    //    }
    //}
}
