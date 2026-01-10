using StarterAssets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("Map UI")]
    public RectTransform mapRect;        // ตัวภาพแมพ (RectTransform ของ MapImage)
    public RectTransform playerIcon;     // ไอคอนผู้เล่น (ลูกศร)

    [Header("World Bounds")]
    public Transform worldMin;           // มุมล่างซ้ายของโลก
    public Transform worldMax;           // มุมขวาบนของโลก


    [Header("Player")]
    public Transform playerTransform;
    public RectTransform deliveryIconPrefab;   // Prefab icon จุดส่งของ

    // จุดส่งของในโลก กับ icon บนแมพ
    List<Transform> deliveryTargets = new List<Transform>();
    List<RectTransform> deliveryIcons = new List<RectTransform>();

    public GameObject ui;
    public bool IsColse;

    void Start()
    {
        
        if (!mapRect) mapRect = GetComponent<RectTransform>();
        if (playerIcon && playerIcon.parent != mapRect)
            playerIcon.SetParent(mapRect, false);

        // reset position ตอนเริ่ม
        playerIcon.anchoredPosition = Vector2.zero;
        IsColse = true;
        ui.SetActive(false);   // เริ่มต้นเป็นปิด minimap
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            IsColse = !IsColse;      // สลับ true/false
            ui.SetActive(!IsColse);  // ถ้า IsColse = true → ปิด, false → เปิด
        }


        if (!playerTransform || !playerIcon) return;

        // อัปเดตตำแหน่งผู้เล่น
        UpdateIconPosition(playerTransform.position, playerIcon);

        // หมุนหัวลูกศรตาม player (optional)
        float yaw = playerTransform.eulerAngles.y;
        playerIcon.localEulerAngles = new Vector3(0, 0, -yaw);

        // 🔹 อัปเดตตำแหน่ง icon ของจุดปลายทางทุกอัน
        for (int i = deliveryTargets.Count - 1; i >= 0; i--)
        {
            var t = deliveryTargets[i];
            var icon = deliveryIcons[i];

            // ถ้า worldTarget หรือ icon หายไป (เพราะเปลี่ยนซีน/ destroy) ก็เคลียร์ออกจากลิสต์
            if (t == null || icon == null)
            {
                if (icon != null)
                    Destroy(icon.gameObject);

                deliveryTargets.RemoveAt(i);
                deliveryIcons.RemoveAt(i);
                continue;
            }

            // แปลงตำแหน่งโลก -> minimap แล้วใส่ให้ icon
            UpdateIconPosition(t.position, icon);
        }


    }


    /// <summary>
    /// แปลงตำแหน่ง world (x,z) → local ใน mapRect แล้วเอาไปใส่ใน icon.anchoredPosition
    /// </summary>
    void UpdateIconPosition(Vector3 worldPos, RectTransform icon)
    {
        if (!mapRect || !icon || !worldMin || !worldMax) return;

        // 1) แปลง world → 0..1
        float nx = Mathf.InverseLerp(worldMin.position.x, worldMax.position.x, worldPos.x);
        float nz = Mathf.InverseLerp(worldMin.position.z, worldMax.position.z, worldPos.z);

        // กันค่าเกิน 0..1 (ไม่งั้น icon จะบินออกขอบ)
        nx = Mathf.Clamp01(nx);
        nz = Mathf.Clamp01(nz);

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
