using UnityEngine;

public class CharacterVisualStabilizer : MonoBehaviour
{
    public CharacterController controller;
    public Transform visual;

    bool wasGrounded;

    void LateUpdate()
    {
        if (!controller || !visual) return;

        // detect landing
        if (!wasGrounded && controller.isGrounded)
        {
            // 🔥 รีเซ็ต visual ตอนแตะพื้น
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
        }

        wasGrounded = controller.isGrounded;
    }
}
