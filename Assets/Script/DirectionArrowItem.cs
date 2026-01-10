using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DirectionArrowItem : MonoBehaviour
{
    [Header("UI")]
    public RectTransform arrowIcon;
    public Image arrowImage;
    public TMP_Text distanceText;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color nearColor = Color.red;
    public float nearDistance = 10f; // เมตร

    Transform player;
    Transform target;

    public void Init(Transform playerTransform, Transform targetTransform)
    {
        player = playerTransform;
        target = targetTransform;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!player || !target)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateDirection();
        UpdateDistance();
    }

    void UpdateDirection()
    {
        Vector3 toTarget = target.position - player.position;
        toTarget.y = 0f;

        Vector3 forward = player.forward;
        forward.y = 0f;

        float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

        // หมุน UI (Z axis)
        arrowIcon.localEulerAngles = new Vector3(0, 0, angle);
        Debug.Log($"Angle = {angle}");

    }

    void UpdateDistance()
    {
        float dist = Vector3.Distance(player.position, target.position);

        distanceText.text = $"{Mathf.RoundToInt(dist)} m";

        arrowImage.color = dist <= nearDistance ? nearColor : normalColor;
    }

    public Transform GetTarget() => target;
}
