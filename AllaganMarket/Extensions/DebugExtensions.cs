using AllaganLib.Universalis.Models.Bson;

namespace AllaganMarket.Extensions;

public static class DebugExtensions
{
    public static string ToDebugString(this SubscriptionReceivedMessage subscriptionReceivedMessage)
    {
        return
            $"Event: {subscriptionReceivedMessage.EventType}, Item: {subscriptionReceivedMessage.Item}, World: {subscriptionReceivedMessage.World}, Listings: {subscriptionReceivedMessage.Listings.Count}";
    }
}
