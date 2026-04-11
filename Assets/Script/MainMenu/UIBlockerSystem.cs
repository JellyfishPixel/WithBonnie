using UnityEngine;

public class UIInputBlocker : MonoBehaviour
{
    void OnEnable()
    {
        PlayerInteractionSystem.BlockWorldInput = true;
    }

    void OnDisable()
    {
        PlayerInteractionSystem.BlockWorldInput = false;
    }
}