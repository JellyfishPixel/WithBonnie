using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;
using Unity.Cinemachine;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene")]
    public string mainSceneName = "Main";

    [Header("Player")]
    public GameObject player;

    [Header("Character Visual (Mesh / Prefab)")]
    public GameObject characterVisual; // ปิดตอน First Person

    [Header("Cameras")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;


    [Header("Starter Assets Controllers")]
    public FirstPersonController firstPersonController;
    public ThirdPersonController thirdPersonController;
    [Header("FP Camera Reset")]
    public Transform fpCameraRoot;      // ตัวที่ถือกล้อง FP
    public Transform fpCameraTarget;    // CameraTarget ของ FirstPersonController

    [Header("Interaction")]
    public PlayerInteractionSystem interactionSystem;


    // ===== Warp =====
    private string pendingSpawnId;
    private bool hasPendingSpawn;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (!interactionSystem && player)
            interactionSystem = player.GetComponent<PlayerInteractionSystem>();
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
        }


        // ===== 3. ตัวละคร =====
        if (characterVisual != null)
            characterVisual.SetActive(!isMain); // FP = ปิด, TP = เปิด

        // ===== 4. เปิด Script ที่ต้องใช้ =====
        if (isMain)
        {
            ResetFirstPersonCamera();

            if (firstPersonController != null)
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
    void ResetFirstPersonCamera()
    {
        if (fpCameraTarget)
        {
            fpCameraTarget.localRotation = Quaternion.identity;
        }

        if (fpCameraRoot)
        {
            fpCameraRoot.localPosition = new Vector3(0,1,0);
            fpCameraRoot.localRotation = Quaternion.identity;
        }
    }



}
