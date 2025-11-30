using UnityEngine;

public class Bin : MonoBehaviour
{
    public void OnTriggerEnter(Collider item)
    {
        if (item.CompareTag("pickable"))
        {
            Destroy(item.gameObject);
        }
    }
}
