using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    //public InventoryBottomUI bottomUI;
    //public InventoryDetailUI detailUI;
    //public MinimapController minimap;

    //int selectedSlotIndex = -1;
    //void Start()
    //{
    //    bottomUI.Init(this);
    //}
    //public void OnInventoryOpened()
    //{
    //    selectedSlotIndex = FindClosestSlotFromMinimap();
    //    bottomUI.Refresh();  
    //    RefreshDetail();       
    //}


    //int FindClosestSlotFromMinimap()
    //{
    //    if (!minimap || BoxInventory.Instance == null)
    //        return -1;

    //    Transform nearestTarget = minimap.GetNearestDeliveryTarget();
    //    if (!nearestTarget) return -1;

    //    // 🔑 เอา Transform → destinationId
    //    string destinationId = nearestTarget.name;
    //    // ⚠️ แนะนำ: ใส่ DestinationIdComponent จะดีกว่า (อธิบายด้านล่าง)

    //    return BoxInventory.Instance.FindSlotByDestination(destinationId);
    //}

    //public void SelectSlot(int index)
    //{
    //    selectedSlotIndex = index;
    //    RefreshDetail();
    //}

    //void RefreshDetail()
    //{
    //    var slot = BoxInventory.Instance.GetSlot(selectedSlotIndex);
    //    detailUI.Refresh(slot);
    //}


}
