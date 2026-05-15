using UnityEngine;

public static class DeliveryCalculationService
{
    public static int CalculateEffectiveDeadlineDays(
        DeliveryItemData itemData,
        int baseDays,
        bool inColdBox,
        bool hasIceBubble)
    {
        if (itemData == null || !itemData.requiresCold)
            return baseDays;

        if (inColdBox)
            return baseDays + (hasIceBubble ? 1 : 0);

        return Mathf.Max(1, baseDays / 3);
    }

    public static int CalculateReward(
        DeliveryItemData itemData,
        float quality,
        int dayCreated,
        int dayDelivered,
        int effectiveLimitDays,
        bool isBroken)
    {
        if (itemData == null)
            return 0;

        int daysUsed = Mathf.Max(0, dayDelivered - dayCreated);
        float reward = itemData.baseReward * Mathf.Clamp01(quality / 100f);

        if (effectiveLimitDays > 0 && daysUsed > effectiveLimitDays)
            reward *= 0.5f;

        if (isBroken || quality <= itemData.brokenThreshold)
            reward = 0f;

        return Mathf.Max(0, Mathf.RoundToInt(reward));
    }

    public static int CalculateFallDamage(DeliveryItemData itemData, float fallHeight, int damageDivisor)
    {
        if (itemData == null)
            return 0;

        int meters = Mathf.RoundToInt(fallHeight);
        if (meters < itemData.minFallHeightMeter)
            return 0;

        int rawDamage = Mathf.Max(0, itemData.damagePerMeter) * meters;
        int divisor = Mathf.Max(1, damageDivisor);
        return Mathf.Max(0, Mathf.RoundToInt((float)rawDamage / divisor));
    }

    public static float ApplyQualityDamage(float currentQuality, float damage)
    {
        if (damage <= 0f)
            return Mathf.Clamp(currentQuality, 0f, 100f);

        return Mathf.Clamp(currentQuality - damage, 0f, 100f);
    }

    public static void EvaluateQualityState(
        DeliveryItemData itemData,
        float quality,
        out bool isDamaged,
        out bool isBroken)
    {
        if (itemData == null)
        {
            isDamaged = quality < 100f;
            isBroken = quality <= 0f;
            return;
        }

        isDamaged = quality <= itemData.damagedThreshold;
        isBroken = quality <= itemData.brokenThreshold;
    }
}
