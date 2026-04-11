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
    Transform cameraTransform;
    Transform player;
    Transform target;

    public void Init(Transform playerTransform, Transform targetTransform, Transform cam)
    {
        player = playerTransform;
        target = targetTransform;
        cameraTransform = cam;

        gameObject.SetActive(true);
        distanceText.gameObject.SetActive(true);
    }
    public void OverrideTarget(Transform newTarget)
    {
        target = newTarget;
    }
    void Update()
    {
        if (!player || !target)
        {
            Hide();
            return;
        }

        Show();

        UpdateDirection();
        UpdateDistance();
    }

    void Hide()
    {
        if (arrowIcon) arrowIcon.gameObject.SetActive(false);
        if (distanceText) distanceText.gameObject.SetActive(false);
    }

    void Show()
    {
        if (arrowIcon) arrowIcon.gameObject.SetActive(true);
        if (distanceText) distanceText.gameObject.SetActive(true);
    }

    void UpdateDirection()
    {
        if (!cameraTransform || !player || !target)
            return;

        Vector3 toTarget = target.position - player.position;
        toTarget.y = 0f;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;

        float angle = Vector3.SignedAngle(camForward, toTarget, Vector3.up);

        arrowIcon.localEulerAngles = new Vector3(0, 0, -angle);
    }
    void UpdateDistance()
    {
        if (!player || !target) return;

        float dist = Vector3.Distance(player.position, target.position);
        distanceText.text = $"{Mathf.RoundToInt(dist)} m";
        arrowImage.color = dist <= nearDistance ? nearColor : normalColor;
    }


    public Transform GetTarget() => target;
}
