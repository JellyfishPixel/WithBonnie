using UnityEngine;

public class BubbleSpawnButton : MonoBehaviour, IInteractable
{
    // ไม่ต้องเซ็ตใน Inspector แล้ว เพราะเราจะหาเองจาก current box
    private BoxBubble targetBubble;

    public void Interact(PlayerInteractionSystem interactor)
    {
        // 1) หา current box ก่อน
        var currentBox = BoxCore.Current;
        if (currentBox == null)
        {
            Debug.LogWarning("[BubbleSpawnButton] ไม่มี BoxCore.Current (ยังไม่มีกล่องปัจจุบัน?)");
            return;
        }

        // 2) จาก current box หา BoxBubble ที่เป็นลูก (Child) ใน Hierarchy
        if (targetBubble == null)
        {
            targetBubble = currentBox.GetComponentInChildren<BoxBubble>(true);

            if (targetBubble == null)
            {
                Debug.LogWarning($"[BubbleSpawnButton] ไม่พบ BoxBubble ใต้กล่อง: {currentBox.name}");
                return;
            }
        }

        // 3) เรียก AddBubble ของกล่องปัจจุบัน
        Debug.Log($"[BubbleSpawnButton] AddBubble บนกล่อง: {currentBox.name}");
        targetBubble.AddBubble();
    }
}
