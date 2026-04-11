using UnityEngine;

public static class ItemResolver
{
    public static DeliveryItemData GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        var items = Resources.LoadAll<DeliveryItemData>("Items");

        foreach (var item in items)
        {
            if (item != null && item.itemId == itemId)
                return item;
        }

        Debug.LogWarning($"[ItemResolver] Item not found: {itemId}");
        return null;
    }
}
