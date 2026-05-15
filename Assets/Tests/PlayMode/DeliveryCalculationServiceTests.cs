using NUnit.Framework;
using UnityEngine;

public class DeliveryCalculationServiceTests
{
    DeliveryItemData itemData;

    [SetUp]
    public void SetUp()
    {
        itemData = ScriptableObject.CreateInstance<DeliveryItemData>();
        itemData.baseReward = 100;
        itemData.deliveryLimitDays = 3;
        itemData.damagedThreshold = 70f;
        itemData.brokenThreshold = 20f;
        itemData.minFallHeightMeter = 2;
        itemData.damagePerMeter = 5;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(itemData);
    }

    [Test]
    public void Deadline_ColdItemInRegularBox_IsReducedToAtLeastOneDay()
    {
        itemData.requiresCold = true;

        int result = DeliveryCalculationService.CalculateEffectiveDeadlineDays(
            itemData,
            baseDays: 3,
            inColdBox: false,
            hasIceBubble: false);

        Assert.AreEqual(1, result);
    }

    [Test]
    public void Deadline_ColdItemWithIceBubbleInColdBox_GainsOneDay()
    {
        itemData.requiresCold = true;

        int result = DeliveryCalculationService.CalculateEffectiveDeadlineDays(
            itemData,
            baseDays: 3,
            inColdBox: true,
            hasIceBubble: true);

        Assert.AreEqual(4, result);
    }

    [Test]
    public void Reward_UsesQualityAndLatePenalty()
    {
        int reward = DeliveryCalculationService.CalculateReward(
            itemData,
            quality: 50f,
            dayCreated: 1,
            dayDelivered: 5,
            effectiveLimitDays: 3,
            isBroken: false);

        Assert.AreEqual(25, reward);
    }

    [Test]
    public void Reward_BrokenItem_IsZero()
    {
        int reward = DeliveryCalculationService.CalculateReward(
            itemData,
            quality: 10f,
            dayCreated: 1,
            dayDelivered: 1,
            effectiveLimitDays: 3,
            isBroken: true);

        Assert.AreEqual(0, reward);
    }

    [Test]
    public void FallDamage_RespectsMinimumHeightAndDivisor()
    {
        Assert.AreEqual(0, DeliveryCalculationService.CalculateFallDamage(itemData, 1f, 1));
        Assert.AreEqual(5, DeliveryCalculationService.CalculateFallDamage(itemData, 2f, 2));
    }
}
