using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using System.Collections.Generic;

public class ItemDialogueManager : MonoBehaviour
{
    public static ItemDialogueManager Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    public Button[] optionButtons;
    public TMP_Text[] optionLabels;

    [Header("Typing")]
    public bool enableTyping = true;
    public float charsPerSecond = 40f;
    public float punctuationPause = 0.08f;
    public bool supportRichText = true;
    public AudioClip perCharSfx;
    public int sfxEveryNChars = 2;

    [Header("Player Control")]
    PlayerMovementLocker movementLocker;

    private ItemDialogueData flow;
    private int stepIndex;
    private bool isShowing;
    private bool isTyping;
    private Coroutine typeCo;
    private Action<int> onChoice;
    private Action onFinished;

    // คุยได้แค่ครั้งเดียวต่อตัว
    private readonly HashSet<int> talkedActorIds = new();
    private int currentActorId = 0;

    public bool IsShowing => isShowing;

    int currentChoiceIndex = 0;
    ItemDialogueData.ChoiceOption[] currentChoices;


    // ================== UNITY ==================

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (panel) panel.SetActive(false);
    }

    void Start()
    {
        movementLocker = FindFirstObjectByType<PlayerMovementLocker>();

    }

    void Update()
    {
        if (!isShowing) return;

        // ===== เลือก choice =====
        if (currentChoices != null && currentChoices.Length >= 2)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentChoiceIndex =
                    (currentChoiceIndex - 1 + currentChoices.Length) % currentChoices.Length;
                UpdateChoiceHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentChoiceIndex =
                    (currentChoiceIndex + 1) % currentChoices.Length;
                UpdateChoiceHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                // ยืนยัน choice
                var chosen = currentChoices[currentChoiceIndex];
                onChoice?.Invoke(currentChoiceIndex);
                currentChoices = null;
                GoTo(chosen.gotoIndex);
            }

            return; // ❗ อย่าให้ Space ไป skip line
        }

        // ===== ไม่มี choice =====
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SkipTypingOrNext();
        }
    }


    // ================== PUBLIC API ==================

    public void Show(GameObject actorOwner, ItemDialogueData flow,
                     Action<int> onChoice = null,
                     Action onFinished = null)
    {
        if (!flow || flow.steps == null || flow.steps.Length == 0)
            return;

        currentActorId = actorOwner ? actorOwner.GetInstanceID() : 0;

        // ❌ คุยได้แค่รอบเดียว
        if (currentActorId != 0 && talkedActorIds.Contains(currentActorId))
            return;

        this.flow = flow;
        this.onChoice = onChoice;
        this.onFinished = onFinished;
        stepIndex = 0;

        if (panel) panel.SetActive(true);

        if (movementLocker != null)
            movementLocker.Lock();



        isShowing = true;
        ShowCurrentStep();
    }

    // ================== STEP ==================

    void ShowCurrentStep()
    {
        if (flow == null || stepIndex < 0 || stepIndex >= flow.steps.Length)
        {
            Close();
            return;
        }

        var step = flow.steps[stepIndex];
        HideAllChoices();
        currentChoices = null;
        currentChoiceIndex = 0;

        if (speakerText)
            speakerText.text = step.speaker ?? "";

        if (enableTyping)
        {
            if (typeCo != null) StopCoroutine(typeCo);
            typeCo = StartCoroutine(TypeLine(step.text, step.voice));
        }
        else
        {
            if (bodyText) bodyText.text = step.text ?? "";
            isTyping = false;
            ShowChoicesIfAny(step);
        }
    }
    void HideAllChoices()
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (!optionButtons[i]) continue;

            optionButtons[i].gameObject.SetActive(false);
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].interactable = true;
        }
    }

    void ShowChoicesIfAny(ItemDialogueData.Step step)
    {
        if (step.options == null || step.options.Length < 2)
            return;

        currentChoices = step.options;
        currentChoiceIndex = 0;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool enable = i < currentChoices.Length;
            if (!optionButtons[i]) continue;

            optionButtons[i].gameObject.SetActive(enable);

            if (enable)
            {
                if (i < optionLabels.Length && optionLabels[i])
                    optionLabels[i].text = currentChoices[i].text ?? "";
            }
        }

        UpdateChoiceHighlight();
    }
    void UpdateChoiceHighlight()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (!optionButtons[i]) continue;

            bool selected = (i == currentChoiceIndex);
            optionButtons[i].interactable = selected;

            // ถ้าอยากให้ชัดขึ้น แนะนำเปลี่ยนสี
            var colors = optionButtons[i].colors;
            colors.normalColor = selected ? Color.yellow : Color.white;
            optionButtons[i].colors = colors;
        }
    }


    // ================== TYPING ==================

    IEnumerator TypeLine(string text, AudioClip voice)
    {
        isTyping = true;
        if (bodyText) bodyText.text = "";

        if (voice && Camera.main)
            AudioSource.PlayClipAtPoint(voice, Camera.main.transform.position);

        text ??= "";
        float secPerChar = charsPerSecond > 0 ? 1f / charsPerSecond : 0f;

        int i = 0;
        while (i < text.Length && isTyping)
        {
            if (supportRichText && text[i] == '<')
            {
                int close = text.IndexOf('>', i);
                if (close < 0) close = i;
                Append(text.Substring(i, close - i + 1));
                i = close + 1;
            }
            else
            {
                Append(text[i].ToString());
                i++;

                if (perCharSfx && i % sfxEveryNChars == 0 && Camera.main)
                    AudioSource.PlayClipAtPoint(perCharSfx, Camera.main.transform.position, 0.6f);

                if (secPerChar > 0f)
                    yield return new WaitForSeconds(secPerChar);

                if (punctuationPause > 0f && IsPunc(text[i - 1]))
                    yield return new WaitForSeconds(punctuationPause);
            }
        }

        if (bodyText) bodyText.text = text;
        isTyping = false;

        ShowChoicesIfAny(flow.steps[stepIndex]);
    }

    void Append(string s)
    {
        if (bodyText) bodyText.text += s;
    }

    bool IsPunc(char c)
    {
        return ".!?;,…。".Contains(c);
    }

    // ================== INPUT ==================

    public void SkipTypingOrNext()
    {
        if (!isShowing) return;

        // ===== ครั้งแรก: เร่ง typing =====
        if (isTyping)
        {
            isTyping = false;

            if (typeCo != null)
            {
                StopCoroutine(typeCo);
                typeCo = null;
            }

            // ⭐ สำคัญ: แสดงข้อความเต็มทันที
            if (bodyText && flow != null)
                bodyText.text = flow.steps[stepIndex].text ?? "";

            return; // ❗ ห้ามไป GoTo
        }

        // ===== ครั้งที่สอง: ไปบรรทัดถัดไป =====
        var step = flow.steps[stepIndex];
        if (step.options == null || step.options.Length < 2)
        {
            GoTo(step.gotoIndex);
        }
    }


    void GoTo(int gotoIndex)
    {
        if (gotoIndex < 0)
        {
            Close();
            return;
        }

        stepIndex = gotoIndex;
        ShowCurrentStep();
    }

    // ================== CLOSE ==================

    public void Close()
    {
        if (panel) panel.SetActive(false);
        currentChoices = null;

        isShowing = false;
        isTyping = false;

        if (movementLocker != null)
            movementLocker.Unlock();


        if (currentActorId != 0)
            talkedActorIds.Add(currentActorId);

        flow = null;
        onChoice = null;
        onFinished?.Invoke();
    }

    // ================== CURSOR ==================


}
