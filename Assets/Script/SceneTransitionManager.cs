using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;
    public string CurrentSpawnId { get; private set; }

    public GameObject player;

    string pendingSpawnId;
    CameraMode pendingCameraMode;
    bool waitingForScene;
    public bool isTransitioning;
    public bool IsInsideShop { get; private set; }
    bool isTeleportingSameScene;
    HashSet<string> visitedScenes = new HashSet<string>();
    public bool HasVisitedScene(string sceneName)
    {
        return visitedScenes.Contains(sceneName);
    }

    public void MarkSceneVisited(string sceneName)
    {
        if (!visitedScenes.Contains(sceneName))
        {
            visitedScenes.Add(sceneName);
            Debug.Log($"[STM] Scene visited: {sceneName}");
        }
    }
    void Start()
    {
        SetShopState(IsInsideShop);
        MarkSceneVisited(SceneManager.GetActiveScene().name);

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
        CurrentSpawnId = spawnId;
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
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");

            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            DontDestroyOnLoad(es);
        }

        MarkSceneVisited(scene.name);

        if (!waitingForScene) return;

        StartCoroutine(SpawnRoutine());
        SetShopState(false);
    }
    public void SetShopState(bool insideShop)
    {
        IsInsideShop = insideShop;
    }

    IEnumerator SpawnRoutine()
    {
        Debug.Log($"[SpawnRoutine] pendingSpawnId = {pendingSpawnId}");

        yield return null;

        SpawnPoint[] points =
            FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        bool teleported = false;

        foreach (var sp in points)
        {
            if (sp.spawnId == pendingSpawnId)
            {
                Debug.Log($"[SpawnRoutine] found spawnId = {sp.spawnId}");

                TeleportInternal(sp.transform, pendingCameraMode);
                teleported = true;
                break;
            }
        }

        if (!teleported && points.Length > 0)
        {
            Debug.LogWarning("[SpawnRoutine] spawnId not found, fallback to first SpawnPoint");
            TeleportInternal(points[0].transform, pendingCameraMode);
        }


        yield return null; 
        var zones = FindObjectsByType<ShopCameraZone>(FindObjectsSortMode.None);
        foreach (var z in zones)
        {
            z.CheckPlayerInside(); 
        }


        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeIn();

        var interaction = player.GetComponent<PlayerInteractionSystem>();
        if (interaction)
        {
            interaction.enabled = false;
            yield return null;
            interaction.enabled = true;
        }


        CameraModeManager.Instance.LockMode(false);
        isTransitioning = false;

        waitingForScene = false;
        pendingSpawnId = null;
    }


    public void Teleport(Transform spawnPoint, CameraMode mode)
    {
        if (isTransitioning || isTeleportingSameScene) return;
        StartCoroutine(TeleportWithFadeRoutine(spawnPoint, mode));
    }

    void TeleportInternal(Transform spawn, CameraMode mode)
    {
        var camMgr = CameraModeManager.Instance;
        var cc = player.GetComponent<CharacterController>();
        var sp = spawn.GetComponent<SpawnPoint>();

        if (cc) cc.enabled = false;

        player.transform.position = spawn.position;
        player.transform.rotation = spawn.rotation;
        
        camMgr.SetMode(mode);


        StartCoroutine(EnableCCNextFrame(cc));
        StartCoroutine(ApplyRotationNextFrame(cc, camMgr, sp));
    }

    IEnumerator EnableCCNextFrame(CharacterController cc)
    {
        yield return null; // รอ 1 frame
        if (cc) cc.enabled = true;
    }
    IEnumerator ApplyRotationNextFrame(
    CharacterController cc,
    CameraModeManager camMgr,
    SpawnPoint sp)
    {
        yield return null; // รอ 1 frame

        if (cc) cc.enabled = true;

        yield return null; // รอให้ controller + camera พร้อมจริง

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
