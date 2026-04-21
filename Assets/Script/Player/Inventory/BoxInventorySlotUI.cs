using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class BoxInventorySlotUI : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text nameText;
    public TMP_Text timeText;
    public TMP_Text itemNameText;

    public Slider qualitySlider;
    public Slider timeSlider;

    [Tooltip("ภาพ Fill ของ Slider คุณภาพ")]
    public Image qualityFillImage;
    public Image qualityHandleImage;

    public Sprite normalHandleSprite;
    public Sprite damagedHandleSprite;
    public Sprite brokenHandleSprite;

    [Header("Icon")]
    public Image iconImage;
    public Sprite emptyIcon;

    [Header("Extra Info (Detail Only)")]
    public TMP_Text npcText;
    public TMP_Text addressText;
    public TMP_Text infoText;

    [Tooltip("ภาพ Fill ของ Slider เวลา")]
    public Image timeFillImage;

    [Header("Colors")]
    public Color normalColor = Color.green;
    public Color damagedColor = new Color(1f, 0.6f, 0f);
    public Color brokenColor = Color.red;
    public Color emptyColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    [Header("Quest Pin")]
    public Button pinButton;
    public Image pinIcon;
    public Graphic pinButtonGraphic;
    public TMP_Text pinButtonText;
    public string pinnedLabel = "ONGOING";
    public string unpinnedLabel = "NOT TRACKED";
    public string emptyLabel = "NO QUEST";
    public Color pinActiveColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color pinInactiveColor = new Color(0.62f, 0.62f, 0.62f, 1f);
    public Color pinEmptyColor = new Color(0.4f, 0.4f, 0.4f, 0.85f);

    public enum InventorySlotDisplayMode
    {
        HUD_Small,
        Inventory_Detail,
        Inventory_Bottom
    }

    [Header("Display Mode")]
    public InventorySlotDisplayMode displayMode;
    BoxInventory.BoxSlot boundSlot;
    int boundIndex = -1;

    void Awake()
    {

        ApplyDisplayMode();
    }

    void OnEnable()
    {
        BoxInventory.QuestPinChanged += HandleQuestPinChanged;
    }

    void OnDisable()
    {
        BoxInventory.QuestPinChanged -= HandleQuestPinChanged;
    }



    public void Refresh(BoxInventory.BoxSlot slot, int index)
    {
        boundSlot = slot;
        boundIndex = index;

        ApplyDisplayMode();

        // ===== EMPTY =====
        if (slot == null || !slot.hasBox || slot.itemData == null)
        {
            RefreshPinUI(slot, index);
            ShowEmpty();
            return;
        }

        // ===== FILLED =====
        RefreshPinUI(slot, index);
        ShowFilled(slot);
    }

    void HandleQuestPinChanged()
    {
        RefreshPinUI(boundSlot, boundIndex);
    }

    void ShowEmpty()
    {
        // ---------- ICON ----------
        if (iconImage) iconImage.sprite = emptyIcon;

        // ---------- NAME ----------
        if (itemNameText) itemNameText.text = "Empty";
        if (nameText) nameText.text = "Name :";

        // ---------- TIME ----------
        if (timeText) timeText.text = "-";

        // ---------- SLIDERS ----------
        if (qualitySlider)
        {
            qualitySlider.minValue = 0f;
            qualitySlider.maxValue = 1f;
            qualitySlider.value = 0f;
        }

        if (timeSlider)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.value = 0f;
        }

        // ---------- DETAIL TEXT ----------
        if (npcText) npcText.text = "NPC : -";
        if (addressText) addressText.text = "Address : -";
        if (infoText) infoText.text = "Information : -";

    }

    void ShowFilled(BoxInventory.BoxSlot slot)
    {
        var data = slot.itemData;

        if (iconImage) iconImage.sprite = data.icon;
        if (nameText) nameText.text = data.itemName;
        if (itemNameText)
            itemNameText.text = slot.itemData.itemName;

        if (qualitySlider)
        {
            qualitySlider.minValue = 0f;
            qualitySlider.maxValue = 100f;
            qualitySlider.value = slot.itemQuality;
        }

        if (timeSlider)
        {
            int remaining = Mathf.Max(0, slot.remainingDays);
            int maxDays = data.deliveryLimitDays > 0 ? data.deliveryLimitDays : Mathf.Max(1, remaining);
            timeSlider.minValue = 0;
            timeSlider.maxValue = maxDays;
            timeSlider.value = remaining;
        }

        if (timeText)
        {
            timeText.text = $"Time Left: {slot.remainingDays} day{(slot.remainingDays == 1 ? "" : "s")}";
        }

        if (npcText)
            npcText.text = string.IsNullOrEmpty(slot.ownerNPCName) ? "NPC: Unknown" : $"NPC: {slot.ownerNPCName}";

        if (addressText)
            addressText.text = string.IsNullOrEmpty(slot.address) ? "Address: -" : $"Address: {slot.address}";

        if (infoText)
            infoText.text = string.IsNullOrEmpty(slot.information) ? "Information: -" : $"Information: {slot.information}";

        UpdateQualityHandle(slot);

        Color stateColor = normalColor;
        if (slot.isBroken) stateColor = brokenColor;
        else if (slot.isDamaged) stateColor = damagedColor;

        SetQualityColor(stateColor);
        UpdateTimeColor(slot);
    }

    void UpdateQualityHandle(BoxInventory.BoxSlot slot)
    {
        if (!qualityHandleImage) return;

        if (slot.isBroken)
            qualityHandleImage.sprite = brokenHandleSprite;
        else if (slot.isDamaged)
            qualityHandleImage.sprite = damagedHandleSprite;
        else
            qualityHandleImage.sprite = normalHandleSprite;

        qualityHandleImage.color = Color.white;
    }
    void ApplyDisplayMode()
    {
        bool isHUD = displayMode == InventorySlotDisplayMode.HUD_Small;
        bool isDetail = displayMode == InventorySlotDisplayMode.Inventory_Detail;
        bool isBottom = displayMode == InventorySlotDisplayMode.Inventory_Bottom;

        if (iconImage) iconImage.gameObject.SetActive(isDetail || isBottom);

        // 🔑 ชื่อ
        if (nameText) nameText.gameObject.SetActive(isHUD || isDetail);
        if (itemNameText) itemNameText.gameObject.SetActive(isDetail); // ✅ เพิ่มบรรทัดนี้

        if (timeText) timeText.gameObject.SetActive(isHUD || isDetail);

        // 🔑 detail only
        if (npcText) npcText.gameObject.SetActive(isDetail);
        if (addressText) addressText.gameObject.SetActive(isDetail);
        if (infoText) infoText.gameObject.SetActive(isDetail);

        if (qualitySlider) qualitySlider.gameObject.SetActive(isHUD || isDetail);
        if (timeSlider) timeSlider.gameObject.SetActive(isHUD || isDetail);
    }


    void SetQualityColor(Color c)
    {
        if (qualityFillImage)
            qualityFillImage.color = c;
    }

    void RefreshPinUI(BoxInventory.BoxSlot slot, int index)
    {
        bool supportsPinUI = displayMode != InventorySlotDisplayMode.HUD_Small;
        bool hasQuest = slot != null && slot.hasBox && slot.itemData != null;
        bool canPin = supportsPinUI &&
                      hasQuest &&
                      index >= 0 &&
                      BoxInventory.Instance != null;

        var registry = FindFirstObjectByType<DestinationRegistry>();
        string currentScene = SceneManager.GetActiveScene().name;
        bool isTracked = canPin && BoxInventory.Instance.IsSlotTracked(index, currentScene, registry);

        if (pinButton != null)
        {

            pinButton.onClick.RemoveAllListeners();
            pinButton.interactable = canPin;

            if (canPin)
            {
                pinButton.onClick.AddListener(() =>
                {
                    BoxInventory.Instance.SetPinnedSlot(index);
                });
            }
        }

        if (pinIcon != null)
            pinIcon.color = isTracked
                ? pinActiveColor
                : (hasQuest ? pinInactiveColor : pinEmptyColor);

        if (pinButtonGraphic != null)
            pinButtonGraphic.color = isTracked
                ? pinActiveColor
                : (hasQuest ? pinInactiveColor : pinEmptyColor);

        if (pinButtonText != null)
        {
            if (isTracked)
            {
                pinButtonText.text = pinnedLabel;
                pinButtonText.color = Color.black;
            }
            else if (hasQuest)
            {
                pinButtonText.text = unpinnedLabel;
                pinButtonText.color = Color.white;
            }
            else
            {
                pinButtonText.text = emptyLabel;
                pinButtonText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            }
        }
    }


    void UpdateTimeColor(BoxInventory.BoxSlot slot)
    {
        if (!timeFillImage || slot.itemData == null)
            return;

        int remaining = slot.remainingDays;
        int maxDays = Mathf.Max(1, slot.itemData.deliveryLimitDays);

        float ratio = (float)remaining / maxDays;

        Color c;

        if (ratio > 0.66f)
            c = normalColor;     // เขียว
        else if (ratio > 0.33f)
            c = damagedColor;    // ส้ม
        else
            c = brokenColor;     // แดง

        timeFillImage.color = c;
    }
}
