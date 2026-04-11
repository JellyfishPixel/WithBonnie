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
        movementLocker?.Lock();

        // ⏸ หยุดเวลา
        Time.timeScale = 0f;

        // 🖱 เปิดเมาส์
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 🔒 ล็อกกล้อง (Starter Assets)
        if (starterInputs != null)
        {
            starterInputs.look = Vector2.zero;
            starterInputs.cursorLocked = false;
        }

    }


    public void CloseMenu()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panel) panel.SetActive(false);

        movementLocker?.Unlock();
        var interact = FindFirstObjectByType<PlayerInteractionSystem>();
        if (interact != null)
            interact.UnlockMovement();
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (starterInputs != null)
        {
            starterInputs.cursorLocked = true;

            // ⭐ Reset Input (สำคัญมาก)
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }
        PlayerInteractionSystem.BlockWorldInput = false;
    }

    // ================= BUTTON EVENTS =================

    public void OnResume()
    {
        CloseMenu();
    }

    public void OnBackToMainMenu()
    {
        // ป้องกัน time ค้าง
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuScene);
    }
    public void OnSaveGame()
    {
        if (SaveManager.Instance == null)
            return;

        SaveManager.Instance.SaveGame();

        // 🔹 เปลี่ยนข้อความปุ่ม
        if (saveButtonText)
            saveButtonText.text = savedLabel;

        Debug.Log("[InGameMenuUI] ▶ Game Save");
    }


}
