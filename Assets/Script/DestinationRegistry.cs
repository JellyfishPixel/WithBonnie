using UnityEngine;

[System.Serializable]
public class DestinationPoint
{
    public string id;        // เช่น "Village", "Sea", "Mountain"
    public Transform point;  // จุดจริงในโลก
}

public class DestinationRegistry : MonoBehaviour
{
    [Header("Destination Points")]
    public DestinationPoint[] points;

    public Transform GetPoint(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var p in points)
        {
            if (p != null && p.id == id && p.point != null)
                return p.point;
        }
        return null;
    }
}
