using UnityEngine;

public class FirstTimeSceneTrigger : MonoBehaviour
{
    public string targetSceneName;
    public string spawnId;
    public CameraMode cameraMode;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (SceneTransitionManager.Instance == null)
            return;

        bool isFirstVisit =
            !SceneTransitionManager.Instance.HasVisitedScene(targetSceneName);


        if (isFirstVisit)
        {
            SceneTransitionManager.Instance
                .MarkSceneVisited(targetSceneName);

            Debug.Log($"[FirstTime] First visit to {targetSceneName}");
 
        }


        SceneTransitionManager.Instance.WarpToScene(
            targetSceneName,
            spawnId,
            cameraMode
        );
    }
}