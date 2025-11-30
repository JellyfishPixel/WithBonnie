using UnityEngine;

public class CustomerDialogueInteract : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public NPC npc;                   // ลาก NPC ตัวนี้มา
    public DeliveryItemData itemData; // ไอเท็มของลูกค้าตัวนี้ (ใช้เอา dialogue)

    [Header("Choices index mapping")]
    [Tooltip("index ของช้อยส์ที่ถือว่าเป็น Accept")]
    public int acceptIndex = 0;
    [Tooltip("index ของช้อยส์ที่ถือว่าเป็น Decline")]
    public int declineIndex = 1;

    public void Interact(PlayerInteractionSystem player)
    {
        if (!ItemDialogueManager.Instance || !itemData || !itemData.dialogueData)
        {
            Debug.LogWarning("[CustomerDialogueInteract] Missing itemData/dialogueData/manager.");
            return;
        }

        // actorOwner = NPC ตัวนี้ ใช้จำว่าเคยคุยครั้งแรก/ครั้งต่อไป
        ItemDialogueManager.Instance.Show(
            actorOwner: npc ? npc.gameObject : gameObject,
            flow: itemData.dialogueData,
            onChoice: (choiceIdx) =>
            {
                if (!npc) return;

                if (choiceIdx == acceptIndex)
                    npc.OnAcceptDelivery();
                else if (choiceIdx == declineIndex)
                    npc.OnDeclineDelivery();
            },
            onFinished: () =>
            {
                // ถ้าต้องทำอะไรหลังจบคุย (แต่ไม่สำคัญก็ปล่อยว่างไว้ได้)
            }
        );
    }
}
