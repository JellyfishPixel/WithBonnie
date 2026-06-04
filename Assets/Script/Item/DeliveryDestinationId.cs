using System;

public static class DeliveryDestinationId
{
    public static bool Matches(string a, string b)
    {
        return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
