using System;

[Serializable]
public class DeliverySaveData
{
    public string destinationId;
    public string itemId;

    public int dayCreated;
    public float itemQuality;
    public int remainingDays;
}
