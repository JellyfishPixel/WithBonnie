using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuUI : MonoBehaviour
{
    [Header("UI Root")]
    public GameObject panel;
    StarterAssetsInputs starterInputs;
    PlayerMovementLocker movementLocker;
    bool isOpen;

    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";

    public TMP_Text saveButtonText; 
    public string saveLabel = "SAVE";
    public string savedLabel = "SAVED";
    

    void Start()
    {
        if (panel) panel.SetActive(false);

        movementLocker = FindFirstObjectByType<PlayerMovementLocker>();
        starterInputs = FindFirstObjectByType<StarterAssetsInputs>();
    }

    void OnDisable()
    {
        UILockManager.Release(this);
        isOpen = false;
    }


    void Update()
    {
        // ปุ่มเปิด/ปิดเมนู (Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
                CloseMenu();
            else
                OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (isOpen) return;
        isOpen = true;

        if (panel) panel.SetActive(true);

        // 🔹 รีเซ็ตข้อความปุ่ม Save ทุกครั้ง
        if (saveButtonText)
            saveButtonText.text = saveLabel;

        // 🔒 ล็อกผู้เล่น
        UILockManager.Instance.PushLock(this, UILockOptions.Menu);

    }


    public void CloseMenu()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panel) panel.SetActive(false);

        UILockManager.Instance.PopLock(this);
    }

    // ================= BUTTON EVENTS =================

    public void OnResume()
    {
        CloseMenu();
    }

    public void OnBackToMainMenu()
    {
        UILockManager.Instance.PopLock(this);

        SceneManager.LoadScene(mainMenuScene);
    }
    public void OnSaveGame()
    {
        if (saveButtonText)
            saveButtonText.text = "SAVE OFF";

        Debug.Log("[InGameMenuUI] Full game save is disabled until the new save system is rebuilt.");
    }


}
