using UnityEngine;

public class SkyManager : MonoBehaviour
{
    public float skySpeed;  // ✅ ย้ายมาไว้ระดับ class

    void Start()
    {
        // ไม่มีอะไรต้องทำใน Start ตอนนี้
    }

    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * skySpeed);
    }
}