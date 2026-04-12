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

        if (loadGameButton)
            loadGameButton.gameObject.SetActive(false);
    }

    // ================= BUTTON EVENTS =================

    public void OnNewGame()
    {
        SceneManager.LoadScene(gameSceneName);
        Cursor.visible = false;
    }

    public void OnLoadGame()
    {
        Debug.Log("[MainMenuUI] Load game is disabled until the new save system is rebuilt.");
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    // ================= HELPERS =================

}
