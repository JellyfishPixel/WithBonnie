using UnityEngine;

public class BoxInventoryUI : MonoBehaviour
{
    [Header("Root Panel")]
    [Tooltip("Panel หลักของ UI (เปิด/ปิด ทั้งก้อน)")]
    public GameObject rootPanel;

    [Header("Slot UIs (สูงสุด 3 ช่อง)")]
    public BoxInventorySlotUI[] slotUIs;   // ใส่ Slot1, Slot2, Slot3 ตามลำดับ

    [Header("Toggle Key")]
    [Tooltip("ปุ่มบนคีย์บอร์ดสำหรับเปิด/ปิด UI (ใช้ปุ่ม 1 ธรรมดา ไม่ใช่ Numpad)")]
    public KeyCode toggleKey = KeyCode.Alpha1;

    void Start()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);   // เริ่มเกมยังไม่ต้องโชว์
    }

    void Update()
    {
        // กดปุ่ม 1 ธรรมดา (ไม่ใช่ Numpad) เพื่อเปิด/ปิด UI
        if (Input.GetKeyDown(toggleKey))
        {
            if (rootPanel != null)
                rootPanel.SetActive(!rootPanel.activeSelf);
        }

        if (rootPanel == null || !rootPanel.activeSelf) return;

        var inv = BoxInventory.Instance;
        if (inv == null || slotUIs == null) return;

        // อัปเดตแต่ละสล็อตให้ตรงกับ BoxInventory
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var ui = slotUIs[i];
            if (ui == null) continue;

            var slot = inv.GetSlot(i);
            ui.Refresh(slot, i);
        }
    }
}
