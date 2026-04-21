using UnityEngine;

public class TrashBinScavenge : MonoBehaviour, IInteractable
{
    [Header("Daily Limit")]
    [SerializeField] int maxScavengesPerDay = 5;

    [Header("Reward Weights")]
    [SerializeField] int smallBoxWeight = 40;
    [SerializeField] int tapeWeight = 35;
    [SerializeField] int bubbleWeight = 25;

    [Header("Reward Amounts")]
    [SerializeField] int smallBoxAmount = 1;
    [SerializeField] int tapeUsesAmount = 1;
    [SerializeField] int bubbleUsesAmount = 1;

    [Header("Feedback")]
    [SerializeField] AudioClip rummageSound;
    [SerializeField] AudioClip successSound;
    [SerializeField] AudioClip failSound;

    static int cachedDay = -1;
    static int scavengesToday = 0;
    static bool miniGameActive;
    static int cachedMaxPerDay = 5;

    enum TrashRewardType
    {
        SmallBox,
        Tape,
        Bubble
    }

    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType interactionType)
    {
        if (interactionType != PlayerInteractionSystem.InteractionType.Primary)
            return;

        GameManager gm = GameManager.Instance;
        EconomyManager eco = EconomyManager.Instance;
        if (gm == null || eco == null)
            return;

        SyncDay(gm.currentDay);
        cachedMaxPerDay = Mathf.Max(1, maxScavengesPerDay);

        if (miniGameActive)
        {
            AddSalesPopupUI.ShowMessage("Already rummaging through trash.");
            return;
        }

        if (scavengesToday >= maxScavengesPerDay)
        {
            AddSalesPopupUI.ShowMessage($"No more trash loot today.\nCome back tomorrow.\n{scavengesToday}/{maxScavengesPerDay} used");
            return;
        }

        miniGameActive = true;
        TrashScavengeMiniGameUI.Show(this, maxScavengesPerDay - scavengesToday);

        if (rummageSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(rummageSound, transform.position);
    }

    public void CompleteMiniGame(bool success)
    {
        miniGameActive = false;

        GameManager gm = GameManager.Instance;
        EconomyManager eco = EconomyManager.Instance;
        if (gm == null || eco == null)
            return;

        SyncDay(gm.currentDay);
        cachedMaxPerDay = Mathf.Max(1, maxScavengesPerDay);
        scavengesToday++;

        if (!success)
        {
            if (failSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(failSound, transform.position);

            AddSalesPopupUI.ShowMessage($"You found nothing.\nTrash searches: {scavengesToday}/{maxScavengesPerDay}");
            return;
        }

        TrashRewardType rewardType = RollRewardType();
        string rewardText = GrantReward(eco, rewardType);

        if (successSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(successSound, transform.position);

        AddSalesPopupUI.ShowMessage($"{rewardText}\nTrash searches: {scavengesToday}/{maxScavengesPerDay}");

        var shopUI = FindFirstObjectByType<BoxShopUI>(FindObjectsInactive.Include);
        if (shopUI != null)
            shopUI.RefreshUI();
    }

    void SyncDay(int currentDay)
    {
        if (cachedDay == currentDay)
            return;

        cachedDay = currentDay;
        scavengesToday = 0;
    }

    TrashRewardType RollRewardType()
    {
        int totalWeight = Mathf.Max(1, smallBoxWeight + tapeWeight + bubbleWeight);
        int roll = Random.Range(0, totalWeight);

        if (roll < smallBoxWeight)
            return TrashRewardType.SmallBox;

        roll -= smallBoxWeight;
        if (roll < tapeWeight)
            return TrashRewardType.Tape;

        return TrashRewardType.Bubble;
    }

    string GrantReward(EconomyManager eco, TrashRewardType rewardType)
    {
        switch (rewardType)
        {
            case TrashRewardType.SmallBox:
                eco.AddBox(BoxSizeSimple.Small, Mathf.Max(1, smallBoxAmount));
                return $"+{Mathf.Max(1, smallBoxAmount)} Small Box";

            case TrashRewardType.Tape:
                TapeColor tapeColor = RandomTapeColor();
                eco.AddTapeUses(tapeColor, Mathf.Max(1, tapeUsesAmount));
                return $"+{Mathf.Max(1, tapeUsesAmount)} {tapeColor} Tape Use";

            default:
                eco.AddBubbleUses(BubbleType.Basic, Mathf.Max(1, bubbleUsesAmount));
                return $"+{Mathf.Max(1, bubbleUsesAmount)} Basic Bubble Use";
        }
    }

    TapeColor RandomTapeColor()
    {
        int count = System.Enum.GetValues(typeof(TapeColor)).Length;
        return (TapeColor)Random.Range(0, count);
    }

    public static int GetRemainingToday()
    {
        if (GameManager.Instance == null)
            return 0;

        if (cachedDay != GameManager.Instance.currentDay)
            return cachedMaxPerDay;

        return Mathf.Max(0, cachedMaxPerDay - scavengesToday);
    }
}
