using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button newGameButton;
    public Button loadGameButton;

    [Header("Scene Names")]
    public string gameSceneName = "Main";

    void Start()
    {
        // เปิดเมาส์ในหน้าเมนู
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;

        // 🔹 แสดงปุ่ม Load เฉพาะเมื่อมีเซฟ
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            loadGameButton.gameObject.SetActive(true);
        }
        else
        {
            loadGameButton.gameObject.SetActive(false);
        }
    }

    // ================= BUTTON EVENTS =================

    public void OnNewGame()
    {
        SceneManager.LoadScene(gameSceneName);
        Cursor.visible = false;
    }

    public void OnLoadGame()
    {
        SceneManager.LoadScene(gameSceneName);
        StartCoroutine(LoadAfterScene());
        Cursor.visible = false;

    }

    public void OnQuit()
    {
        Application.Quit();
    }

    // ================= HELPERS =================

    System.Collections.IEnumerator LoadAfterScene()
    {
        yield return null; // รอให้ scene โหลดก่อน
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }
    }
}
