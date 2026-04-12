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
        if (CurrentMode == mode) return;

        // ===== ปิดทุกอย่างก่อน (กัน frame ค้าง) =====
        firstPersonController.enabled = false;
        thirdPersonController.enabled = false;
        ResetInput();

        // ===== สลับกล้อง =====
        bool isFP = mode == CameraMode.FirstPerson;
        firstPersonCamera.gameObject.SetActive(isFP);
        thirdPersonCamera.gameObject.SetActive(!isFP);

        // ===== Visual =====
        characterVisual.SetActive(!isFP);
        InteractPoint.SetActive(isFP);
        if (interactionSystem)
        {
            if (isFP)
            {
                interactionSystem.SetCurrentCamera(firstPersonCamera);
                interactionSystem.interactRayMode = InteractRayMode.Camera;
            }
            else
            {
                interactionSystem.SetCurrentCamera(thirdPersonCamera);
                interactionSystem.interactRayMode = InteractRayMode.Player;
            }
        }

        // ===== เปิด Controller ที่ถูกต้อง =====
        if (isFP)
        {
            ResetFPCameraRoot();
            ResetCharacterController();
            firstPersonController.enabled = true;
        }
        else
        {
            CleanupAfterFirstPerson();
            ResetTPCameraRootAndState();
            thirdPersonController.HardResetCamera();
            thirdPersonController.enabled = true;
        }

        CurrentMode = mode;
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

    
        thirdPersonController.enabled = false;

        if (thirdPersonCameraRoot.parent != player.transform)
            thirdPersonCameraRoot.SetParent(player.transform, false);

      
        thirdPersonCameraRoot.localPosition = Vector3.zero;

  
        float playerYaw = player.transform.eulerAngles.y;
        thirdPersonCameraRoot.localRotation =
            Quaternion.Euler(0f, 0f, 0f);

  
        thirdPersonController.SetLookAngles(0f, 0f);
    }


    void ResetCharacterController()
    {
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
        if (CurrentMode == CameraMode.FirstPerson)
        {
            ResetFPCameraRoot();
            ResetCharacterController();
            ResetCharacterVisual();
            firstPersonController.enabled = true;
        }
        else
        {
            CleanupAfterFirstPerson();
            ResetTPCameraRootAndState();
            ResetCharacterVisual();
            thirdPersonController.enabled = true;
        }


    }

    private IEnumerator ResetControllerRoutine()
    {
        // ปิดทุก controller ก่อน (ปลอดภัย)
        firstPersonController.enabled = false;
        thirdPersonController.enabled = false;

        ResetInput();

        yield return null; // ⏸ 1 frame

        // เปิดเฉพาะ controller ที่ตรงกับ mode
        if (CurrentMode == CameraMode.FirstPerson)
        {
            ResetFPCameraRoot();
            ResetCharacterController();
            firstPersonController.enabled = true;
        }
        else
        {
            CleanupAfterFirstPerson();
            ResetTPCameraRootAndState();
            thirdPersonController.enabled = true;
          
        }

    }
    public void ApplyRotation(
        Vector3? playerEuler,
        Vector2? cameraLook)
    {
        if (playerEuler.HasValue)
        {
            player.transform.rotation =
                Quaternion.Euler(0, playerEuler.Value.y, 0);
        }

        if (cameraLook.HasValue)
        {
            SetCameraLook(cameraLook.Value);
        }
    }

    void CleanupAfterFirstPerson()
    {
        if (!firstPersonCameraRoot) return;

        // ถอด parent เพื่อกัน offset ค้าง
        firstPersonCameraRoot.SetParent(null);

        firstPersonCameraRoot.localPosition = Vector3.zero;
        firstPersonCameraRoot.localRotation = Quaternion.identity;
    }



    void SetCameraLook(Vector2 look)
    {
        if (CurrentMode == CameraMode.FirstPerson)
        {
            firstPersonController.SetLookAngles(
                look.y, // pitch
                look.x  // yaw
            );
        }
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
        return new CameraSaveData
        {
            mode = CurrentMode,
            playerYaw = player.transform.eulerAngles.y,
            look = starterInput.look
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

        if (CurrentMode == CameraMode.ThirdPerson && thirdPersonCameraRoot)
        {
            Vector3 euler = thirdPersonCameraRoot.localEulerAngles;
            thirdPersonCameraRoot.localRotation =
                Quaternion.Euler(0f, euler.y, 0f);
        }
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
