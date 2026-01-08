using UnityEngine;

public class BubbleSpawnButton : MonoBehaviour, IInteractable
{
    public BubbleType bubbleType = BubbleType.Basic;
    private BoxBubble targetBubble;

    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        // Mouse0 เท่านั้น
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
        var currentBox = BoxCore.Current;
        if (currentBox == null)
        {
            Debug.LogWarning("[BubbleSpawnButton] ไม่มีกล่องปัจจุบัน");
            return;
        }

        // ✅ เช็คว่ากล่องนี้ใช้บับเบิลชนิดนี้ได้ไหม (เช่น Ice ต้องใช้กับ ColdBox)
        if (!currentBox.CanUseBubbleType(bubbleType))
        {
            AddSalesPopupUI.ShowMessage("This bubble type can't be used with this box.");
            return;
        }

        // เช็ค eco ว่ามีบับเบิลไหม
        var eco = EconomyManager.Instance;
        if (eco == null)
        {
            Debug.LogWarning("[BubbleSpawnButton] ไม่มี EconomyManager");
            return;
        }

        if (!eco.HasBubbleStock(bubbleType))
        {
            AddSalesPopupUI.ShowMessage("No bubble left.\nPlease buy more at the shop.");
            return;
        }

        // หักสต็อก
        if (!eco.TryConsumeBubble(bubbleType))
            return;

        // ✅ เซ็ตประเภทบับเบิลให้กล่อง (ใช้ในเรื่องดาเมจ / ลาย / ฯลฯ)
        currentBox.ApplyBubbleType(bubbleType);
        currentBox.hasIceBubble = (bubbleType == BubbleType.Ice);

        if (targetBubble == null)
            targetBubble = currentBox.GetComponentInChildren<BoxBubble>(true);

        if (targetBubble == null)
        {
            Debug.LogWarning("[BubbleSpawnButton] ไม่พบ BoxBubble");
            return;
        }

        // ตรงนี้ AddBubble จะไปดู BoxCore.BubbleType แล้วเรียก ApplyVisualByBubbleType ให้เอง
        targetBubble.AddBubble();
    }

}
