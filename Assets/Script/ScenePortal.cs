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
    [SerializeField] private AudioClip interactSound;
    bool CanUsePortal()
    {
        if (SceneTransitionManager.Instance == null)
            return true;

        return SceneTransitionManager.Instance
            .HasVisitedScene(targetSceneName);
    }
    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }
    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        if (!CanUsePortal())
        {
            AddSalesPopupUI.ShowMessage("Discover this place first before using the portal.");
            return;
        }

        PlayInteractSound();
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
