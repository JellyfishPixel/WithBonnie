using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    // ชื่อ spawn ที่ต้องใช้ในฉากถัดไป
    string pendingSpawnId;
    bool hasPendingSpawn = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
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
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// เรียกจาก Portal
    /// เช่น WarpToScene("Map", "FromMain")
    /// </summary>
    public void WarpToScene(string targetSceneName, string spawnId)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SceneTransition] targetSceneName ว่าง");
            return;
        }

        pendingSpawnId = spawnId;
        hasPendingSpawn = true;

        Debug.Log($"[SceneTransition] LoadScene('{targetSceneName}') with spawnId='{spawnId}'");
        SceneManager.LoadScene(targetSceneName);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasPendingSpawn) return;

        // หา Player (ตัวที่ tag = Player)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[SceneTransition] ไม่พบ Player ที่ tag = Player ในฉากใหม่");
            hasPendingSpawn = false;
            return;
        }

        // หา SpawnPoint ทั้งหมดในฉาก
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        Debug.Log($"[SceneTransition] Scene '{scene.name}' loaded, finding SpawnPoint id='{pendingSpawnId}'. Found {spawnPoints.Length} spawn(s).");

        SpawnPoint target = null;
        foreach (var sp in spawnPoints)
        {
            Debug.Log($"[SceneTransition] Found SpawnPoint '{sp.spawnId}' at {sp.transform.position}");
            if (sp.spawnId == pendingSpawnId)
            {
                target = sp;
                break;
            }
        }

        if (target != null)
        {
            // ย้ายตำแหน่ง + หมุนให้ตรง SpawnPoint
            player.transform.position = target.transform.position;
            player.transform.rotation = target.transform.rotation;

            Debug.Log($"[SceneTransition] Warp player -> spawnId='{pendingSpawnId}' pos={target.transform.position}");
        }
        else
        {
            Debug.LogWarning($"[SceneTransition] ไม่พบ SpawnPoint ที่ spawnId = '{pendingSpawnId}' ในฉาก {scene.name} (จะใช้ตำแหน่งเดิมของ Player แทน)");
        }

        hasPendingSpawn = false;
        pendingSpawnId = null;
    }
}
