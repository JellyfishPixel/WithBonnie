using UnityEngine;

public class DoorTeleport : MonoBehaviour
{
    public Transform spawnInside;
    public Transform spawnOutside;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (SceneTransitionManager.Instance.isTransitioning) return;

        var stm = SceneTransitionManager.Instance;

        if (stm.IsInsideShop)
        {
            // ออกจากร้าน
            stm.Teleport(spawnOutside, CameraMode.ThirdPerson);
            stm.SetShopState(false);
        }
        else
        {
            // เข้าร้าน
            stm.Teleport(spawnInside, CameraMode.FirstPerson);
            stm.SetShopState(true);
        }
    }
}
