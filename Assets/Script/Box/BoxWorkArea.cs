using UnityEngine;
using UnityEngine.UIElements;

public class BoxWorkArea : MonoBehaviour
{
    public static BoxWorkArea Instance { get; private set; }

    [SerializeField] public BoxCore CurrentBox { get; private set; }
    public Collider workAreaCollider;

    void Awake()
    {
        Instance = this;
    }

    public void SetCurrentBox(BoxCore box)
    {
        if (CurrentBox == box) return;

        CurrentBox = box;
        Debug.Log($"[WorkArea] CurrentBox set to {box?.name}");
    }

    public void ClearCurrentBox(BoxCore box)
    {
        if (CurrentBox != box) return;

        CurrentBox = null;
        Debug.Log("[WorkArea] CurrentBox cleared");
    }
    private void OnCollisionEnter(Collision collision)
    {
        var box = collision.gameObject.GetComponentInParent<BoxCore>();
        if (!box) return;
        BoxWorkArea.Instance.SetCurrentBox(box);
    }
    void Update()
    {
        if (!CurrentBox) return;

        if (!workAreaCollider.bounds.Contains(CurrentBox.transform.position))
        {
            CurrentBox = null;
        }
    }
}
