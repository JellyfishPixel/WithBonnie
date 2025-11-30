using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AddSalesPopupUI : MonoBehaviour
{
    public static AddSalesPopupUI Instance;

    [Header("Refs")]
    public TextMeshProUGUI priceText;
    public RectTransform rect;          // ตัว popup เอง
    [Tooltip("เลเยอร์สำหรับวาง popup (ส่วนใหญ่คือ Canvas root)")]
    public RectTransform popupLayer;
    [Tooltip("จุดอ้างอิงให้ popup เด้ง (เช่น ตำแหน่งเงิน /มุมจอ)")]
    public RectTransform anchorAt;

    [Header("Motion (รวม ๆ ประมาณ 2 วินาที)")]
    public Vector2 startOffset = new Vector2(0f, -20f);   // เริ่มต่ำกว่าจุด anchor นิดหน่อย
    public Vector2 endOffset = new Vector2(0f, 40f);    // เด้งขึ้นสูงกว่าจุด anchor
    public float moveDuration = 0.4f;                    // ขึ้น
    public float holdDuration = 1.0f;                    // ค้าง
    public float fadeDuration = 1.2f;                    // ลง+จางหาย

    CanvasGroup cg;
    Coroutine co;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // ===== auto assign ให้ขี้เกียจเซ็ตใน Inspector ได้เลย =====
        if (!rect) rect = GetComponent<RectTransform>();

        if (!popupLayer)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) popupLayer = canvas.transform as RectTransform;
            else if (rect != null) popupLayer = rect.parent as RectTransform;
        }

        // ถ้า anchor ไม่ได้ตั้ง จะใช้ popupLayer เองเป็นจุดอ้างอิงกลางจอ
        if (!anchorAt && popupLayer != null)
            anchorAt = popupLayer;

        // ===== CanvasGroup =====
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        // ใส่ LayoutElement เพื่อให้ไม่ถูกรบกวนโดย LayoutGroup ต่าง ๆ
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

    // ===== static helper =====
    public static void ShowNotice(int amount)
    {
        if (Instance == null) return;
        Instance.InternalShow(amount);
    }

    void InternalShow(int amount)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Animate(amount));
    }

    // หา screen-position ของ anchor
    Vector2 GetAnchorScreenPos()
    {
        // ถ้าไม่มี anchor เลย ใช้กลางหน้าจอไปก่อน
        if (anchorAt == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        var canvas = anchorAt.GetComponentInParent<Canvas>();
        Camera camForUI = null;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            camForUI = canvas.worldCamera;

        return RectTransformUtility.WorldToScreenPoint(camForUI, anchorAt.position);
    }

    IEnumerator Animate(int amount)
    {
        // 1) ข้อความ popup
        if (priceText)
        {
            // ตัวอย่าง: "Delivery complete!  +100$"
            priceText.text = $"Delivery complete!\n+{amount:N0}$";
        }

        // 2) ย้าย parent ไปอยู่ใต้ popupLayer
        if (popupLayer != null && rect.parent != popupLayer)
            rect.SetParent(popupLayer, false);

        // 3) คำนวณ local pos จาก anchor
        Vector2 screen = GetAnchorScreenPos();
        Vector2 local = Vector2.zero;

        var canvas = popupLayer != null ? popupLayer.GetComponentInParent<Canvas>() : null;
        Camera camForUI = null;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            camForUI = canvas.worldCamera;

        if (popupLayer != null)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, screen, camForUI, out local);

        Vector2 startPos = local + startOffset;
        Vector2 midPos = local + endOffset;   // จุดสูงสุด
        Vector2 endPos = local + startOffset; // ลงกลับมาต่ำ ๆ แล้วหายไป

        rect.anchoredPosition = startPos;
        cg.alpha = 0f;

        // 4) ขึ้น + fade-in
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / moveDuration);
            float ease = 1f - Mathf.Pow(1f - p, 3f); // ease-out

            rect.anchoredPosition = Vector2.Lerp(startPos, midPos, ease);
            cg.alpha = Mathf.Lerp(0f, 1f, ease);
            yield return null;
        }

        // 5) ค้างเฉย ๆ
        yield return new WaitForSecondsRealtime(holdDuration);

        // 6) ลง + fade-out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);
            rect.anchoredPosition = Vector2.Lerp(midPos, endPos, p);
            cg.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        HideImmediate();
        co = null;
    }
}
