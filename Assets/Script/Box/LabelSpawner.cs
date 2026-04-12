using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// เครื่องปริ้นฉลากกลางซีน
/// - มีจุด spawn ของตัวเอง
/// - ปริ้น label ออกมาพร้อมอนิเมชั่นเลื่อน (ไม่เปลี่ยน scale)
/// </summary>
public class LabelSpawner : MonoBehaviour, IInteractable
{
    public static LabelSpawner Instance { get; private set; }

    [Header("Label Settings")]
    [Tooltip("Prefab ของฉลากที่จะปริ้นออกมา")]
    public GameObject labelPrefab;

    [Tooltip("จุดที่ฉลากจะ spawn (ปากเครื่องปริ้น)")]
    public Transform spawnPoint;

    [Tooltip("ถ้ากำหนด จะเลื่อนไปตำแหน่งนี้แทนการใช้ forward ของ spawnPoint")]
    public Transform targetPoint;

    [Header("Animation")]
    [Tooltip("เวลารวมของอนิเมชั่นปริ้น")]
    public float duration = 0.6f;

    [Tooltip("ระยะที่ฉลากจะเลื่อนออกมาจากเครื่อง (ใช้เมื่อไม่มี targetPoint)")]
    public float moveDistance = 0.2f;

    [Tooltip("โค้ง easing (ถ้าเว้นว่างจะเป็นเส้นตรง)")]
    public AnimationCurve easing = AnimationCurve.Linear(0, 0, 1, 1);
    [Header("Print Sound")]
    [SerializeField] private AudioClip printSound;
    [Header("Random Material")]
    [SerializeField] private Material[] randomMaterials;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void Interact(PlayerInteractionSystem interactor,
                        PlayerInteractionSystem.InteractionType type)
    {

    }
    public void PrintLabel(BoxCore box)
    {
        if (labelPrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("[LabelSpawner] Missing prefab or spawnPoint");
            return;
        }

        if (box != null)
            box.RememberLabelPrefab(labelPrefab);

        StartCoroutine(SpawnAndAnimate());
    }

    IEnumerator SpawnAndAnimate()
    {
        // สร้างฉลากที่ปากเครื่อง
        GameObject label = Instantiate(labelPrefab, spawnPoint.position, spawnPoint.rotation);

        // 🔀 Random Material
        if (randomMaterials != null && randomMaterials.Length > 0)
        {
            Renderer r = label.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                Material randomMat = randomMaterials[Random.Range(0, randomMaterials.Length)];
                r.material = randomMat; // สำคัญ: ใช้ .material ไม่ใช่ sharedMaterial
            }
        }
        // 🔊 Print Sound (SFX)
        AudioManager.Instance.PlaySFX(
            printSound,
            spawnPoint.position
        );
        // จำ scale เดิมของ prefab ไว้ แล้วไม่ไปยุ่งมันอีก
        Vector3 originalScale = label.transform.localScale;

        float t = 0f;

        Vector3 startPos = spawnPoint.position;
        Vector3 endPos;

        if (targetPoint != null)
        {
            endPos = targetPoint.position;
        }
        else
        {
            // ถ้าไม่มี targetPoint ให้เลื่อนไปตาม forward ของ spawnPoint
            endPos = startPos + spawnPoint.forward * moveDistance;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float e = (easing != null) ? easing.Evaluate(n) : n;

            // เลื่อนตำแหน่งอย่างเดียว
            label.transform.position = Vector3.Lerp(startPos, endPos, e);
            label.transform.localScale = originalScale;

            yield return null;
        }

        label.transform.position = endPos;
        label.transform.localScale = originalScale;
    }
}
