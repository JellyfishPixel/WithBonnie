using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class BoxInventorySlotUI : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text nameText;
    public TMP_Text timeText;

    public Slider qualitySlider;
    public Slider timeSlider;

    [Tooltip("ภาพ Fill ของ Slider คุณภาพ (ดึง child Image ของ Slider มาใส่)")]
    public Image qualityFillImage;

    [Tooltip("ภาพ Fill ของ Slider เวลา (จะเปลี่ยนสีตาม state ด้วย)")]
    public Image timeFillImage;

    [Header("Colors")]
    public Color normalColor = Color.green;                     // ปกติ
    public Color damagedColor = new Color(1f, 0.6f, 0f);        // ส้ม
    public Color brokenColor = Color.red;                       // แดง
    public Color emptyColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);// เทาเวลาไม่มีของ

    public void Start()
    {
        if (qualityFillImage) qualityFillImage.color = normalColor;
        if (timeFillImage) timeFillImage.color = normalColor;
    }
    public void Refresh(BoxInventory.BoxSlot slot, int index)
    {
        if (slot == null || !slot.hasBox || slot.itemData == null)
        {
            // สล็อตว่าง
            if (nameText) nameText.text = $"Slot {index + 1}: Empty";
            if (timeText) timeText.text = "-";

            if (qualitySlider)
            {
                qualitySlider.maxValue = 100f;
                qualitySlider.value = 0f;
            }
            if (timeSlider)
            {
                timeSlider.maxValue = 1f;
                timeSlider.value = 0f;
            }

            SetColor(emptyColor);
            return;
        }

        // ----- มีของในสล็อต -----
        var data = slot.itemData;

        // ชื่อ
        if (nameText)
        {
            nameText.text = data.itemName;
        }

        // คุณภาพ
        if (qualitySlider)
        {
            qualitySlider.minValue = 0f;
            qualitySlider.maxValue = 100f;
            qualitySlider.value = slot.itemQuality;
        }

        // เวลาเหลือ
        int remaining = Mathf.Max(0, slot.remainingDays);
        int maxDays = data.deliveryLimitDays > 0
            ? data.deliveryLimitDays
            : Mathf.Max(1, remaining); // กัน max = 0

        if (timeSlider)
        {
            timeSlider.minValue = 0;
            timeSlider.maxValue = maxDays;
            timeSlider.value = remaining;
        }

        if (timeText)
        {
            timeText.text = $"Time Left: {remaining} day{(remaining == 1 ? "" : "s")}";
        }

        // ----- เลือกสีตามสถานะ -----
        Color stateColor = normalColor;
        if (slot.isBroken)
            stateColor = brokenColor;
        else if (slot.isDamaged)
            stateColor = damagedColor;

        SetColor(stateColor);
    }

    void SetColor(Color c)
    {
        if (qualityFillImage) qualityFillImage.color = c;
        if (timeFillImage) timeFillImage.color = c;
    }
}
