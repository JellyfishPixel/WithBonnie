using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("Map UI")]
    public RectTransform mapRect;        // ตัวภาพแมพ (RectTransform ของ MapImage)
    public RectTransform playerIcon;     // ไอคอนผู้เล่น (ลูกศร)

    [Header("World Bounds")]
    public Transform worldMin;           // มุมล่างซ้ายของโลก
    public Transform worldMax;           // มุมขวาบนของโลก

    [Header("Refs")]
    public Transform player;             // ตัว Player ในโลก
    public RectTransform deliveryIconPrefab;   // Prefab icon จุดส่งของ

    // จุดส่งของในโลก กับ icon บนแมพ
    List<Transform> deliveryTargets = new List<Transform>();
    List<RectTransform> deliveryIcons = new List<RectTransform>();

    void Start()
    {
        if (!mapRect) mapRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!player || !worldMin || !worldMax || !mapRect) return;

        // === อัปเดตไอคอนผู้เล่น ===
        UpdateIconPosition(player.position, playerIcon);

        // หมุนลูกศรให้หันตามมุมของ player (yaw)
        if (playerIcon)
        {
            float yaw = player.eulerAngles.y;
            playerIcon.localEulerAngles = new Vector3(0, 0, -yaw);
        }

        // === อัปเดตทุกจุดส่งของ ===
        for (int i = 0; i < deliveryTargets.Count; i++)
        {
            var t = deliveryTargets[i];
            var icon = deliveryIcons[i];

            if (!t || !icon) continue;

            UpdateIconPosition(t.position, icon);
        }
    }

    /// <summary>
    /// แปลงตำแหน่ง world (x,z) → local ใน mapRect แล้วเอาไปใส่ใน icon.anchoredPosition
    /// </summary>
    void UpdateIconPosition(Vector3 worldPos, RectTransform icon)
    {
        if (!icon) return;

        // 1) ทำเป็น normalized (0..1) ในกรอบโลก
        float nx = Mathf.InverseLerp(worldMin.position.x, worldMax.position.x, worldPos.x);
        float nz = Mathf.InverseLerp(worldMin.position.z, worldMax.position.z, worldPos.z);

        Vector2 normalized = new Vector2(nx, nz);

        // 2) แปลง 0..1 → local (center = 0,0)
        Vector2 mapSize = mapRect.rect.size;
        Vector2 local = (normalized - new Vector2(0.5f, 0.5f)) * mapSize;

        icon.anchoredPosition = local;
    }

    /// <summary>
    /// เรียกจาก GameManager เวลา spawn จุดส่งของใหม่
    /// </summary>
    public void RegisterDeliveryTarget(Transform targetWorldTransform)
    {
        if (!targetWorldTransform || !deliveryIconPrefab || !mapRect) return;

        // สร้าง icon ใต้ mapRect
        RectTransform icon = Instantiate(deliveryIconPrefab, mapRect);
        icon.anchoredPosition = Vector2.zero;

        deliveryTargets.Add(targetWorldTransform);
        deliveryIcons.Add(icon);
    }

    /// <summary>
    /// เรียกตอนส่งของสำเร็จ/ยกเลิก เพื่อเอา icon ออกจากแมพ
    /// </summary>
    public void UnregisterDeliveryTarget(Transform targetWorldTransform)
    {
        int index = deliveryTargets.IndexOf(targetWorldTransform);
        if (index < 0) return;

        if (deliveryIcons[index])
            Destroy(deliveryIcons[index].gameObject);

        deliveryTargets.RemoveAt(index);
        deliveryIcons.RemoveAt(index);
    }
}
