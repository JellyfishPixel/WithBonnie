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

    [Header("FALL DAMAGE (simple)")]
    [Tooltip("เริ่มเสียหายเมื่อความสูง >= ค่านี้ (เมตร)")]
    public int minFallHeightMeter = 1;

    [Tooltip("ดาเมจต่อ 1 เมตร (เช่น 5 = 5 ดาเมจ/เมตร)")]
    public int damagePerMeter = 5;

    [Header("COLD STORAGE")]
    [Tooltip("ของต้องใส่กล่องเย็นหรือไม่")]
    public bool requiresCold = false;

    [Header("QUALITY THRESHOLDS")]
    [Tooltip("ต่ำกว่าค่านี้ถือว่า 'เสียหาย' ")]
    [Range(0, 100)] public float damagedThreshold = 70f;

    [Tooltip("ต่ำกว่าค่านี้ถือว่า 'พัง / ใช้ไม่ได้' ")]
    [Range(0, 100)] public float brokenThreshold = 20f;


    [Header("การเสียหายจากน้ำ / สภาพแวดล้อม")]
    [Tooltip("ของชิ้นนี้พังทันทีเมื่อโดนน้ำหรือไม่ (เช่น เอกสาร, อิเล็กทรอนิกส์ที่ไม่กันน้ำ)")]
    public bool breaksOnWater = true;

    [Header("กล่องที่อนุญาตให้ใส่")]
    [Tooltip("ของชิ้นนี้สามารถใส่กล่องประเภทไหนได้บ้าง (S/M/L/ColdBox)")]
    public BoxKind[] allowedBoxTypes;

    [Header("Dialogue")]
    [Tooltip("บทสนทนากับลูกค้าเวลามารับ/ส่งของ")]
    public ItemDialogueData dialogueData;
}
