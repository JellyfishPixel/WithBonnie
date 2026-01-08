using StarterAssets;
using UnityEngine;

public class PlayerMovementLocker : MonoBehaviour
{
    FirstPersonController fps;
    ThirdPersonController tps;

    bool isLocked;

    void Awake()
    {
        fps = GetComponent<FirstPersonController>();
        tps = GetComponent<ThirdPersonController>();

        if (!fps && !tps)
            Debug.LogWarning("[PlayerControlLocker] No controller found on Player");
    }

    public void Lock()
    {
        if (isLocked) return;
        isLocked = true;

        if (fps) fps.LockMovement();
        if (tps) tps.LockMovement();
    }

    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;

        if (fps) fps.UnlockMovement();
        if (tps) tps.UnlockMovement();
    }

    public bool IsLocked()
    {
        return isLocked;
    }
}
