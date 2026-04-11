using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public enum SaveMode
    {
        DevTest,   // ❌ ไม่เซฟจริง
        Production // ✅ เซฟจริง
    }
    public SaveMode saveMode = SaveMode.DevTest;
    GameSaveData cachedData;
    bool waitingForSceneLoad;
    public bool IsLoading { get; private set; }

    public static SaveManager Instance { get; private set; }

    string savePath;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "save.json");
        Debug.Log("SAVE PATH = " + Application.persistentDataPath);


    }
    public void SaveGame()
    {
        if (saveMode == SaveMode.DevTest)
        {
            Debug.Log("[SaveManager] DEV MODE: Skip Save");
            return;
        }

        string path = GetSavePath();

        GameSaveData data = BuildSaveData();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log("[SaveManager] Game Saved at " + path);
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnSceneLoadedForLoad();
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    string GetSavePath()
    {
#if UNITY_EDITOR
        if (saveMode == SaveMode.DevTest)
        {

            string dir = Path.Combine(Application.dataPath, "save");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return Path.Combine(dir, "save.json");
        }
#endif

        // 🎮 เกมจริง
        return Path.Combine(Application.persistentDataPath, "save.json");
    }
    public void OnSceneLoadedForLoad()
    {
        if (!waitingForSceneLoad || cachedData == null)
            return;

        var stm = SceneTransitionManager.Instance;
        var camMgr = CameraModeManager.Instance;

        if (cachedData.scene.hasPlayerTransform && stm != null && stm.player != null)
        {
            var player = stm.player;
            var cc = player.GetComponent<CharacterController>();

            if (cc) cc.enabled = false;

            player.transform.position = cachedData.scene.playerPosition;
            player.transform.rotation = cachedData.scene.playerRotation;

            if (camMgr != null)
            {
                camMgr.SetMode(cachedData.scene.cameraMode);
                StartCoroutine(ApplyCameraLookNextFrame(camMgr, cachedData.scene.cameraLook));
            }

            StartCoroutine(EnableCCNextFrame(cc));
        }

        GameManager.Instance.RestoreTime(cachedData.time);
        EconomyManager.Instance.Restore(cachedData.economy);
        BoxInventory.Instance.Restore(cachedData.inventory);
        RestoreDeliveries(cachedData.activeDeliveries);

        waitingForSceneLoad = false;
        cachedData = null;
        IsLoading = false;
    }


    public void LoadGame()
    {
        if (saveMode == SaveMode.DevTest)
        {
            Debug.Log("[SaveManager] DEV MODE: Skip Load");
            return;
        }

        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.Log("[SaveManager] No save file at " + path);
            return;
        }

        // ✅ 1. อ่านไฟล์ก่อน
        string json = File.ReadAllText(path);
        cachedData = JsonUtility.FromJson<GameSaveData>(json);

        // ✅ 2. ตรวจข้อมูลหลังอ่าน
        if (cachedData == null || cachedData.scene == null || !cachedData.scene.hasPlayerTransform)
        {
            Debug.LogWarning("[SaveManager] Save file has no player transform");
            return;
        }

        IsLoading = true;
        waitingForSceneLoad = true;

        // ✅ 3. โหลดซีน
        SceneManager.LoadScene(cachedData.scene.sceneName);

        Debug.Log("[SaveManager] Load requested, waiting for scene...");
    }
    public bool HasSave()
    {
        string path = GetSavePath();
        return File.Exists(path);
    }


    GameSaveData BuildSaveData()
    {
        return new GameSaveData
        {
            time = GameManager.Instance.CaptureTime(),
            economy = EconomyManager.Instance.Capture(),
            inventory = BoxInventory.Instance.Capture(),
            camera = CameraModeManager.Instance.Capture(),
            activeDeliveries = CaptureDeliveries(),
            scene = CaptureScene()
        };
    }
  
    SceneSaveData CaptureScene()
    {
        var stm = SceneTransitionManager.Instance;
        var camMgr = CameraModeManager.Instance;

        SceneSaveData scene = new SceneSaveData();
        scene.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (stm != null && stm.player != null)
        {
            Transform t = stm.player.transform;
            scene.playerPosition = t.position;
            scene.playerRotation = t.rotation;
            scene.hasPlayerTransform = true;
        }

        if (camMgr != null)
        {
            scene.cameraMode = camMgr.CurrentMode;
            scene.cameraLook = camMgr.starterInput.look;
        }

        return scene;
    }

    IEnumerator EnableCCNextFrame(CharacterController cc)
    {
        yield return null;
        if (cc) cc.enabled = true;
    }

    IEnumerator ApplyCameraLookNextFrame(CameraModeManager camMgr, Vector2 look)
    {
        yield return null;
        yield return null; // รอ input พร้อมจริง
        camMgr.ApplyRotation(null, look);
    }

    List<DeliverySaveData> CaptureDeliveries()
    {
        var list = new List<DeliverySaveData>();

        var gm = GameManager.Instance;
        if (gm == null) return list;

        foreach (var rec in gm.activeBoxes)
        {
            if (rec == null || rec.data == null) continue;

            list.Add(new DeliverySaveData
            {
                destinationId = rec.destinationId,
                itemId = rec.data.itemId,
                dayCreated = rec.dayCreated,
                itemQuality = rec.itemInstance != null
                    ? rec.itemInstance.currentQuality
                    : 100f
            });
        }

        return list;
    }
    void RestoreDeliveries(List<DeliverySaveData> list)
    {
        if (list == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        gm.activeBoxes.Clear();

        foreach (var d in list)
        {
            var itemData = ItemResolver.GetItem(d.itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[Load] Delivery item not found: {d.itemId}");
                continue;
            }

            var record = new GameManager.DeliveryRecord
            {
                data = itemData,
                destinationId = d.destinationId,
                dayCreated = d.dayCreated
            };

            gm.activeBoxes.Add(record);
        }

        gm.SendMessage("RelinkSceneSystemsAndRebuildMinimap",
            SendMessageOptions.DontRequireReceiver);
    }

    private void Update()
    {
        if (SceneTransitionManager.Instance != null &&
    SceneTransitionManager.Instance.isTransitioning)
        {
            Debug.Log("[SaveManager] Skip save during transition");
            return;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            SaveGame();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            LoadGame();
        }
    }
}
