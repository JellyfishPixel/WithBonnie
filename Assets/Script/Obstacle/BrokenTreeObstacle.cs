using UnityEngine;

public class BrokenTreeFallLean : ObstacleBase
{
    [Header("LeanTween Fall")]
    public Transform tree;
    public float fallZAngle = -90f;
    public float fallDuration = 0.6f;
    public LeanTweenType ease = LeanTweenType.easeOutQuad;

    bool triggered;
    bool damaged;

    protected override void Awake()
    {
        base.Awake();
        if (!tree)
            tree = transform;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // หมุนเพิ่มจากมุมปัจจุบัน
        Vector3 targetEuler = tree.localEulerAngles;
        targetEuler.z += fallZAngle;

        LeanTween.rotateLocal(tree.gameObject, targetEuler, fallDuration)
                 .setEase(ease);

        Debug.Log("[TreeFall] LeanTween rotate");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!triggered || damaged) return;

        if (!collision.collider.CompareTag("Player") )
            return;

        damaged = true;

        float dmg = baseQualityDamage;

        var box = collision.collider.GetComponentInChildren<BoxCore>();
        if (box != null)
            box.ApplyQualityDamage(dmg);
        else if (BoxInventory.Instance != null)
            BoxInventory.Instance.ApplyObstacleDamage(dmg);

        Debug.Log("[TreeFall] Damage applied");
    }
}
