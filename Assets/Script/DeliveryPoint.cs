using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    [Header("ปลายทาง (ID ต้องตรงกับของที่จะรับ)")]
    public string destinationId;

    [Header("ข้อความฟีดแบ็ก (Optional)")]
    public string successMessage = "ส่งของสำเร็จ!";
    public string noBoxMessage = "ไม่มีของปลายทางนี้อยู่ในกล่องที่ถืออยู่";

    public void Interact(PlayerInteractionSystem player)
    {
        if (BoxInventory.Instance == null)
        {
            Debug.Log("[DeliveryPoint] BoxInventory.Instance = null");
            ShowMessage(noBoxMessage);
            return;
        }

        // ✅ พยายามส่งของจาก inventory ตาม destinationId
        int reward;
        bool ok = BoxInventory.Instance.TryDeliverFromInventory(destinationId, out reward);

        if (!ok)
        {
            Debug.Log("[DeliveryPoint] ไม่มี slot ที่ปลายทางตรงกันใน BoxInventory");
            ShowMessage(noBoxMessage);
            return;
        }

        // ✅ ถ้าส่งสำเร็จ → บวกเงิน
        if (reward > 0)
        {
  
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(reward);
                AddSalesPopupUI.ShowNotice(reward);
            }
            else
                Debug.LogWarning("[DeliveryPoint] GameManager.Instance = null → เงินไม่ถูกบวก");
        }

        Debug.Log($"[DeliveryPoint] ส่งของปลายทาง {destinationId} สำเร็จ ได้เงิน {reward}");
        ShowMessage(successMessage);
    }

    void ShowMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        // TODO: เชื่อมกับ UI ของคุณ (เช่น popup, dialogue ฯลฯ)
        Debug.Log($"[DeliveryPoint] {msg}");
    }
}
