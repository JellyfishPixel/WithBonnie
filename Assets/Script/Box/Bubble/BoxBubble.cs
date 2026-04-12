using System.Collections;
using UnityEngine;

public class BoxBubble : MonoBehaviour
{
    [Header("Visual")]
    public GameObject bubbleObject;

    [Header("Bubble Texture")]
    public Renderer bubbleRenderer;     // MeshRenderer ของบับเบิล
    public Texture basicTexture;        // ลายสำหรับ Basic
    public Texture strongTexture;       // ลายสำหรับ Strong
    public Texture iceTexture;          // ลายสำหรับ Ice

    [Header("Logic")]
    public int maxBubble = 3;
    public float stepY = 0.001f;
    public float scaleDuration = 0.25f;

    int bubbleCount = 0;
    float baseY;
    Coroutine scaleCo;
    void Start()
    {
        if (bubbleObject != null)
        {
            baseY = bubbleObject.transform.localScale.y;
            bubbleObject.SetActive(false);
        }

        // หา Renderer อัตโนมัติถ้าไม่เซ็ตใน Inspector
        if (bubbleRenderer == null && bubbleObject != null)
        {
            bubbleRenderer = bubbleObject.GetComponentInChildren<Renderer>();
        }
    }


    public bool IsFull()
    {
        return bubbleCount >= maxBubble;
    }

    public void AddBubbleUnit(BubbleType bubbleType)
    {
        if (bubbleObject == null)
        {
            Debug.LogError("[BoxBubble] bubbleObject is missing.");
            return;
        }

        if (bubbleCount >= maxBubble)
            return;

        if (!bubbleObject.activeSelf)
            bubbleObject.SetActive(true);

        ApplyVisual(bubbleType);
        bubbleCount++;

        Vector3 s = bubbleObject.transform.localScale;
        float targetY = baseY + stepY * bubbleCount;
        Vector3 target = new Vector3(s.x, targetY, s.z);

        if (scaleCo != null)
            StopCoroutine(scaleCo);

        scaleCo = StartCoroutine(ScaleTo(target, scaleDuration));
    }

    void ApplyVisual(BubbleType bubbleType)
    {
        if (bubbleRenderer == null)
            return;

        Texture tex = null;

        switch (bubbleType)
        {
            case BubbleType.Basic:
                tex = basicTexture;
                break;

            case BubbleType.Strong:
                tex = strongTexture;
                break;

            case BubbleType.Ice:
                tex = iceTexture;
                break;
        }

        if (tex != null)
        {
            bubbleRenderer.material.mainTexture = tex;
        }
        else
        {
            Debug.LogWarning($"[BoxBubble] No texture set for BubbleType: {bubbleType}");
        }
    }
    public bool HasAnyBubble()
    {
        return bubbleCount > 0;
    }



    IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = bubbleObject.transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            bubbleObject.transform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }
        bubbleObject.transform.localScale = target;
        scaleCo = null;
    }
}
