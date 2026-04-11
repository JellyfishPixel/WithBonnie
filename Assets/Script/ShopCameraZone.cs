using UnityEngine;

public class ShopCameraZone : MonoBehaviour
{
    void Start()
    {
        CheckPlayerInside();
    }

    public void CheckPlayerInside()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        Collider zone = GetComponent<Collider>();
        if (!zone) return;

        if (zone.bounds.Contains(player.transform.position))
        {
            SceneTransitionManager.Instance.SetShopState(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (SceneTransitionManager.Instance.isTransitioning) return;

        SceneTransitionManager.Instance.SetShopState(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (SceneTransitionManager.Instance.isTransitioning) return;

        SceneTransitionManager.Instance.SetShopState(false);
    }
}
