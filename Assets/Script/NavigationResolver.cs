using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationResolver : MonoBehaviour
{
    public static NavigationResolver Instance;

    public DestinationRegistry destinationRegistry;
    public Transform warpPoint; // จุดวาร์ปของแมพปัจจุบัน

    void Awake()
    {
        Instance = this;
    }

    public Transform GetCurrentNavigationTarget()
    {
        if (BoxInventory.Instance == null) return null;

        var slot = BoxInventory.Instance.GetNearestSlot();
        if (slot == null || slot.itemData == null) return null;

        string destId = slot.itemData.destinationId;
        if (string.IsNullOrEmpty(destId)) return null;

        Transform realTarget = destinationRegistry.GetPointById(destId);
        if (!realTarget) return null;

        string currentScene = SceneManager.GetActiveScene().name;
        string targetScene = realTarget.gameObject.scene.name;

        if (currentScene != targetScene)
        {
            return warpPoint; // ยังไม่ถึงแมพ → ชี้วาร์ป
        }

        return realTarget; // ถึงแมพแล้ว → ชี้ของจริง
    }
}