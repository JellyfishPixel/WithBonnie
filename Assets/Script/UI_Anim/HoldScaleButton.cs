using UnityEngine;
using UnityEngine.EventSystems;

public class HoldScaleButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Hover (Move Only)")]
    public float hoverMoveY = 6f;          // ลอยขึ้นกี่ px
    public float tweenTime = 0.12f;
    public LeanTweenType ease = LeanTweenType.easeOutQuad;

    [Header("Press (Scale Down)")]
    public float pressedScale = 0.9f;      // ตอนกดให้เล็กลง

    [Header("Sound")]
    public string clickSoundId = "ui_click";

    RectTransform rect;
    Vector3 defaultScale;
    Vector2 defaultPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        defaultScale = rect.localScale;
        defaultPos = rect.anchoredPosition;
    }

    // ================= HOVER =================
    public void OnPointerEnter(PointerEventData eventData)
    {
        LeanTween.cancel(rect);

        // ลอยขึ้นอย่างเดียว (ไม่ scale)
        LeanTween.moveY(rect, defaultPos.y + hoverMoveY, tweenTime)
            .setEase(ease)
            .setIgnoreTimeScale(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPositionAndScale();
    }

    // ================= PRESS =================
    public void OnPointerDown(PointerEventData eventData)
    {
        LeanTween.cancel(rect);

        // ตอนกดให้เล็กลง
        LeanTween.scale(rect, defaultScale * pressedScale, tweenTime)
            .setEase(ease)
            .setIgnoreTimeScale(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetPositionAndScale();
        PlayClickSound();
    }

    // ================= RESET =================
    void ResetPositionAndScale()
    {
        LeanTween.cancel(rect);

        LeanTween.moveY(rect, defaultPos.y, tweenTime)
            .setEase(ease)
            .setIgnoreTimeScale(true);

        LeanTween.scale(rect, defaultScale, tweenTime)
            .setEase(ease)
            .setIgnoreTimeScale(true);
    }

    void PlayClickSound()
    {
        AudioManager.Instance.PlayUIById(clickSoundId);

    }
}
