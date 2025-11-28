using UnityEngine;


public enum ItemCategory
{
    Normal,     // ของทั่วไป
    Cold,       // ของที่ต้องเก็บความเย็น
    Fragile,    // ของแตกง่าย
    Liquid,     // ของเหลว
}


public enum BoxKind
{
    Small,
    Medium,
    Large,
    ColdBox
}

[CreateAssetMenu(
    fileName = "DeliveryItemData",
    menuName = "SendGame/Delivery Item Data")]
public class DeliveryItemData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐานของไอเท็ม")]
    [Tooltip("ชื่อไอเท็ม (ใช้โชว์บน UI / Debug)")]
    public string itemName;

    [Tooltip("รูปไอคอนของไอเท็ม")]
    public Sprite icon;

    [Tooltip("ประเภทของไอเท็ม เช่น Normal/Cold/Fragile")]
    public ItemCategory category = ItemCategory.Normal;

    [Header("การส่งของ / Deadline")]
    [Tooltip("ต้องส่งของชิ้นนี้ภายในกี่วัน (เช่น 3 วัน)")]
    public int deliveryLimitDays = 3;

    [Header("คุณภาพเริ่มต้น")]
    [Tooltip("คุณภาพเริ่มต้น (0-100) ตอนสร้างของขึ้นมาใหม่")]
    [Range(0, 100)]
    public float baseQuality = 100f;

    [Header("เงินพื้นฐานที่จะได้")]
    [Tooltip("เงินที่จะได้ถ้าส่งทันเวลาและของไม่เสียหาย")]
    public int baseReward = 100;

    [Header("ปลายทาง (หลายซีนใช้ ID)")]
    [Tooltip("ID ของปลายทางที่จะเอาไปแมปกับ Transform ในแต่ละซีนอีกที")]
    public string destinationId;

    // ------------------ CONFIG ดาเมจ / ชำรุด / พัง ------------------

    [Header("Threshold คุณภาพสำหรับสถานะต่าง ๆ")]
    [Tooltip("ถ้าคุณภาพต่ำกว่าค่านี้ ถือว่าเป็นของ \"เสียหาย\" แต่ยังไม่พัง (isDamaged = true)")]
    [Range(0, 100)]
    public float damagedThreshold = 80f;

    [Tooltip("ถ้าคุณภาพต่ำกว่าค่านี้ ถือว่า \"แตก/พัง\" ใช้งานไม่ได้ (isBroken = true)")]
    [Range(0, 100)]
    public float brokenThreshold = 20f;

    [Header("ค่าดาเมจจากการชน/ตกแรง (ใช้กับความเร็ว)")]
    [Tooltip("ความเร็วชน (m/s) ที่เริ่มจะโดนดาเมจ เช่น 3 = ชนเบา ๆ ไม่เป็นไร จนเกิน 3 ค่อยเริ่มเสียหาย")]
    public float safeImpactVelocity = 3f;

    [Tooltip("เมื่อชนเกิน safeImpactVelocity ไป 1 หน่วย จะโดนดาเมจเท่าไหร่")]
    public float damagePerVelocity = 5f;

    [Header("ค่าดาเมจจากความสูง (ใช้กับการตก)")]
    [Tooltip("ตกจากความสูง (เมตร) ตั้งแต่ค่านี้ขึ้นไป ถึงจะเริ่มคำนวณดาเมจ (ใช้เวลาอยู่ใน inventory หรือโดนตกทั้งกล่อง)")]
    public float minFallHeightForDamage = 2.0f;

    [Header("การเสียหายจากน้ำ / สภาพแวดล้อม")]
    [Tooltip("ของชิ้นนี้พังทันทีเมื่อโดนน้ำหรือไม่ (เช่น เอกสาร, อิเล็กทรอนิกส์ที่ไม่กันน้ำ)")]
    public bool breaksOnWater = true;

    [Header("กล่องที่อนุญาตให้ใส่")]
    [Tooltip("ของชิ้นนี้สามารถใส่กล่องประเภทไหนได้บ้าง (S/M/L/ColdBox)")]
    public BoxKind[] allowedBoxTypes;
}
