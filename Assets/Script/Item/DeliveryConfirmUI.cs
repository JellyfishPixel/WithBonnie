using UnityEngine;
using TMPro;
using UnityEngine.UI;
using StarterAssets;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeliveryConfirmUI : MonoBehaviour
{
    public static DeliveryConfirmUI Instance;

    public CanvasGroup cg;
    public RectTransform rect;

    public TextMeshProUGUI messageText;
    public Button confirmButton;


    private DeliveryPoint currentPoint;
    PlayerMovementLocker movementLocker;
    PlayerInteractionSystem interactionSystem;
    StarterAssetsInputs starterInputs;
    public TextMeshProUGUI confirmButtonText;
    public bool IsVisible => cg != null && cg.alpha > 0.001f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject); // 🔥 สำคัญ

        HideImmediate();
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UILockManager.Release(this);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RefreshReferences());
    }
    IEnumerator RefreshReferences()
    {
        yield return null; // รอ object spawn

        movementLocker = FindFirstObjectByType<PlayerMovementLocker>();
        interactionSystem = FindFirstObjectByType<PlayerInteractionSystem>();
        starterInputs = FindFirstObjectByType<StarterAssetsInputs>();
    }
    void HideImmediate()
    {
        cg.alpha = 0;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        rect.localScale = Vector3.one * 0.8f;
        currentPoint = null;
    }

    public void Show(DeliveryPoint point, bool hasItem)
    {
        if (cg.alpha > 0) return;

        currentPoint = point;

        cg.blocksRaycasts = true;
        cg.interactable = true;

        UILockManager.Instance.PushLock(this, UILockOptions.Dialogue);

        LeanTween.cancel(gameObject);

        rect.localScale = Vector3.one * 0.8f;

        if (!hasItem)
        {
            messageText.text = "You don't have any items.";
            confirmButtonText.text = "OK";
        }
        else
        {
            // ✅ มีของ
            messageText.text = "Deliver items here?";

            confirmButtonText.text = "YES";
        }

        LeanTween.scale(rect, Vector3.one, 0.25f).setEaseOutBack();
        LeanTween.alphaCanvas(cg, 1f, 0.2f);
    }
    public void Hide()
    {
        LeanTween.alphaCanvas(cg, 0f, 0.2f)
            .setOnComplete(() =>
            {
                HideImmediate();

                UILockManager.Instance.PopLock(this);
            });
    }

    public void ForceHide()
    {
        LeanTween.cancel(gameObject);
        HideImmediate();
        UILockManager.Release(this);
    }

    public void OnConfirm()
    {
        if (currentPoint != null)
        {

            if (currentPoint.HasItemToDeliver())
            {
                currentPoint.ConfirmDelivery();
            }

        }

        Hide();
    }



}
