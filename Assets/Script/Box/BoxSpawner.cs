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

    public BoxCore currentBox;

    public void Interact(PlayerInteractionSystem player)
    {
        currentBox = FindFirstObjectByType<BoxCore>();
        if(currentBox != null)  return;
        TrySpawnBox();
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
            eco.AddBox(sizeForThisSpawner, 1);
            return;
        }

        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }
}
