using StarterAssets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("Map UI")]

    public RectTransform mapRect;        // ตัวภาพแมพ (RectTransform ของ MapImage)
    public RectTransform playerIcon;     // ไอคอนผู้เล่น (ลูกศร)
    public RectTransform mapImageRect; // RawImage ด้านใน
    private Vector2 originalImageSize;
    [Header("World Bounds")]
    public Transform worldMin;           // มุมล่างซ้ายของโลก
    public Transform worldMax;           // มุมขวาบนของโลก
    [Header("Minimap Follow")]
    public RectTransform minimapRoot; // กรอบ 100x100
    [Header("Map Sprites")]
    [Header("Map Sprites")]
    public Image mapImage;          // Image ของ minimap
    public Sprite mainMapSprite;    // 🗺️ Main
    public Sprite mapSceneSprite;   // 🧭 Map
    [Header("Expand Settings")]
    public float expandScale = 3f;
    public float tweenTime = 0.35f;

    private bool isBigMapOpen = false;
    private bool isTweening = false;

    private Vector2 originalRootPos;
    private Vector3 originalRootScale;

    [Header("Player")]
    public Transform playerTransform;
    public RectTransform deliveryIconPrefab;   // Prefab icon จุดส่งของ
    private Vector2 originalRootSize;
    [Header("Day Text")]
    public RectTransform dayText;
    public float dayMoveOffsetY = 120f; // ระยะเลื่อนขึ้น

    private Vector2 dayOriginalPos;
    // จุดส่งของในโลก กับ icon บนแมพ
    List<Transform> deliveryTargets = new List<Transform>();
    List<RectTransform> deliveryIcons = new List<RectTransform>();

    [Header("Zoom")]
    public float zoom = 1.5f;          // ค่าเริ่มต้น
    public float minZoom = 0.5f;
    public float maxZoom = 3f;
    public float zoomSpeed = 1f;


    private Vector2 dayTextOriginalPos;
    public Vector2 dayTextTopPos = new Vector2(0, -50f); // ปรับตามต้องการ
    void Start()
    {
        
        if (!mapRect) mapRect = GetComponent<RectTransform>();
        if (playerIcon && playerIcon.parent != mapRect)
            playerIcon.SetParent(mapRect, false);
        originalRootSize = minimapRoot.sizeDelta;
        originalRootPos = minimapRoot.anchoredPosition;
        originalImageSize = mapImageRect.sizeDelta;
        dayOriginalPos = dayText.anchoredPosition;
        // reset position ตอนเริ่ม
        playerIcon.anchoredPosition = Vector2.zero;
        ApplyMapByScene(SceneManager.GetActiveScene().name);

    }

    void Update()
    {

        if (!playerTransform || !playerIcon) return;
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleBigMap();
        }
        //UpdateZoom(); // ⭐ เพิ่มแค่นี้

        // ของเดิมคุณ ใช้ต่อได้เลย
        UpdateIconPosition(playerTransform.position, playerIcon);

        float yaw = playerTransform.eulerAngles.y;
        playerIcon.localEulerAngles = new Vector3(0, 0, -yaw);

        for (int i = deliveryTargets.Count - 1; i >= 0; i--)
        {
            var t = deliveryTargets[i];
            var icon = deliveryIcons[i];
            if (t == null || icon == null) continue;

            UpdateIconPosition(t.position, icon);
        }
        if (!isBigMapOpen)
        {
            UpdateMapFollowFromFrame();
        }
        else
        {
            mapRect.anchoredPosition = Vector2.zero;
        }

    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void ToggleBigMap()
    {
        if (isTweening) return;

        isBigMapOpen = !isBigMapOpen;
        isTweening = true;

        LeanTween.cancel(minimapRoot.gameObject);
        LeanTween.cancel(dayText.gameObject);

        if (isBigMapOpen)
        {
            LeanTween.size(minimapRoot, new Vector2(900, 900), tweenTime)
                .setEaseOutCubic();

            LeanTween.move(minimapRoot, Vector2.zero, tweenTime)
                .setEaseOutCubic();

            Vector2 targetPos = dayOriginalPos + new Vector2(0f, dayMoveOffsetY);

            LeanTween.move(dayText, targetPos, tweenTime)
                .setEaseOutCubic()
                .setOnComplete(() =>
                {
                    isTweening = false;
                });
        }
        else
        {
            LeanTween.size(minimapRoot, originalRootSize, tweenTime)
                .setEaseOutCubic();

            LeanTween.move(minimapRoot, originalRootPos, tweenTime)
                .setEaseOutCubic();

            LeanTween.move(dayText, dayOriginalPos, tweenTime)
                .setEaseOutCubic()
                .setOnComplete(() =>
                {
                    isTweening = false;
                });
        }
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMapByScene(scene.name);
    }

    void ApplyMapByScene(string sceneName)
    {
        if (!mapImage) return;

        if (sceneName == "Map")
        {
            // 🧭 ซีนดูแผนที่
            mapImage.sprite = mapSceneSprite;
        }
        else
        {
            // 🗺️ ซีนเล่นปกติ (Main)
            mapImage.sprite = mainMapSprite;
        }

        Debug.Log($"[Minimap] Map sprite set for scene: {sceneName}");
    }
    void UpdateMapFollowFromFrame()
    {
        if (!mapRect || !minimapRoot || !playerTransform || !worldMin || !worldMax)
            return;

        // ===== 1) world → normalized 0..1 =====
        float nx = Mathf.InverseLerp(worldMin.position.x, worldMax.position.x, playerTransform.position.x);
        float nz = Mathf.InverseLerp(worldMin.position.z, worldMax.position.z, playerTransform.position.z);

        nx = Mathf.Clamp01(nx);
        nz = Mathf.Clamp01(nz);

        // กลับทิศ (ตามของเดิม)
        nx = 1f - nx;
        nz = 1f - nz;

        // ===== 2) ขนาดจริงของ map (รวม zoom) =====
        float scale = mapRect.localScale.x;
        Vector2 mapSize = mapRect.rect.size * scale;
        Vector2 frameSize = minimapRoot.rect.size;

        // ===== 3) ตำแหน่ง player บน map (pixel) =====
        Vector2 mapPixelPos = new Vector2(
            nx * mapSize.x,
            nz * mapSize.y
        );

        // ชดเชย pivot
        mapPixelPos -= new Vector2(
            mapSize.x * mapRect.pivot.x,
            mapSize.y * mapRect.pivot.y
        );

        // ===== 4) เลื่อนแมพให้ player อยู่กลาง =====
        Vector2 desiredMapPos = -mapPixelPos;

        // ===== 5) Clamp ไม่ให้แมพหลุด =====
        float limitX = Mathf.Max(0, (mapSize.x - frameSize.x) * 0.5f);
        float limitY = Mathf.Max(0, (mapSize.y - frameSize.y) * 0.5f);

        desiredMapPos.x = Mathf.Clamp(desiredMapPos.x, -limitX, limitX);
        desiredMapPos.y = Mathf.Clamp(desiredMapPos.y, -limitY, limitY);

        mapRect.anchoredPosition = desiredMapPos;
    }
    void UpdateZoom()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            zoom += scroll * zoomSpeed * Time.deltaTime;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            mapRect.localScale = Vector3.one * zoom;
        }
    }
    void UpdateIconPosition(Vector3 worldPos, RectTransform icon)
    {
        if (!mapRect || !icon || !worldMin || !worldMax) return;

        // 1) แปลง world → 0..1
        float nx = Mathf.InverseLerp(worldMin.position.x, worldMax.position.x, worldPos.x);
        float nz = Mathf.InverseLerp(worldMin.position.z, worldMax.position.z, worldPos.z);

        nx = Mathf.Clamp01(nx);
        nz = Mathf.Clamp01(nz);

        // 🔥 กลับทิศ minimap
        nx = 1f - nx;   // ซ้าย ↔ ขวา
        nz = 1f - nz;   // บน ↔ ล่าง

        // 2) ขนาด minimap จริง (ตาม RectTransform)
        Vector2 mapSize = mapRect.rect.size;   // เช่น 200x200

        // 3) ใช้ pivot ของ mapRect แปลง 0..1 → local pos
        //    ถ้า pivot (0.5,0.5) = ตรงกลาง
        //    ถ้า pivot (0,1) = มุมซ้ายบน
        Vector2 pivot = mapRect.pivot;

        // จุดบน minimap ก่อนคิด pivot (0..1 ไปเป็น pixel)
        Vector2 localPos = new Vector2(
            nx * mapSize.x,
            nz * mapSize.y
        );

        // เลื่อนให้สัมพันธ์กับ pivot
        localPos -= new Vector2(
            mapSize.x * pivot.x,
            mapSize.y * pivot.y
        );

        icon.anchoredPosition = localPos;
    }

    public void RebindWorldBoundsFromScene()
    {
        var bounds = FindFirstObjectByType<MinimapWorldBounds>();

        if (bounds == null)
        {
            Debug.LogWarning("[Minimap] No MinimapWorldBounds found in this scene");
            worldMin = null;
            worldMax = null;
            return;
        }

        worldMin = bounds.worldMin;
        worldMax = bounds.worldMax;

        Debug.Log($"[Minimap] WorldBounds rebound: " +
                  $"min={(worldMin ? worldMin.name : "NULL")} " +
                  $"max={(worldMax ? worldMax.name : "NULL")}");
    }

    public RectTransform RegisterDeliveryTarget(Transform targetWorldTransform)
    {
        if (!targetWorldTransform || !deliveryIconPrefab || !mapRect) return null;

        var icon = Instantiate(deliveryIconPrefab, mapRect);
        icon.anchoredPosition = Vector2.zero;

        deliveryTargets.Add(targetWorldTransform);
        deliveryIcons.Add(icon);

        return icon;
    }
    public void ClearAllDeliveryIcons()
    {
        for (int i = 0; i < deliveryIcons.Count; i++)
        {
            if (deliveryIcons[i] != null)
                Destroy(deliveryIcons[i].gameObject);
        }
        deliveryIcons.Clear();
        deliveryTargets.Clear();
    }

    public void UnregisterIcon(RectTransform icon)
    {
        if (icon == null) return;

        for (int i = deliveryIcons.Count - 1; i >= 0; i--)
        {
            if (deliveryIcons[i] == icon)
            {
                if (deliveryIcons[i] != null)
                    Destroy(deliveryIcons[i].gameObject);
                deliveryIcons.RemoveAt(i);
                deliveryTargets.RemoveAt(i);
                break;
            }
        }
    }
    public Transform GetNearestDeliveryTarget()
    {
        if (!playerTransform || deliveryTargets.Count == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var t in deliveryTargets)
        {
            if (!t) continue;

            float d = Vector3.Distance(playerTransform.position, t.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = t;
            }
        }

        return nearest;
    }

}
