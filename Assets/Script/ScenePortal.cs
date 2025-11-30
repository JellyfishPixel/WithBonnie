using UnityEngine;


public class ScenePortal : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("ชื่อฉากปลายทาง (เช่น \"Main\" หรือ \"Map\")")]
    public string targetSceneName = "Map";

    [Header("Spawn Point In Target Scene")]
    [Tooltip("spawnId ของ SpawnPoint ในฉากปลายทาง (เช่น \"FromMain\")")]
    public string targetSpawnId = "FromMain";

    [Header("Player Tag")]
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.WarpToScene(targetSceneName, targetSpawnId);
        }
        else
        {
            Debug.LogError("[ScenePortal] SceneTransitionManager.Instance = null");
        }
    }
}
