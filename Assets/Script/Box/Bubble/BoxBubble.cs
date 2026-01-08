using System.Collections;
using UnityEngine;

public class BoxBubble : MonoBehaviour, IInteractable
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
    BoxCore box;

    void Awake()
    {
        box = GetComponentInParent<BoxCore>();
    }

    void Start()
    {
        box = GetComponentInParent<BoxCore>();

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


    // เผื่อยังอยากให้กดที่ Bubble ตรง ๆ ก็ยังใช้ได้
    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        // Mouse0 เท่านั้น
        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
        AddBubble();
    }

    // ⭐ สำคัญ: ให้เป็น public เพื่อให้ "ปุ่ม" ตัวอื่นเรียกได้
    public void AddBubble()
    {
        Debug.Log("========== AddBubble CALLED ==========");

        // 1. เช็ค BoxCore
        if (box == null)
        {
            Debug.LogError("AddBubble STOP: box == NULL (ไม่มี BoxCore บน Parent)");
            return;
        }
        else
        {
            Debug.Log("BoxCore FOUND: " + box.name);
        }

        // 2. เช็ค bubbleObject
        if (bubbleObject == null)
        {
            Debug.LogError("AddBubble STOP: bubbleObject == NULL (ยังไม่ได้ตั้ง bubbleObject ใน Inspector)");
            return;
        }
        else
        {
            Debug.Log("bubbleObject FOUND: " + bubbleObject.name);
        }

        // 3. เช็ค Step จาก BoxCore
        Debug.Log("Box Step = " + box.Step);
        Debug.Log("HasItem = " + box.HasItem);
        Debug.Log("BubbleFull = " + box.BubbleFull);

        // 4. เช็คว่าสามารถใส่ bubble ได้หรือไม่
        bool canAdd = box.CanAddBubble();
        Debug.Log("CanAddBubble() = " + canAdd);

        if (!canAdd)
        {
            Debug.LogWarning("AddBubble CANCELLED: BoxCore.CanAddBubble() == false");
            return;
        }

        // 5. เช็คจำนวน bubble
        Debug.Log($"BubbleCount = {bubbleCount} / {maxBubble}");

        if (bubbleCount >= maxBubble)
        {
            Debug.LogWarning("AddBubble STOP: bubbleCount >= maxBubble (ครบแล้ว)");
            return;
        }

        // 6. แสดง bubble object
        if (!bubbleObject.activeSelf)
        {
            bubbleObject.SetActive(true);
            Debug.Log("bubbleObject.SetActive(true)");
        }
        ApplyVisualByBubbleType();
        // 7. เพิ่มจำนวน
        bubbleCount++;
        Debug.Log("bubbleCount INCREASED -> " + bubbleCount);

        // 8. คำนวณ scale ใหม่
        Vector3 s = bubbleObject.transform.localScale;
        float targetY = baseY + stepY * bubbleCount;
        Vector3 target = new Vector3(s.x, targetY, s.z);

        Debug.Log($"Scale start Y = {s.y} | Target Y = {targetY}");

        // 9. Stop coroutine เก่า
        if (scaleCo != null)
        {
            StopCoroutine(scaleCo);
            Debug.Log("Old coroutine stopped");
        }

        // 10. เริ่ม coroutine ใหม่
        scaleCo = StartCoroutine(ScaleTo(target, scaleDuration));
        Debug.Log("ScaleTo coroutine started");

        if (bubbleCount == 1)
        {
            Debug.Log("Bubble START -> Lock Box");
            box.NotifyBubbleStarted();
        }

        // ✅ ครบจำนวนตามเดิม (เอาไว้ใช้เช็คว่า bubble เต็ม / UI / เอฟเฟกต์)
        if (bubbleCount >= maxBubble)
        {
            Debug.Log("Bubble FULL -> Notify BoxCore");
            box.NotifyBubbleFull();
        }
        Debug.Log("========== AddBubble FINISHED ==========");
    }
    void ApplyVisualByBubbleType()
    {
        if (bubbleRenderer == null || box == null)
            return;

        Texture tex = null;

        switch (box.BubbleType)
        {
            case BubbleType.Basic:   // ธรรมดา
                tex = basicTexture;
                break;

            case BubbleType.Strong:  // แข็งแรง
                tex = strongTexture;
                break;

            case BubbleType.Ice:     // น้ำแข็ง
                tex = iceTexture;
                break;
        }

        if (tex != null)
        {
            bubbleRenderer.material.mainTexture = tex;
        }
        else
        {
            Debug.LogWarning($"[BoxBubble] No texture set for BubbleType: {box.BubbleType}");
        }
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
