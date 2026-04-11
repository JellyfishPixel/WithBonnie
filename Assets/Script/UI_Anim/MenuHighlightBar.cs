using UnityEngine;

public class MenuHighlightBar : MonoBehaviour
{
    public RectTransform highlightBar;
    public float moveTime = 0.2f;
    public LeanTweenType ease = LeanTweenType.easeOutQuad;

    int tweenId = -1;

    public void MoveToButton(RectTransform targetButton)
    {
        if (!highlightBar || !targetButton) return;

        // ตำแหน่ง Y ของปุ่ม (local)
        float targetY = targetButton.anchoredPosition.y;

        Vector2 targetPos = highlightBar.anchoredPosition;
        targetPos.y = targetY;

        // กัน tween ซ้อน
        if (tweenId != -1)
            LeanTween.cancel(tweenId);

        tweenId = LeanTween.moveY(highlightBar, targetY, moveTime)
            .setEase(ease)
            .setIgnoreTimeScale(true)
            .id;
    }
}
