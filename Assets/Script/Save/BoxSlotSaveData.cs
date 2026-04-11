using System;

[Serializable]
public class BoxSlotSaveData
{
    public bool hasBox;
    public BoxKind boxType;
    public string itemId;

    public float quality;
    public int remainingDays;

    public int protectionDivisor;
    public float protectionPercent;
    public bool isWaterproof;
}
