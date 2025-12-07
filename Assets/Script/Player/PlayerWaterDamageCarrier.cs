using UnityEngine;

/// <summary>
/// เช็คว่าผู้เล่นกำลังอยู่ในโซนน้ำ (Collider ที่ tag = "Water")
/// ถ้าใช่ จะไปบอก BoxInventory ให้ลดคุณภาพ item ที่ waterSensitive
/// 1 หน่วยต่อ 1 วินาที
/// </summary>
public class PlayerWaterDamageCarrier : MonoBehaviour
{
    [Header("Water Check")]
    [Tooltip("Tag ของ Collider ที่ใช้แทนน้ำในฉาก")]
    public string waterTag = "Water";

    [Header("Options")]
    [Tooltip("เปิด/ปิดระบบดาเมจน้ำชั่วคราวได้")]
    public bool waterDamageEnabled = true;

    bool inWater = false;

    void OnTriggerEnter(Collider other)
    {
        if (!waterDamageEnabled) return;
        if (other.CompareTag(waterTag))
        {
            inWater = true;
            Debug.Log("[PlayerWater] Enter water");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!waterDamageEnabled) return;
        if (other.CompareTag(waterTag))
        {
            inWater = false;
            Debug.Log("[PlayerWater] Exit water");
        }
    }

    void Update()
    {
        if (!waterDamageEnabled) return;
        if (!inWater) return;

        if (BoxInventory.Instance == null) return;

        // ลดคุณภาพ item ที่ waterSensitive ตามเวลาเฟรมนี้
        BoxInventory.Instance.ApplyWaterDamageToSensitive(Time.deltaTime);
    }
}
