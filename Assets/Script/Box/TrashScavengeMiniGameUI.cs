using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrashScavengeMiniGameUI : MonoBehaviour
{
    static TrashScavengeMiniGameUI instance;

    TrashBinScavenge currentBin;

    [Header("Optional Scene References")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] CanvasGroup assignedCanvasGroup;
    [SerializeField] TextMeshProUGUI assignedTitleText;
    [SerializeField] TextMeshProUGUI assignedHintText;
    [SerializeField] Slider assignedProgressSlider;
    [SerializeField] Slider assignedCursorSlider;
    [SerializeField] RectTransform assignedTargetZone;

    Canvas canvas;
    CanvasGroup canvasGroup;
    RectTransform panelRect;
    RectTransform laneRect;
    RectTransform targetRect;
    Slider cursorSlider;
    Slider progressSlider;
    TextMeshProUGUI titleText;
    TextMeshProUGUI hintText;

    float cursorNormalized;
    float targetCenter;
    float targetWidth;
    float successProgress;
    bool isShowing;

    const float HoldSpeed = 0.85f;
    const float DriftSpeed = 0.55f;
    const float SuccessFillSpeed = 0.9f;
    const float SuccessDrainSpeed = 0.45f;
    const float MinTargetWidth = 0.18f;
    const float MaxTargetWidth = 0.28f;

    public static void Show(TrashBinScavenge bin, int remainingAttempts)
    {
        if (bin == null)
            return;

        if (instance == null)
        {
            instance = FindFirstObjectByType<TrashScavengeMiniGameUI>(FindObjectsInactive.Include);

            if (instance == null)
                CreateInstance();
            else
                instance.SetupUI();
        }

        instance.Begin(bin, remainingAttempts);
    }

    static void CreateInstance()
    {
        var go = new GameObject("TrashScavengeMiniGameUI");
        instance = go.AddComponent<TrashScavengeMiniGameUI>();
        DontDestroyOnLoad(go);
        instance.SetupUI();
    }

    void Awake()
    {
        if (instance != null && instance != this)
            return;

        instance = this;
        SetupUI();
    }

    void SetupUI()
    {
        if (panelRect != null && cursorSlider != null && progressSlider != null && targetRect != null)
        {
            HideImmediate();
            return;
        }

        if (TryBindAssignedUI())
        {
            HideImmediate();
            return;
        }

        if (!TryBindSceneUI())
            BuildFallbackUI();

        HideImmediate();
    }

    bool TryBindAssignedUI()
    {
        if (panelRoot != null)
            panelRect = panelRoot.GetComponent<RectTransform>();

        if (panelRect == null && assignedCursorSlider != null)
            panelRect = assignedCursorSlider.GetComponentInParent<RectTransform>();

        canvasGroup = assignedCanvasGroup;
        titleText = assignedTitleText;
        hintText = assignedHintText;
        progressSlider = assignedProgressSlider;
        cursorSlider = assignedCursorSlider;
        targetRect = assignedTargetZone;

        if (panelRect == null || cursorSlider == null || progressSlider == null || targetRect == null)
            return false;

        if (canvasGroup == null)
            canvasGroup = panelRect.GetComponent<CanvasGroup>() ?? panelRect.gameObject.AddComponent<CanvasGroup>();

        laneRect = cursorSlider.GetComponent<RectTransform>();
        canvas = panelRect.GetComponentInParent<Canvas>();

        cursorSlider.minValue = 0f;
        cursorSlider.maxValue = 1f;
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        return true;
    }

    bool TryBindSceneUI()
    {
        panelRect = FindRect("TrashScavengePanel");
        if (panelRect == null)
            return false;

        canvas = panelRect.GetComponentInParent<Canvas>();
        canvasGroup = panelRect.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panelRect.gameObject.AddComponent<CanvasGroup>();

        titleText = FindInChildren<TextMeshProUGUI>(panelRect, "TitleText");
        hintText = FindInChildren<TextMeshProUGUI>(panelRect, "HintText");
        progressSlider = FindInChildren<Slider>(panelRect, "ProgressSlider");
        cursorSlider = FindInChildren<Slider>(panelRect, "CursorSlider");

        if (cursorSlider == null || progressSlider == null)
            return false;

        laneRect = cursorSlider.GetComponent<RectTransform>();
        targetRect = cursorSlider.handleRect;
        if (targetRect == null)
            targetRect = FindInChildren<RectTransform>(panelRect, "TargetZone");

        if (targetRect == null)
            return false;

        cursorSlider.minValue = 0f;
        cursorSlider.maxValue = 1f;
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;

        return true;
    }

    void BuildFallbackUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        gameObject.AddComponent<GraphicRaycaster>();
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        panelRect = CreateRect("Panel", transform, new Vector2(760f, 320f));
        var panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);

        titleText = CreateText("Title", panelRect, "Trash Search", 42, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -28f);
        titleText.rectTransform.sizeDelta = new Vector2(640f, 60f);

        hintText = CreateText("Hint", panelRect, "", 26, FontStyles.Normal);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        hintText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        hintText.rectTransform.pivot = new Vector2(0.5f, 1f);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, -90f);
        hintText.rectTransform.sizeDelta = new Vector2(660f, 70f);

        laneRect = CreateRect("Lane", panelRect, new Vector2(620f, 34f));
        laneRect.anchorMin = new Vector2(0.5f, 0.5f);
        laneRect.anchorMax = new Vector2(0.5f, 0.5f);
        laneRect.pivot = new Vector2(0.5f, 0.5f);
        laneRect.anchoredPosition = new Vector2(0f, 6f);
        var laneImage = laneRect.gameObject.AddComponent<Image>();
        laneImage.color = new Color(0.2f, 0.24f, 0.3f, 1f);

        targetRect = CreateRect("Target", laneRect, new Vector2(120f, 34f));
        targetRect.anchorMin = new Vector2(0f, 0.5f);
        targetRect.anchorMax = new Vector2(0f, 0.5f);
        targetRect.pivot = new Vector2(0.5f, 0.5f);
        var targetImage = targetRect.gameObject.AddComponent<Image>();
        targetImage.color = new Color(0.24f, 0.78f, 0.42f, 0.95f);

        cursorSlider = laneRect.gameObject.AddComponent<Slider>();
        cursorSlider.direction = Slider.Direction.LeftToRight;
        cursorSlider.minValue = 0f;
        cursorSlider.maxValue = 1f;
        cursorSlider.value = 0f;

        var cursorFillArea = CreateRect("CursorFillArea", laneRect, Vector2.zero);
        cursorFillArea.anchorMin = Vector2.zero;
        cursorFillArea.anchorMax = Vector2.one;
        cursorFillArea.offsetMin = new Vector2(4f, 4f);
        cursorFillArea.offsetMax = new Vector2(-4f, -4f);

        var cursorFill = CreateRect("CursorFill", cursorFillArea, Vector2.zero);
        cursorFill.anchorMin = new Vector2(0f, 0f);
        cursorFill.anchorMax = new Vector2(1f, 1f);
        cursorFill.offsetMin = Vector2.zero;
        cursorFill.offsetMax = Vector2.zero;
        var cursorFillImage = cursorFill.gameObject.AddComponent<Image>();
        cursorFillImage.color = new Color(0.97f, 0.97f, 0.98f, 0.9f);

        cursorSlider.fillRect = cursorFill;
        cursorSlider.handleRect = targetRect;
        cursorSlider.targetGraphic = targetImage;

        var progressRoot = CreateRect("ProgressRoot", panelRect, new Vector2(620f, 28f));
        progressRoot.anchorMin = new Vector2(0.5f, 0f);
        progressRoot.anchorMax = new Vector2(0.5f, 0f);
        progressRoot.pivot = new Vector2(0.5f, 0f);
        progressRoot.anchoredPosition = new Vector2(0f, 72f);

        progressSlider = progressRoot.gameObject.AddComponent<Slider>();
        progressSlider.direction = Slider.Direction.LeftToRight;
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;

        var progressBackground = CreateRect("Background", progressRoot, Vector2.zero);
        progressBackground.anchorMin = Vector2.zero;
        progressBackground.anchorMax = Vector2.one;
        progressBackground.offsetMin = Vector2.zero;
        progressBackground.offsetMax = Vector2.zero;
        var progressBgImage = progressBackground.gameObject.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.24f, 0.3f, 1f);

        var progressFillArea = CreateRect("Fill Area", progressRoot, Vector2.zero);
        progressFillArea.anchorMin = Vector2.zero;
        progressFillArea.anchorMax = Vector2.one;
        progressFillArea.offsetMin = new Vector2(4f, 4f);
        progressFillArea.offsetMax = new Vector2(-4f, -4f);

        var progressFill = CreateRect("Fill", progressFillArea, Vector2.zero);
        progressFill.anchorMin = new Vector2(0f, 0f);
        progressFill.anchorMax = new Vector2(1f, 1f);
        progressFill.offsetMin = Vector2.zero;
        progressFill.offsetMax = Vector2.zero;
        var progressFillImage = progressFill.gameObject.AddComponent<Image>();
        progressFillImage.color = new Color(0.98f, 0.8f, 0.27f, 1f);

        progressSlider.targetGraphic = progressFillImage;
        progressSlider.fillRect = progressFill;
    }

    void Begin(TrashBinScavenge bin, int remainingAttempts)
    {
        currentBin = bin;
        cursorNormalized = 0.08f;
        successProgress = 0f;
        progressSlider.value = 0f;
        cursorSlider.value = cursorNormalized;
        targetWidth = Random.Range(MinTargetWidth, MaxTargetWidth);
        targetCenter = Random.Range(targetWidth * 0.5f, 1f - targetWidth * 0.5f);
        isShowing = true;

        if (titleText != null)
            titleText.text = "Trash Search";

        if (hintText != null)
            hintText.text = $"Hold Left Mouse Button and keep the fill inside the target.\nAttempts left today: {Mathf.Max(0, remainingAttempts)}";

        UpdateVisuals();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            panelRect.gameObject.SetActive(true);

        UILockManager.Instance?.PushLock(this, UILockOptions.Dialogue);
    }

    void Update()
    {
        if (!isShowing)
            return;

        float delta = Time.unscaledDeltaTime;

        if (Input.GetMouseButton(0))
            cursorNormalized += HoldSpeed * delta;
        else
            cursorNormalized -= DriftSpeed * delta;

        cursorNormalized = Mathf.Clamp01(cursorNormalized);

        bool insideTarget = Mathf.Abs(cursorNormalized - targetCenter) <= targetWidth * 0.5f;
        if (insideTarget)
            successProgress += SuccessFillSpeed * delta;
        else
            successProgress -= SuccessDrainSpeed * delta;

        successProgress = Mathf.Clamp01(successProgress);
        progressSlider.SetValueWithoutNotify(successProgress);

        UpdateVisuals();

        if (successProgress >= 1f)
        {
            Complete(true);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Complete(false);
            return;
        }
    }

    void UpdateVisuals()
    {
        if (laneRect == null || targetRect == null || cursorSlider == null)
            return;

        float laneWidth = laneRect.rect.width;

        targetRect.sizeDelta = new Vector2(laneWidth * targetWidth, targetRect.sizeDelta.y);
        targetRect.anchoredPosition = new Vector2(laneWidth * targetCenter, 0f);
        cursorSlider.SetValueWithoutNotify(cursorNormalized);
    }

    void Complete(bool success)
    {
        TrashBinScavenge bin = currentBin;
        currentBin = null;
        isShowing = false;

        HideImmediate();
        UILockManager.Release(this);

        if (bin != null)
            bin.CompleteMiniGame(success);
    }

    void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else if (panelRect != null)
            panelRect.gameObject.SetActive(false);
    }

    RectTransform CreateRect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        return rect;
    }

    TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, FontStyles fontStyle)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = Color.white;
        return label;
    }

    static RectTransform FindRect(string name)
    {
        var allRects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rect in allRects)
        {
            if (rect != null && rect.name == name)
                return rect;
        }

        return null;
    }

    static T FindInChildren<T>(Transform root, string name) where T : Component
    {
        if (root == null)
            return null;

        var items = root.GetComponentsInChildren<T>(true);
        foreach (var item in items)
        {
            if (item != null && item.name == name)
                return item;
        }

        return null;
    }
}
