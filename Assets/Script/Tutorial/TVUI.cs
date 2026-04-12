using UnityEngine;
using StarterAssets;
using FourHandsTwoCats.VideoPlayer;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.UI;
public class TVUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    PlayerInteractionSystem interactionSystem;

    [Header("Panels")]
    public GameObject infoPanel;
    public GameObject videoPanel;

    [Header("Info Pages")]
    public GameObject inputPage;
    public GameObject howToPlayPage;

    [Header("Video Manager")]
    public VideoPlayerManager videoManager;

    [Header("Video Clips")]
    public VideoClip shoppingClip;
    public VideoClip packingClip;
    public VideoClip sendingClip;

    [Header("Popup Animation")]
    public RectTransform tvRect;

    PlayerMovementLocker movementLocker;
    StarterAssetsInputs starterInputs;

    bool isOpen;
    [Header("Button Colors")]
    public Color normalColor = Color.white;
    public Color activeColor = new Color(0.7f, 0.7f, 0.7f);

    void Start()
    {
        root.SetActive(false);

        movementLocker = FindFirstObjectByType<PlayerMovementLocker>();
        starterInputs = FindFirstObjectByType<StarterAssetsInputs>();
    }

    void OnDisable()
    {
        UILockManager.Release(this);
        isOpen = false;
    }

    // ================= OPEN =================

    public void OpenTV()
    {
        if (isOpen) return;

        isOpen = true;

        root.SetActive(true);
  
        // POPUP animation
        LeanTween.cancel(tvRect.gameObject);

        tvRect.localScale = Vector3.one * 0.05f;

        LeanTween.scale(tvRect, Vector3.one, 0.25f)
        .setEaseOutBack();

        UILockManager.Instance.PushLock(this, UILockOptions.Dialogue);

        ChannelInput();

        AddSalesPopupUI.ShowMessage("TV opened");
        GuideArrowManager.Instance?.NextTarget();
    }

    // ================= CLOSE =================

    public void CloseTV()
    {
        if (!isOpen) return;

        isOpen = false;

        LeanTween.cancel(tvRect.gameObject);

        LeanTween.scale(tvRect, Vector3.one * 0.05f, 0.18f)
        .setEaseInBack()
        .setOnComplete(() =>
        {
            root.SetActive(false);

            UILockManager.Instance.PopLock(this);
        });

        AddSalesPopupUI.ShowMessage("TV closed");
    }

    // ================= CHANNELS =================
    public void FocusButton(Button clickedBtn)
    {
        // หา button ทั้งหมดใน parent เดียวกัน
        Button[] allButtons = clickedBtn.transform.parent.GetComponentsInChildren<Button>();

        foreach (Button btn in allButtons)
        {
            Image img = btn.GetComponent<Image>();

            if (btn == clickedBtn)
                img.color = activeColor;
            else
                img.color = normalColor;
        }
    }
    public void ChannelInput()
    {
        HideAll();

        infoPanel.SetActive(true);

        inputPage.SetActive(true);
        howToPlayPage.SetActive(false);

        AddSalesPopupUI.ShowMessage("Channel: Input");
    }

    public void ChannelHowToPlay()
    {
        HideAll();

        infoPanel.SetActive(true);

        inputPage.SetActive(false);
        howToPlayPage.SetActive(true);

        AddSalesPopupUI.ShowMessage("Channel: How To Play");
    }

    public void ChannelShopping()
    {
        HideAll();

        videoPanel.SetActive(true);

        PlayClip(shoppingClip);

        AddSalesPopupUI.ShowMessage("Channel: Shopping");
    }

    public void ChannelPacking()
    {
        HideAll();

        videoPanel.SetActive(true);

        PlayClip(packingClip);

        AddSalesPopupUI.ShowMessage("Channel: Packing");
    }

    public void ChannelSending()
    {
        HideAll();

        videoPanel.SetActive(true);

        PlayClip(sendingClip);

        AddSalesPopupUI.ShowMessage("Channel: Sending");
    }

    // ================= VIDEO =================

    void PlayClip(VideoClip clip)
    {
        if (!clip)
        {
            Debug.LogError("No clip assigned");
            return;
        }

        var vp = videoManager.GetComponent<UnityEngine.Video.VideoPlayer>();

        if (vp == null)
        {
            Debug.LogError("No VideoPlayer found!");
            return;
        }

        vp.clip = clip;
        vp.Play();
    }

    // ================= UTILITY =================

    void HideAll()
    {
        infoPanel.SetActive(false);
        videoPanel.SetActive(false);

        inputPage.SetActive(false);
        howToPlayPage.SetActive(false);
    }

    void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTV();
        }
    }
}
