using UnityEngine;

public class GuideTarget : MonoBehaviour
{
    public void CompleteStep()
    {
        if (GuideArrowManager.Instance != null)
        {
            GuideArrowManager.Instance.NextTarget();
        }
    }
}