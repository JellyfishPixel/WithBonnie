using UnityEngine;
using StarterAssets;

public class BounceMushroom : MonoBehaviour
{
    public float bounceForce = 12f;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Hit: " + other.name);

        if (!other.CompareTag("Player"))
            return;

        var controller =
            other.GetComponentInParent<ThirdPersonController>();

        if (!controller)
        {
            Debug.Log("No controller");
            return;
        }

        Debug.Log("BOUNCE!");

        LeanTween.scaleY(gameObject, 0.7f, 0.1f)
            .setLoopPingPong(1);

        controller.AddJumpForce(bounceForce);
    }
}