namespace AllaganMarket.Models.Bson;

using System.Collections.Generic;
using Interfaces;
using MongoDB.Bson.Serialization.Attributes;
using Services;

public class SubscriptionReceivedMessage : IDebuggable
{
    public UniversalisWebsocketService.EventType EventType
    {
        get
        {
            return this.Event switch
            {
                "listings/add" => UniversalisWebsocketService.EventType.ListingsAdd,
                "listings/remove" => UniversalisWebsocketService.EventType.ListingsRemove,
                "sales/add" => UniversalisWebsocketService.EventType.SalesAdd,
                "sales/remove" => UniversalisWebsocketService.EventType.SalesRemove,
                _ => UniversalisWebsocketService.EventType.Unknown
            };
        }
    }

    [BsonElement("event")]
    public string Event { get; set; }

    [BsonElement("item")]
    public uint Item { get; set; }

    [BsonElement("world")]
    public uint World { get; set; }

    [BsonElement("listings")]
    public List<Listing> Listings { get; set; }

    [BsonElement("sales")]
    public List<Sale> Sales { get; set; }

    public string AsDebugString()
    {
        return $"Event: {this.EventType.ToString()}, Item: {this.Item}, World: {this.World}, Listings: {this.Listings.Count}";
    }
}
