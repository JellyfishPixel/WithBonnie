using System.Collections.Generic;
using UnityEngine;

public class DirectionArrowUI : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public DirectionArrowItem arrowPrefab;
    public RectTransform container;

    Dictionary<Transform, DirectionArrowItem> arrows = new();

    // ===== API =====

    public void SetTarget(Transform target)
    {
        if (!target || arrows.ContainsKey(target)) return;

        var arrow = Instantiate(arrowPrefab, container);
        arrow.Init(player, target);

        arrows.Add(target, arrow);
    }

    public void RemoveTarget(Transform target)
    {
        if (!target) return;

        if (arrows.TryGetValue(target, out var arrow))
        {
            Destroy(arrow.gameObject);
            arrows.Remove(target);
        }
    }

    public void ClearAll()
    {
        foreach (var a in arrows.Values)
            Destroy(a.gameObject);

        arrows.Clear();
    }

    public bool HasAnyTarget() => arrows.Count > 0;
}
