using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    public GameObject player;

    string pendingSpawnId;
    CameraMode pendingCameraMode;
    bool waitingForScene;
    public bool isTransitioning;
    public bool IsInsideShop { get; private set; }

    void Start()
    {
        SetShopState(IsInsideShop);
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void WarpToScene(
        string sceneName,
        string spawnId,
        CameraMode cameraMode)
    {
        if (isTransitioning) return;

        StartCoroutine(WarpSceneRoutine(sceneName, spawnId, cameraMode));
    }
    IEnumerator WarpSceneRoutine(
        string sceneName,
        string spawnId,
        CameraMode cameraMode)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;


        CameraModeManager.Instance.LockMode(true);

        pendingSpawnId = spawnId;
        pendingCameraMode = cameraMode;
        waitingForScene = true;

        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeOut();
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }



    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!waitingForScene) return;

        StartCoroutine(SpawnRoutine());
        SetShopState(false);
    }
    public void SetShopState(bool insideShop)
    {
        IsInsideShop = insideShop;

        CameraModeManager.Instance.SetMode(
            insideShop ? CameraMode.FirstPerson : CameraMode.ThirdPerson
        );
    }

    IEnumerator SpawnRoutine()
    {
        yield return null;

        SpawnPoint[] points =
            FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        foreach (var sp in points)
        {
            if (sp.spawnId == pendingSpawnId)
            {
                TeleportInternal(sp.transform, pendingCameraMode);
                break;
            }
        }

        yield return null;

        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeIn();

        CameraModeManager.Instance.LockMode(false);
        isTransitioning = false;

        waitingForScene = false;
        pendingSpawnId = null;
    }


    public void Teleport(Transform spawnPoint, CameraMode mode)
    {
        if (isTransitioning) return;

        StartCoroutine(TeleportWithFadeRoutine(spawnPoint, mode));
    }

    void TeleportInternal(
        Transform spawn,
        CameraMode mode)
    {
        var cc = player.GetComponent<CharacterController>();
        var camMgr = CameraModeManager.Instance;

 
        camMgr.SetMode(mode);

        cc.enabled = false;
        player.transform.position = spawn.position;
        cc.enabled = true;

    
        var sp = spawn.GetComponent<SpawnPoint>();
        if (sp != null)
        {
            camMgr.ApplyRotation(
                sp.overridePlayerRotation ? sp.playerEuler : null,
                sp.overrideCameraRotation ? sp.cameraLook : null
            );
        }
    }

    IEnumerator TeleportWithFadeRoutine(
      Transform spawnPoint,
      CameraMode mode)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        try
        {
            // 🔒 ล็อก CameraMode ตั้งแต่เริ่ม
            CameraModeManager.Instance.LockMode(true);

            // 1. FadeOut
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeOut();
                yield return null; // ให้จอดำ render จริง
            }

            // 2. ตั้งโหมด + วาป ใต้จอดำ
            TeleportInternal(spawnPoint, mode);

            yield return null;

            // 3. FadeIn
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeIn();
            }
        }
        finally
        {
            // 🔓 ปลดล็อกเมื่อทุกอย่างเสร็จ
            CameraModeManager.Instance.LockMode(false);
            isTransitioning = false;
        }
    }



}
