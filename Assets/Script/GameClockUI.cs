using TMPro;
using UnityEngine;

public class GameClockUI : MonoBehaviour
{
    public TMP_Text dayText;
    public TMP_Text timeText;
    public TMP_Text moneyText;

    void Update()
    {
        var gm = GameManager.Instance;
        if (!gm) return;

        if (dayText)
            dayText.text = $"DAY {gm.currentDay:00}";

        if (timeText)
            timeText.text = $"{gm.currentHour:00}:{gm.currentMinute:00}";

        if (moneyText)
            moneyText.text = $"{gm.totalMoney:N0} $";   // หรือหน่วยอื่น
    }
}
