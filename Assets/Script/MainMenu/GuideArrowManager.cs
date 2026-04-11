using System.Collections.Generic;
using UnityEngine;

public class GuideArrowManager : MonoBehaviour
{
    public static GuideArrowManager Instance;

    [Header("Arrow")]
    public Transform arrow;

    [Header("Targets")]
    public List<Transform> targets = new List<Transform>();

    [Header("Settings")]
    public float heightOffset = 1.5f;
    public float floatAmount = 0.35f;
    public float floatSpeed = 1f;

    int currentIndex = 0;
    Vector3 basePos;

    static bool tutorialFinished; // ⭐ ไม่ใช้ PlayerPrefs แล้ว

    void Awake()
    {
        Instance = this;

        if (tutorialFinished)
            arrow.gameObject.SetActive(false);
    }

    void Start()
    {
        if (tutorialFinished) return;

        if (targets.Count > 0)
        {
            MoveArrowToCurrent();
        }
    }

    void StartFloat()
    {
        LeanTween.cancel(arrow.gameObject);

        LeanTween.moveY(
            arrow.gameObject,
            basePos.y + floatAmount,
            floatSpeed
        ).setLoopPingPong();
    }

    void MoveArrowToCurrent()
    {
        if (currentIndex >= targets.Count)
        {
            FinishTutorial();
            return;
        }

        Transform t = targets[currentIndex];

        basePos = t.position + Vector3.up * heightOffset;

        arrow.position = basePos;

        arrow.rotation = Quaternion.identity;
        arrow.Rotate(0f, 0f, 0f);

        StartFloat();
    }

    public void NextTarget()
    {
        if (tutorialFinished) return;

        currentIndex++;
        MoveArrowToCurrent();
    }

    void FinishTutorial()
    {
        tutorialFinished = true;

        arrow.gameObject.SetActive(false);
    }
}