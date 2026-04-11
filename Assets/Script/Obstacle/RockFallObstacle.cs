using UnityEngine;

public class RockFallObstacle : ObstacleBase
{
    public enum RockMode
    {
        FallDown,      // ตกตรงๆ
        EnablePhysics  // เปิด Rigidbody แล้วกลิ้งเอง
    }

    [Header("Rock Settings")]
    public RockMode mode = RockMode.FallDown;

    public Rigidbody rockRb;

    [Header("Fall Mode")]
    public float fallForce = 10f;

    [Header("Damage")]
    public float impactDamageMultiplier = 2f;

    bool triggered = false;

    void Reset()
    {
        rockRb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (!rockRb) return;

        rockRb.isKinematic = false;
        rockRb.useGravity = true;


        if (mode == RockMode.FallDown)
        {
            rockRb.AddForce(Vector3.down * fallForce, ForceMode.Impulse);
        }

    }

    void OnCollisionEnter(Collision collision)
    {
        if (!triggered) return;

        float dmg = baseQualityDamage * impactDamageMultiplier;
        ApplyDamageToCollision(collision, dmg);

        triggered = false;
    }
}