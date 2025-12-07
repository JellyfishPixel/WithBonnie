using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using UnityEngine.SceneManagement;
using static ItemDialogueData;

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
    [Tooltip("ตัวอักษร/วินาที")] public float charsPerSecond = 40f;
    [Tooltip("หน่วงเมื่อเจอเครื่องหมายวรรคตอน")] public float punctuationPause = 0.08f;
    [Tooltip("รองรับ <b>, <i>, <color>")] public bool supportRichText = true;
    [Tooltip("เสียงทีละตัว (ออปชัน)")] public AudioClip perCharSfx;
    [Tooltip("ทุก N ตัวอักษรจะเล่นเสียง 1 ครั้ง")] public int sfxEveryNChars = 2;

    [Header("UI Buttons")]
    public Button nextButton;

    [Header("Player Control")]
    public FirstPersonController player;   

    private ItemDialogueData flow;
    private int stepIndex;
    private bool isShowing;
    private bool isTyping;
    private Coroutine typeCo;
    private Action<int> onChoice;
    private Action onFinished;
    public bool IsShowing => isShowing;

    public bool hasEverTalked = false;

    [Header("Debounce")]
    [Tooltip("เวลาหน่วงเพื่อกันคลิกแรกไหลไปข้าม step0")]
    public float advanceCooldown = 0.12f;

    // choice ที่ผู้เล่นเลือกไว้: key = flowName#stepIndex -> choiceIndex
    private readonly Dictionary<string, int> choiceMemory = new Dictionary<string, int>();

    // โหมดทวน (ครั้งเดียวต่อการเรียก ShowReview)
    private bool reviewMode = false;
    private readonly HashSet<int> talkedActorIds = new HashSet<int>();
    private int currentActorId = 0;

    // ใช้เก็บ echo ให้แสดงเป็น line ก่อนจะไป goto จริง
    private struct EchoLine { public string text; public int gotoIndex; }
    private readonly Queue<EchoLine> echoQueue = new Queue<EchoLine>();

    private string currentFullText = "";

    // =============== ACTOR & MEMORY ====================

    public void ForgetActor(int actorInstanceId)
    {
        talkedActorIds.Remove(actorInstanceId);
        if (currentActorId == actorInstanceId) currentActorId = 0;
    }

    string ChoiceKeyFor(int stepIdx)
    {
        string flowId = flow ? flow.name : "noflow";
        return $"{flowId}#{stepIdx}";
    }

    private bool IsFirstTimeForActor(GameObject actor)
    {
        currentActorId = actor ? actor.GetInstanceID() : 0;
        return currentActorId == 0 ? true : !talkedActorIds.Contains(currentActorId);
    }

    // =============== CURSOR HELPER (แทน CursorCoordinator) ===============

    void SetCursor(bool show)
    {
        SetDialogueCursorActive(show);
    }

    void SetDialogueCursorActive(bool active)
    {
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;
    }

    // =============== LIFE CYCLE ====================

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (panel) panel.SetActive(false);

        choiceMemory.Clear();
    }

    void Start()
    {
        SetCursor(false);
        player = FindFirstObjectByType<FirstPersonController>();
    }

    void ResetSessionState(bool clearChoices)
    {
        hasEverTalked = false;
        isTyping = false;

        if (typeCo != null)
        {
            StopCoroutine(typeCo);
            typeCo = null;
        }

        if (clearChoices)
            choiceMemory.Clear();
    }


    // =============== PUBLIC API ====================

    // รอบปกติ
    public void Show(GameObject actorOwner, ItemDialogueData flow,
                     Action<int> onChoice = null, Action onFinished = null)
    {
        bool firstTime = IsFirstTimeForActor(actorOwner);
        bool forceReview = !firstTime;  // ถ้าเคยคุยแล้ว → review mode

        InternalShow(flow, onChoice, onFinished, forceReview);
    }

    // รอบรีวิว (ใช้ช้อยส์เดิม)
    public void ShowReview(ItemDialogueData flow,
                           Action<int> onChoice = null, Action onFinished = null)
    {
        InternalShow(flow, onChoice, onFinished, forceReview: true);
    }

    void InternalShow(ItemDialogueData flow, Action<int> onChoice, Action onFinished, bool forceReview)
    {
        if (flow == null || flow.steps == null || flow.steps.Length == 0)
        {
            Debug.LogWarning("[ItemDialogueManager] Invalid flow");
            return;
        }

        reviewMode = forceReview;

        // reviewMode = true → ไม่ล้าง choiceMemory
        ResetSessionState(clearChoices: !forceReview);

        this.flow = flow;
        this.onChoice = onChoice;
        this.onFinished = onFinished;
        stepIndex = 0;

        if (panel) panel.SetActive(true);

        // 🔒 ล็อก movement ด้วย LockMovement()
        if (flow.lockPlayer && player != null)
        {
            player.LockMovement();
        }

        if (flow.openSfx && Camera.main)
            AudioSource.PlayClipAtPoint(flow.openSfx, Camera.main.transform.position);

        HideAllChoices();

        if (forceReview)
        {
            enableTyping = false;
            hasEverTalked = true;
        }
        else
        {
            enableTyping = true;
            hasEverTalked = false;
        }

        isShowing = true;
        reviewMode = forceReview;

        // เปิดเคอร์เซอร์ช่วงคุย
        SetDialogueCursorActive(true);

        ShowCurrentStep();
    }

    // =============== UI CONTROL ====================

    void HideAllChoices()
    {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (!optionButtons[i]) continue;
            optionButtons[i].gameObject.SetActive(false);
            optionButtons[i].onClick.RemoveAllListeners();
        }
    }

    bool HasChoices(ItemDialogueData.Step step)
    {
        return step != null && step.options != null && step.options.Length >= 2;
    }

    void ShowCurrentStep()
    {
        if (flow == null || flow.steps == null)
        {
            Close();
            return;
        }

        if (stepIndex < 0 || stepIndex >= flow.steps.Length)
        {
            Close();
            onFinished?.Invoke();
            return;
        }

        var step = flow.steps[stepIndex];
        HideAllChoices();

        if (speakerText) speakerText.text = string.IsNullOrEmpty(step.speaker) ? "" : step.speaker;

        // ถ้ามี echo ค้างให้แสดงก่อน
        if (echoQueue.Count > 0)
        {
            var e = echoQueue.Peek();
            if (bodyText) bodyText.text = e.text;
            if (nextButton) nextButton.gameObject.SetActive(true);
            return;
        }

        // Review mode + มี Choices → ใช้ช้อยส์ที่เคยเลือก
        if (HasChoices(step) && reviewMode)
        {
            int chosenIdx = 0;
            string key = ChoiceKeyFor(stepIndex);
            if (choiceMemory.TryGetValue(key, out int savedIdx))
                chosenIdx = Mathf.Clamp(savedIdx, 0, step.options.Length - 1);

            var opt = step.options[chosenIdx];
            ShowEchoLineNow(opt.text, opt.gotoIndex);
            return;
        }

        // ไม่มีช้อยส์ → line ปกติ
        if (!HasChoices(step))
        {
          
            if (nextButton) nextButton.gameObject.SetActive(true);

            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
                    if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                        StartCoroutine(InvokeAfterDelay(() => ExecuteLineEndAction(step), Mathf.Max(0f, step.onLineEndDelay)));

                    if (step.gotoIndex < 0)
                    {
                        if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                        {
                            Close();
                            onFinished?.Invoke();
                            return;
                        }
                    }
                }));
            }
            else
            {
                if (bodyText) bodyText.text = step.text ?? "";
                isTyping = false;

                if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                    StartCoroutine(InvokeAfterDelay(() => ExecuteLineEndAction(step), Mathf.Max(0f, step.onLineEndDelay)));

                if (step.gotoIndex < 0)
                {
                    if (step.onLineEndAction != ItemDialogueData.LineAction.None)
                    {
                        Close();
                        onFinished?.Invoke();
                        return;
                    }
                }
            }
        }
        else
        {
            // มีช้อยส์
            if (nextButton) nextButton.gameObject.SetActive(false);

        

            if (enableTyping)
            {
                if (typeCo != null) StopCoroutine(typeCo);
                typeCo = StartCoroutine(TypeLine(step.text, step.voice, onTypedDone: () =>
                {
          
                    ShowChoices(step.options);
                }));
            }
            else
            {
                if (bodyText) bodyText.text = step.text ?? "";
       
                ShowChoices(step.options);
            }
        }
    }

    void ShowEchoLineNow(string text, int gotoIndex)
    {
        HideAllChoices();


        SetDialogueCursorActive(true);

        if (nextButton)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = !enableTyping;
        }

        if (enableTyping)
        {
            if (typeCo != null) StopCoroutine(typeCo);
            typeCo = StartCoroutine(TypeLine(
                text ?? "",
                null,
                onTypedDone: () =>
                {
                    isTyping = false;
                    if (nextButton) nextButton.interactable = true;
                }
            ));
        }
        else
        {
            isTyping = false;
            if (bodyText) bodyText.text = text ?? "";
            if (nextButton) nextButton.interactable = true;
        }

        echoQueue.Enqueue(new EchoLine { text = text ?? "", gotoIndex = gotoIndex });
    }

    void ShowChoices(ItemDialogueData.ChoiceOption[] options)
    {
       
        SetDialogueCursorActive(true);

        if (options == null || options.Length < 2)
        {
            GoTo(flow.steps[stepIndex].gotoIndex);
            return;
        }

        int count = Mathf.Clamp(options.Length, 2, 4);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool enable = i < count;
            if (!optionButtons[i]) continue;

            optionButtons[i].gameObject.SetActive(enable);
            optionButtons[i].onClick.RemoveAllListeners();

            if (enable)
            {
                if (i < optionLabels.Length && optionLabels[i])
                    optionLabels[i].text = options[i].text ?? "";

                int idx = i;
                optionButtons[i].onClick.AddListener(() =>
                {
                    // ล็อกปุ่มอื่น
                    for (int k = 0; k < optionButtons.Length; k++)
                        if (optionButtons[k]) optionButtons[k].interactable = false;

                    // แจ้ง NPC ว่าเลือกช้อยส์ไหน (รับ / ไม่รับ)
                    onChoice?.Invoke(idx);

                    // ถ้าระหว่าง onChoice() มีการ Close() ไปแล้ว → ไม่ทำอะไรต่อ (ป้องกันเมาส์ค้าง)
                    if (!isShowing)
                        return;

                    string key = ChoiceKeyFor(stepIndex);
                    if (choiceMemory.ContainsKey(key)) choiceMemory[key] = idx;
                    else choiceMemory.Add(key, idx);

                    // แสดง echo ข้อความช้อยส์ และต่อไป step ตาม gotoIndex
                    ShowEchoLineNow(options[idx].text, options[idx].gotoIndex);
                });


                optionButtons[i].interactable = true;
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // =============== TYPING ====================

    IEnumerator TypeLine(string text, AudioClip voice, Action onTypedDone = null)
    {
        isTyping = true;
        currentFullText = text ?? "";

        if (bodyText) bodyText.text = "";

        if (voice && Camera.main)
            AudioSource.PlayClipAtPoint(voice, Camera.main.transform.position);

        text ??= "";
        int i = 0;
        float secPerChar = (charsPerSecond <= 0f) ? 0f : (1f / charsPerSecond);

        while (i < text.Length)
        {
            if (!isTyping)
                break;

            if (supportRichText && text[i] == '<')
            {
                int closeIdx = text.IndexOf('>', i);
                if (closeIdx == -1) closeIdx = i;
                Append(text.Substring(i, closeIdx - i + 1));
                i = closeIdx + 1;
            }
            else
            {
                Append(text[i].ToString());
                i++;

                if (perCharSfx && sfxEveryNChars > 0 && (i % sfxEveryNChars == 0) && Camera.main)
                    AudioSource.PlayClipAtPoint(perCharSfx, Camera.main.transform.position, 0.7f);

                if (secPerChar > 0f) yield return new WaitForSeconds(secPerChar);
                if (punctuationPause > 0f && IsPunc(text[i - 1]))
                    yield return new WaitForSeconds(punctuationPause);
            }
        }

        if (bodyText) bodyText.text = currentFullText;

        isTyping = false;
        onTypedDone?.Invoke();
    }

    void Append(string s)
    {
        if (bodyText) bodyText.text += s;
    }

    bool IsPunc(char c)
    {
        return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == '…' || c == '，' || c == '。';
    }

    public void SkipTypingOrNext()
    {
        if (!isShowing) return;

        if (isTyping)
        {
            isTyping = false;
            if (typeCo != null) { StopCoroutine(typeCo); typeCo = null; }
            if (bodyText) bodyText.text = currentFullText;
            if (nextButton) nextButton.interactable = true;
            return;
        }

        OnNextButtonPressed();
    }

    public void OnNextButtonPressed()
    {
        if (!isShowing || isTyping) return;

        // มี echo pending อยู่
        if (echoQueue.Count > 0)
        {
            var e = echoQueue.Dequeue();
            GoTo(e.gotoIndex);
            return;
        }

        var step = (flow != null && stepIndex >= 0 && stepIndex < flow.steps.Length)
            ? flow.steps[stepIndex] : null;

        if (step != null && !HasChoices(step))
        {
            GoTo(step.gotoIndex);
        }
    }

    public void Oncilck()
    {
        if (flow == null || flow.steps == null) return;
        if (stepIndex < 0 || stepIndex >= flow.steps.Length) return;

        var step = flow.steps[stepIndex];
        if (!HasChoices(step))
        {
            GoTo(step.gotoIndex);
        }
    }

    void GoTo(int gotoIndex)
    {
        if (gotoIndex < 0)
        {
            Close();
            onFinished?.Invoke();
            return;
        }

        if (gotoIndex >= 0 && gotoIndex < (flow?.steps?.Length ?? 0))
        {
            stepIndex = gotoIndex;
            ShowCurrentStep();
        }
        else
        {
            Close();
            onFinished?.Invoke();
        }
    }

    IEnumerator InvokeAfterDelay(Action act, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        act?.Invoke();
    }

    void ExecuteLineEndAction(ItemDialogueData.Step step)
    {
        if (step == null) return;

        var npc = FindFirstObjectByType<NPC>();
        if (!npc)
        {
            Debug.LogWarning("[Dialogue] No NPC found for action.");
            return;
        }

        switch (step.onLineEndAction)
        {
            case ItemDialogueData.LineAction.None:
                break;

            case ItemDialogueData.LineAction.Accept:
                npc.OnAcceptDelivery();
                break;

            case ItemDialogueData.LineAction.Decline:
                npc.OnDeclineDelivery();
                break;
        }
    }

    // =============== CLOSE ====================

    public void Close()
    {
        if (panel) panel.SetActive(false);
        if (nextButton) nextButton.gameObject.SetActive(false);

        isShowing = false;
        isTyping = false;

        SetDialogueCursorActive(false);

        if (flow != null && flow.lockPlayer && player != null)
        {
            player.UnlockMovement();
        }

        if (currentActorId != 0)
            talkedActorIds.Add(currentActorId);

        flow = null;
        stepIndex = 0;
        onChoice = null;
        onFinished = null;

        hasEverTalked = true;
        reviewMode = false;
    }


    public int GetCurrentStepIndex()
    {
        return stepIndex;
    }

}
