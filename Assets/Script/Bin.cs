using UnityEngine;

public class Bin : MonoBehaviour
{
    public void ontriggerEnter(Collider item)
    {
        if (item.CompareTag("pickable"))
        {
            Destroy(item.gameObject);
        }
    }
}
