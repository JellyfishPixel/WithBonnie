using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DirectionArrowUI : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public DirectionArrowItem arrowPrefab;
    public RectTransform container;
    public Transform warpTarget;
    public Transform cam;
    Dictionary<Transform, DirectionArrowItem> arrows = new();

    public void SetTarget(Transform target)
    {
        if (!target) return;
       
        if (!arrows.ContainsKey(target))
        {
            var arrow = Instantiate(arrowPrefab, container);
            arrow.Init(player, target , cam);
            arrows.Add(target, arrow);
        }

        UpdateNearestTarget();
    }

    public void RemoveTarget(Transform target)
    {
        if (!target) return;

        if (arrows.TryGetValue(target, out var arrow))
        {
            arrows.Remove(target);
            Destroy(arrow.gameObject);
        }

        UpdateNearestTarget(); // 🔥 บังคับรีคำนวณใหม่
    }

    public void ClearAll()
    {
        foreach (var a in arrows.Values)
            if (a) Destroy(a.gameObject);

        arrows.Clear();
    }

    void Update()
    {
        UpdateNearestTarget();
    }
    public void Rebuild()
    {
        UpdateNearestTarget();
    }
    void UpdateNearestTarget()
    {
        if (arrows.Count == 0)
            return;

        var registry = FindFirstObjectByType<DestinationRegistry>();
        string currentScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        var bestSlot =
            BoxInventory.Instance.GetNearestSlotInSceneFirst(currentScene, registry);

        Transform bestTarget = null;

        if (bestSlot != null)
        {
            foreach (var rec in GameManager.Instance.activeBoxes)
            {
                if (rec.destinationId == bestSlot.itemData.destinationId)
                {
                    bestTarget = rec.worldTarget;
                    break;
                }
            }
        }

        foreach (var kv in arrows)
        {
            bool show = kv.Key == bestTarget;
            if (kv.Value)
                kv.Value.gameObject.SetActive(show);
        }
    }
   
    public bool HasAnyTarget() => arrows.Count > 0;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAll();

        Invoke(nameof(RebuildFromGameManager), 0.1f);
    }

    void RebuildFromGameManager()
    {
        if (GameManager.Instance == null) return;

        foreach (var rec in GameManager.Instance.activeBoxes)
        {
            if (rec.worldTarget != null)
            {
                SetTarget(rec.worldTarget);
            }
        }
    }
}
