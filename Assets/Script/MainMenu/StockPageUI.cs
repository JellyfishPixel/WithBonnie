using UnityEngine;

public class StockPageUI : MonoBehaviour
{
    [Header("Sub Pages")]
    public GameObject boxPage;
    public GameObject tapePage;
    public GameObject bubblePage;

    GameObject currentSubPage;

    void OnEnable()
    {

        ShowBox();
    }

    void Show(GameObject target)
    {
        if (boxPage) boxPage.SetActive(false);
        if (tapePage) tapePage.SetActive(false);
        if (bubblePage) bubblePage.SetActive(false);

        if (target)
        {
            target.SetActive(true);
            currentSubPage = target;
        }
    }

    public void ShowBox()
    {
        Show(boxPage);
    }

    public void ShowTape()
    {
        Show(tapePage);
    }

    public void ShowBubble()
    {
        Show(bubblePage);
    }
}
