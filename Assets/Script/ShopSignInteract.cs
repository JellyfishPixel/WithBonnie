using UnityEngine;
using TMPro;

public class ShopSignInteract : MonoBehaviour, IInteractable
{
    public TMP_Text signLabel;   // ตัวหนังสือบนป้าย (OPEN / CLOSED)
    public string openText = "OPEN";
    public string closedText = "CLOSED";

    public void Interact(PlayerInteractionSystem player)
    {
        var gm = FindFirstObjectByType<GameManager>();
        if (!gm) return;

        gm.shopIsOpen = !gm.shopIsOpen;

        // อัปเดตสปอว์น
        if (NPCSpawner.Instance != null)
            NPCSpawner.Instance.SetSpawningEnabled(gm.shopIsOpen);

        // อัปเดตตัวหนังสือป้าย
        if (signLabel)
            signLabel.text = gm.shopIsOpen ? openText : closedText;

        // ถ้าปิดร้าน → ไลน์ลูกค้าที่เหลือให้ออก
        if (!gm.shopIsOpen)
        {
            var allCustomers = FindObjectsByType<NPC>(FindObjectsSortMode.None);
            foreach (var c in allCustomers)
            {
                c.ForceExitAndClearItem(); // ลบของแล้วออกทาง exitPoint
            }
        }
    }
}
