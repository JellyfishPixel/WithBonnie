using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GlobalUIFeedbackManager : MonoBehaviour
{
    public static GlobalUIFeedbackManager Instance;

    [Header("Sound")]
    [SerializeField] private string clickSoundID = "ui_click";

    [Header("Cursor")]
    public Texture2D normalCursor;
    public Texture2D hoverCursor;
    public Vector2 hotspot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    private HashSet<Button> registeredButtons = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        Cursor.SetCursor(normalCursor, hotspot, cursorMode);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterAllButtons();
    }

    void RegisterAllButtons()
    {
        registeredButtons.Clear();

        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);

        foreach (Button btn in buttons)
        {
            RegisterButton(btn);
        }
    }

    void RegisterButton(Button btn)
    {
        if (registeredButtons.Contains(btn)) return;

        // Click sound
        btn.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayUIById(clickSoundID);
        });

        // Hover cursor
        EventTrigger trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btn.gameObject.AddComponent<EventTrigger>();

        // Pointer Enter
        EventTrigger.Entry enter = new();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) =>
        {
            Cursor.SetCursor(hoverCursor, hotspot, cursorMode);
        });
        trigger.triggers.Add(enter);

        // Pointer Exit
        EventTrigger.Entry exit = new();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) =>
        {
            Cursor.SetCursor(normalCursor, hotspot, cursorMode);
        });
        trigger.triggers.Add(exit);

        registeredButtons.Add(btn);
    }
}