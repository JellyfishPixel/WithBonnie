using UnityEngine;

public class UIPopup : MonoBehaviour
{
    public enum AnimationMode
    {
        Popup,      // Default
        Slide
    }

    public enum SlideDirection
    {
        Left,
        Right
    }

    [Header("Animation Mode")]
    public AnimationMode animationMode = AnimationMode.Popup;

    [Header("Slide Settings")]
    public SlideDirection slideDirection = SlideDirection.Right;
    public float slideDistance = 600f;

    [Header("Canvas")]
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    public float openTime = 0.25f;
    public float closeTime = 0.2f;

    Vector3 defaultScale;
    Vector3 defaultPos;
    [Header("Block World Input")]
    public bool blockWorldInput = true;
    void Awake()
    {
        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        defaultScale = transform.localScale;
        defaultPos = transform.localPosition;
    }

    void OnEnable()
    {
        if (blockWorldInput)
            PlayerInteractionSystem.BlockWorldInput = true;

        PlayOpen();
    }
    // ================= OPEN =================

    public void PlayOpen()
    {
        LeanTween.cancel(gameObject);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        switch (animationMode)
        {
            case AnimationMode.Popup:
                OpenPopup();
                break;

            case AnimationMode.Slide:
                OpenSlide();
                break;
        }
    }

    void OpenPopup()
    {
        transform.localScale = defaultScale * 0.8f;
        canvasGroup.alpha = 0f;

        LeanTween.scale(gameObject, defaultScale, openTime)
            .setEase(LeanTweenType.easeOutBack)
            .setIgnoreTimeScale(true);

        LeanTween.alphaCanvas(canvasGroup, 1f, openTime)
            .setIgnoreTimeScale(true);
    }

    void OpenSlide()
    {
        Vector3 startPos = defaultPos;

        if (slideDirection == SlideDirection.Left)
            startPos.x -= slideDistance;
        else
            startPos.x += slideDistance;

        transform.localPosition = startPos;
        canvasGroup.alpha = 0f;

        LeanTween.moveLocal(gameObject, defaultPos, openTime)
            .setEase(LeanTweenType.easeOutCubic)
            .setIgnoreTimeScale(true);

        LeanTween.alphaCanvas(canvasGroup, 1f, openTime)
            .setIgnoreTimeScale(true);
    }

    // ================= CLOSE =================

    public void PlayClose()
    {
        LeanTween.cancel(gameObject);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        switch (animationMode)
        {
            case AnimationMode.Popup:
                ClosePopup();
                break;

            case AnimationMode.Slide:
                CloseSlide();
                break;
        }
    }

    void ClosePopup()
    {
        LeanTween.scale(gameObject, defaultScale * 0.85f, closeTime)
            .setEase(LeanTweenType.easeInQuad)
            .setIgnoreTimeScale(true);

        LeanTween.alphaCanvas(canvasGroup, 0f, closeTime)
            .setIgnoreTimeScale(true)
           .setOnComplete(() =>
           {
               if (blockWorldInput)
                   PlayerInteractionSystem.BlockWorldInput = false;

               gameObject.SetActive(false);
           });
    }

    void CloseSlide()
    {
        Vector3 endPos = defaultPos;

        if (slideDirection == SlideDirection.Left)
            endPos.x -= slideDistance;
        else
            endPos.x += slideDistance;

        LeanTween.moveLocal(gameObject, endPos, closeTime)
            .setEase(LeanTweenType.easeInCubic)
            .setIgnoreTimeScale(true);

        LeanTween.alphaCanvas(canvasGroup, 0f, closeTime)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                transform.localPosition = defaultPos;
                gameObject.SetActive(false);
            });
    }
}