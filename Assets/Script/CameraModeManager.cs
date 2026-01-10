using UnityEngine;
using StarterAssets;
using System.Collections;

public enum CameraMode
{
    FirstPerson,
    ThirdPerson
}

public class CameraModeManager : MonoBehaviour
{
    public static CameraModeManager Instance { get; private set; }

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


    public CameraMode CurrentMode { get; private set; }
    bool lockMode;


    public void LockMode(bool value)
    {
        lockMode = value;
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
        // 🔥 ตั้งค่า dummy ให้ต่างจาก startMode
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
            thirdPersonController.enabled = true;
         
        }

        CurrentMode = mode;

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
        firstPersonCameraRoot.localRotation = Quaternion.identity;
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
            Quaternion.Euler(0f, playerYaw, 0f);

  
        thirdPersonController.SetLookAngles(0f, playerYaw);
    }


    void ResetCharacterController()
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc)
            cc.center = new Vector3(0, cc.height / 2f, 0);
    }
    public void ResetActiveControllerOneFrame()
    {
        StartCoroutine(ResetControllerRoutine());
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

}
