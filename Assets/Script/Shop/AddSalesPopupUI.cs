using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AddSalesPopupUI : MonoBehaviour
{
    public static AddSalesPopupUI Instance;

    [Header("Refs")]
    public TextMeshProUGUI priceText;
    public RectTransform rect;

    [Tooltip("Canvas root")]
    public RectTransform popupLayer;

    [Tooltip("จุด anchor นิ่ง (เช่น TopLeftAnchor)")]
    public RectTransform anchorAt;

    [Header("Motion")]
    public Vector2 startOffset = new Vector2(0f, -20f);
    public Vector2 endOffset = new Vector2(0f, 40f);
    public float moveDuration = 0.4f;
    public float holdDuration = 1.0f;
    public float fadeDuration = 1.2f;

    CanvasGroup cg;
    Coroutine co;
    bool isAnimating;
    [Header("Sound")]
    public AudioClip noticeSound;
    public AudioClip messageSound;
    // 🔒 cache anchor
    Vector2 cachedAnchorLocal;
    bool hasCachedAnchor;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (!rect) rect = GetComponent<RectTransform>();

        if (!popupLayer)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas) popupLayer = canvas.transform as RectTransform;
        }

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        var le = GetComponent<LayoutElement>();
        if (!le) le = gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        HideImmediate();
    }

    void HideImmediate()
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (priceText) priceText.text = "";
    }

    // ================= PUBLIC API =================

    public static void ShowNotice(int amount, bool playSound = false)
    {
        if (Instance == null) return;

        LeanTween.cancel(Instance.gameObject);

        Instance.InternalShow($"Delivery complete!\n+{amount:N0}$");

        if (playSound)
        {
            AudioManager.Instance.PlaySFX(
                Instance.noticeSound,
                Instance.transform.position);
        }
    }
    public static void ShowMessage(string message)
    {
        if (Instance == null) return;

        LeanTween.cancel(Instance.gameObject);

        Instance.InternalShow(message);

        // 🔊 เสียงแจ้งเตือน
        if (Instance.messageSound != null)
        {
            AudioManager.Instance.PlaySFX(
                Instance.messageSound,
                Instance.transform.position
            );
        }
    }


    // ================= CORE =================

    void CacheAnchorIfNeeded()
    {
        if (hasCachedAnchor) return;
        if (!anchorAt || !popupLayer) return;

        var canvas = popupLayer.GetComponentInParent<Canvas>();
        Camera cam = (canvas && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            ? canvas.worldCamera
            : null;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, anchorAt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            popupLayer, screen, cam, out cachedAnchorLocal);

        hasCachedAnchor = true;
    }
    void InternalShow(string message)
    {
        if (priceText)
            priceText.text = message;

        if (popupLayer && rect.parent != popupLayer)
            rect.SetParent(popupLayer, false);

        CacheAnchorIfNeeded();

        Vector2 startPos = cachedAnchorLocal + startOffset;
        Vector2 midPos = cachedAnchorLocal + endOffset;

        rect.anchoredPosition = startPos;
        cg.alpha = 0f;

        isAnimating = true;

        LeanTween.cancel(gameObject);

        // ===== POP SCALE =====
        rect.localScale = Vector3.one * 0.8f;

        LeanTween.scale(rect, Vector3.one, 0.25f)
            .setEaseOutBack();

        // ===== MOVE UP =====
        LeanTween.move(rect, midPos, 0.5f)
            .setEaseOutCubic();

        // ===== FADE IN =====
        LeanTween.alphaCanvas(cg, 1f, 0.3f);

        // ===== HOLD + FADE OUT =====
        LeanTween.delayedCall(gameObject, holdDuration, () =>
        {
            LeanTween.move(rect, startPos, fadeDuration)
                .setEaseInCubic();

            LeanTween.alphaCanvas(cg, 0f, fadeDuration)
                .setOnComplete(() =>
                {
                    HideImmediate();
                    isAnimating = false;
                });
        });
    }
    public static void ShowSticky(string message)
    {
        if (Instance == null) return;

        LeanTween.cancel(Instance.gameObject);

        Instance.InternalShowSticky(message);
    }

    public static void HideSticky()
    {
        if (Instance == null) return;

        LeanTween.cancel(Instance.gameObject);
        Instance.HideImmediate();
    }
    void InternalShowSticky(string message)
    {
        if (priceText)
            priceText.text = message;

        if (popupLayer && rect.parent != popupLayer)
            rect.SetParent(popupLayer, false);

        CacheAnchorIfNeeded();

        Vector2 pos = cachedAnchorLocal + endOffset;

        rect.anchoredPosition = pos;
        rect.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        LeanTween.scale(rect, Vector3.one, 0.25f)
            .setEaseOutBack();

        LeanTween.alphaCanvas(cg, 1f, 0.25f);
    }
}

