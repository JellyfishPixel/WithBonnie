using UnityEngine;


public class ScenePortal : MonoBehaviour , IInteractable
{
    [Header("Target Scene")]
    [Tooltip("ชื่อฉากปลายทาง (เช่น \"Main\" หรือ \"Map\")")]
    public string targetSceneName = "Map";

    [Header("Spawn Point In Target Scene")]
    [Tooltip("spawnId ของ SpawnPoint ในฉากปลายทาง (เช่น \"FromMain\")")]
    public string targetSpawnId = "FromMain";

    [Header("Player Tag")]
    public string playerTag = "Player";
    public CameraMode targetCameraMode;

    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        // E เท่านั้น
        if (type != PlayerInteractionSystem.InteractionType.Secondary)
            return;

        if (SceneTransitionManager.Instance != null)
        {

            SceneTransitionManager.Instance.WarpToScene(
                targetSceneName,
                targetSpawnId,
                targetCameraMode   
            );
        }
        else
        {
            Debug.LogError("[ScenePortal] SceneTransitionManager.Instance = null");
        }
    }
}
