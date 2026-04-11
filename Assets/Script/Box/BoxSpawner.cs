using UnityEngine;

public class BoxSpawner : MonoBehaviour, IInteractable
{
    public Transform spawnPoint;

    [Header("Prefabs")]
    public GameObject boxSmallPrefab;
    public GameObject boxMediumPrefab;
    public GameObject boxLargePrefab;
    public GameObject CoolBox;
    public GameObject boxWaterMediumPrefab;
    public GameObject boxWaterLargePrefab;
    [Header("Box Size นี้ใช้กับจุดกดนี้")]
    public BoxSizeSimple sizeForThisSpawner = BoxSizeSimple.Small;
    [SerializeField] private AudioClip interactSound;


    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
        
        var box = BoxWorkArea.Instance.CurrentBox;
        if (box != null)
        {
            AddSalesPopupUI.ShowMessage(
                "A box is already in the work area.\nFinish it or remove it first."
            );
            return;
        }
        //var eco = EconomyManager.Instance;
        //if (!eco.HasBoxStock())
        //{
        //    AddSalesPopupUI.ShowMessage(
        //        "No boxes left.\nPlease buy more at the shop."
        //    );
        //    return;
        //}


        TrySpawnBox();
    }

    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }
    void TrySpawnBox()
    {
        var eco = EconomyManager.Instance;
        if (!eco) return;

        if (!eco.TryConsumeBox(sizeForThisSpawner))
        {
            Debug.Log("[BoxSpawner] ไม่มีกล่องขนาดนี้ในสต็อก");
            AddSalesPopupUI.ShowMessage("No box left.\nPlease buy more at the shop.");
            return;
        }

        GameObject prefab = null;
        switch (sizeForThisSpawner)
        {
            case BoxSizeSimple.Small: prefab = boxSmallPrefab; break;
            case BoxSizeSimple.Medium: prefab = boxMediumPrefab; break;
            case BoxSizeSimple.Large: prefab = boxLargePrefab; break;
            case BoxSizeSimple.ColdBox: prefab = CoolBox; break;
            case BoxSizeSimple.WaterMedium: prefab = boxWaterMediumPrefab; break;
            case BoxSizeSimple.WaterLarge: prefab = boxWaterLargePrefab; break;
        }

        if (!prefab)
        {
            Debug.LogWarning("[BoxSpawner] Prefab ยังไม่ได้เซ็ต");
            eco.AddBox(sizeForThisSpawner, 1); // คืนสต็อกให้กันพลาด
            return;
        }

        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        PlayInteractSound();
        var shopUI = FindFirstObjectByType<BoxShopUI>();
        if (shopUI != null)
            shopUI.RefreshUI();

        GuideArrowManager.Instance?.NextTarget();
    }
}
