using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;
using UnityEngine.InputSystem;
using static PlayerInteractionSystem;


public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public GameObject InteractPoint;
    [Header("Scene")]
    public string mainSceneName = "Main";

    [Header("Player")]
    public GameObject player;

    [Header("Character Visual (Mesh / Prefab)")]
    public GameObject characterVisual; // ปิดตอน First Person

    [Header("Cameras")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;
    public Transform firstPersonCameraRoot;


    [Header("Starter Assets Controllers")]
    public FirstPersonController firstPersonController;
    public ThirdPersonController thirdPersonController;
    [SerializeField] private StarterAssetsInputs starterInput;

    // ===== Warp =====
    private string pendingSpawnId;
    private bool hasPendingSpawn;

    [Header("Interaction")]
    public PlayerInteractionSystem interactionSystem;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        starterInput = player.GetComponent<StarterAssetsInputs>();

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // =================================================
    // 🌍 Warp
    // =================================================
    public void WarpToScene(string targetSceneName, string spawnId)
    {
        pendingSpawnId = spawnId;
        hasPendingSpawn = true;

        SceneManager.LoadScene(targetSceneName);
    }

    // =================================================
    // 🔁 Scene Loaded
    // =================================================
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ❗ สำคัญ: สลับโหมดก่อน
        SwitchMode(scene.name);

        if (!hasPendingSpawn) return;

        SpawnPoint[] spawnPoints =
            FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        foreach (var sp in spawnPoints)
        {
            if (sp.spawnId == pendingSpawnId)
            {
                player.transform.position = sp.transform.position;
                player.transform.rotation = sp.transform.rotation;
                break;
            }
        }

        hasPendingSpawn = false;
        pendingSpawnId = null;
    }

    // =================================================
    // 🎮 Mode Switch (SAFE)
    // =================================================
    void SwitchMode(string sceneName)
    {
        bool isMain = sceneName == mainSceneName;

        // ===== 1. ปิด Script ก่อน (กัน LateUpdate พัง) =====
        if (firstPersonController != null)
            firstPersonController.enabled = false;

        if (thirdPersonController != null)
            thirdPersonController.enabled = false;
        ResetStarterInput();
        // ===== 2. สลับกล้อง =====
        if (firstPersonCamera != null)
            firstPersonCamera.gameObject.SetActive(isMain);

        if (thirdPersonCamera != null)
            thirdPersonCamera.gameObject.SetActive(!isMain);

        // ===== 2.1 บอกระบบ Interact ว่ากล้องไหนคือ current =====
        if (interactionSystem != null)
        {
            interactionSystem.SetCurrentCamera(
                isMain ? firstPersonCamera : thirdPersonCamera
            );
            interactionSystem.interactRayMode =
    isMain ? InteractRayMode.Camera : InteractRayMode.Player;

        }

        InteractPoint.SetActive(isMain);
        // ===== 3. ตัวละคร =====
        if (characterVisual != null)
            characterVisual.SetActive(!isMain); // FP = ปิด, TP = เปิด
        if (isMain)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        //InteractPoint.SetActive(isMain);
        if (isMain && firstPersonController != null)
        {
            // 1. ปิด controller ก่อน
            firstPersonController.enabled = false;

            // 🔥 สลับ Action Map (จุดสำคัญ)

            // 2. รีเซ็ต Player
            player.transform.rotation =
                Quaternion.Euler(0, player.transform.eulerAngles.y, 0);

            // 3. 🔥 reset CameraRoot แบบ hard
            HardResetFirstPersonCameraRoot();

            // 4. รีเซ็ต CharacterController (สำคัญ)
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.center = new Vector3(0, cc.height / 2f, 0);

            // 5. เปิด controller ทีหลัง
            firstPersonController.enabled = true;
        }


        else
        {
            if (thirdPersonController != null)
                thirdPersonController.enabled = true;

        }
        
        Debug.Log(isMain
            ? "[SceneTransition] FIRST PERSON MODE"
            : "[SceneTransition] THIRD PERSON MODE");
    }

    void LateUpdate()
    {
        if (!firstPersonCameraRoot) return;

        firstPersonCameraRoot.localPosition = new Vector3(0, 1.2f, 0);
    }
    void HardResetFirstPersonCameraRoot()
    {
        if (!firstPersonCameraRoot || !player) return;

        // 🔥 บังคับ parent ใหม่ (สำคัญมาก)
        firstPersonCameraRoot.SetParent(player.transform, false);

        // 🔥 reset local space
        firstPersonCameraRoot.localPosition = new Vector3(0, 1.2f, 0);
        firstPersonCameraRoot.localRotation = Quaternion.identity;
        firstPersonCameraRoot.localScale = Vector3.one;
    }
    void ResetStarterInput()
    {
        if (!starterInput) return;

        starterInput.move = Vector2.zero;
        starterInput.look = Vector2.zero;
        starterInput.jump = false;
        starterInput.sprint = false;
    }

}
