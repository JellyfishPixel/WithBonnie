using UnityEngine;

public class UIInputBlocker : MonoBehaviour
{
    [SerializeField] bool blockWorldInput = false;
    UIPopup popup;
    bool appliedOwnLock;

    void Awake()
    {
        popup = GetComponent<UIPopup>();
    }

    void OnEnable()
    {
        if (!blockWorldInput)
            return;

        if (popup != null)
            return;

        UILockManager.Instance.PushLock(this, UILockOptions.Popup);
        appliedOwnLock = true;
    }

    void OnDisable()
    {
        if (!appliedOwnLock)
            return;

        UILockManager.Release(this);
        appliedOwnLock = false;
    }
}
