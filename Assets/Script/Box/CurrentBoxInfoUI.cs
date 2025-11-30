using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentBoxInfoUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelRoot;
    public KeyCode toggleKey = KeyCode.Tab;

    public TMP_Text nameText;
    public TMP_Text categoryText;
    public TMP_Text qualityText;
    public TMP_Text timeLeftText;
    public Image iconImage;
    public Image timeBarFill;    // image ที่ใช้ fillAmount แสดงเวลาเหลือ

    void Start()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && panelRoot != null)
        {
            panelRoot.SetActive(!panelRoot.activeSelf);
        }

        if (!panelRoot || !panelRoot.activeSelf) return;

        UpdateInfo();
    }

    void UpdateInfo()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.activeBoxes.Count == 0)
        {
            nameText.text = "NO BOX";
            return;
        }

        // ตัวอย่าง: ดูกล่องอันแรกใน activeBoxes (หรือคุณจะให้ผู้เล่นเลือก index ก็ได้)
        var rec = gm.activeBoxes[0];
        var data = rec.data;
        var item = rec.itemInstance;

        if (!data || !item)
            return;

        nameText.text = data.itemName.ToUpperInvariant();
        categoryText.text = data.category.ToString();
        qualityText.text = $"QUALITY {item.currentQuality:0}%";

        int daysPassed = gm.currentDay - rec.dayCreated;
        int daysLeft = data.deliveryLimitDays - daysPassed;
        daysLeft = Mathf.Max(daysLeft, 0);

        timeLeftText.text = $"TIME LEFT: {daysLeft} DAY(S)";

        if (iconImage) iconImage.sprite = data.icon;

        // แถบเวลา: 1.0 = ยังเต็มเวลา, 0.0 = หมดเวลา
        float frac = data.deliveryLimitDays > 0
            ? Mathf.Clamp01((float)daysLeft / data.deliveryLimitDays)
            : 0f;

        if (timeBarFill) timeBarFill.fillAmount = frac;
    }
}
