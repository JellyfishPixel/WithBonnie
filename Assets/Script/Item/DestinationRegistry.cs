using System;
using UnityEngine;

[System.Serializable]
public class DestinationPoint
{
    public string id;        // เช่น "Home1"
    public Transform point;  // จุดจริงในโลก
}

public class DestinationRegistry : MonoBehaviour
{
    [Header("Destination Points")]
    public DestinationPoint[] points;

    public Transform GetPoint(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        // ตัด space หน้า–หลัง + ไม่สนตัวพิมพ์เล็กใหญ่
        string key = id.Trim();

        foreach (var p in points)
        {
            if (p == null || p.point == null) continue;

            var pid = (p.id ?? string.Empty).Trim();

            if (string.Equals(pid, key, StringComparison.OrdinalIgnoreCase))
            {
                return p.point;
            }
        }

        Debug.LogWarning($"[DestinationRegistry] not found id='{id}' (after trim='{key}')");
        return null;
    }
    public Transform GetPointById(string id)
    {
        foreach (var p in points)
        {
            if (DeliveryDestinationId.Matches(p.id, id))
                return p.point;
        }
        return null;
    }
    public string GetSceneById(string id)
    {
        var point = GetPointById(id);
        if (point == null) return null;

        return point.gameObject.scene.name;
    }
}
