using System;
using UnityEngine;

[Serializable]
public class PackedBoxData
{
    public GameObject boxPrefab;
    public GameObject labelPrefab;
    public Material tapeMaterial;
    public TapeColor tapeColor;
    public BoxKind boxType;
    public BubbleType bubbleType;
    public DeliveryItemData itemData;
    public string destinationId;
    public string ownerNPCName;
    public string address;
    [TextArea]
    public string information;
    [Range(0, 100)]
    public float itemQuality = 100f;
    public int remainingDays;
    public int protectionDivisor = 1;
    [Range(0f, 100f)]
    public float protectionPercent;
    public bool isWaterproof;
    public bool hasIceBubble;
    public bool isDamaged;
    public bool isBroken;

    public void RefreshState()
    {
        if (itemData == null)
        {
            isDamaged = false;
            isBroken = false;
            return;
        }

        isDamaged = itemQuality <= itemData.damagedThreshold;
        isBroken = itemQuality <= itemData.brokenThreshold;
        destinationId = itemData.destinationId;
    }
}
