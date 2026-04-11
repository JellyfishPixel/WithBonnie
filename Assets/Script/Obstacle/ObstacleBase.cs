using UnityEngine;

public abstract class ObstacleBase : MonoBehaviour, IObstacleDamage
{
    [Header("Obstacle Damage")]
    [Tooltip("ค่าดาเมจพื้นฐานที่ลดคุณภาพ")]
    public float baseQualityDamage = 5f;

    [Tooltip("ใช้ลดซ้ำได้หรือไม่")]
    public bool canHitMultipleTimes = false;

    protected virtual void Awake()
    {
        gameObject.tag = "Obstacle";
    }

    public virtual void ApplyTo(BoxCore box)
    {
        if (!box || !box.HasItem) return;

        box.ApplyQualityDamage(baseQualityDamage);
    }
    protected void ApplyDamageToCollision(Collision collision, float damage)
    {
        // ชน Player
        if (collision.collider.CompareTag("Player"))
        {
            BoxInventory.Instance?.ApplyObstacleDamage(damage);
            return;
        }

        // ชนกล่องในโลก
        var box = collision.collider.GetComponentInParent<BoxCore>();
        if (box != null)
        {
            box.ApplyQualityDamage(damage);
        }
    }

}
