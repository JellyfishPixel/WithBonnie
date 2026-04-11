using UnityEngine;

public class SmoothLidClose : MonoBehaviour
{
    public float closedAngle = -180f; // ปรับตามที่ต้องการ
    public float openAngle = 0f;

    public float smoothSpeed = 3.5f;

    // เก็บแกนตั้งต้น
    private float initialX;
    private float initialY;

    private float targetAngle;
    private float currentAngle;
    public bool isClosed = false;

    private void Start()
    {
        // ดึงค่าต้นฉบับจาก Transform
        Vector3 startAngles = transform.localEulerAngles;
        initialX = startAngles.x;
        initialY = startAngles.y;

        currentAngle = openAngle;
        targetAngle = openAngle;
        isClosed = false;

        // หมุนด้วยแกน Z
        transform.localRotation = Quaternion.Euler(initialX, initialY, currentAngle);
    }

    private void Update()
    {
        if (Mathf.Abs(currentAngle - targetAngle) > 0.01f)
        {
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
            transform.localRotation = Quaternion.Euler(initialX, initialY, currentAngle);

            if (Mathf.Abs(currentAngle - closedAngle) < 0.5f)
            {
                currentAngle = closedAngle;
                transform.localRotation = Quaternion.Euler(initialX, initialY, currentAngle);
                isClosed = true;
            }
        }
    }

    public void CloseLid()
    {
        targetAngle = closedAngle;
        isClosed = false;
    }

    // (เสริม) ถ้าอยากเปิดกลับ
    public void OpenLid()
    {
        targetAngle = openAngle;
        isClosed = false;
    }
}
