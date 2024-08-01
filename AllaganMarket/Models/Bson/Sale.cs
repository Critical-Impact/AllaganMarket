namespace AllaganMarket.Models.Bson;

using MongoDB.Bson.Serialization.Attributes;

public class Sale
{
    [BsonElement("buyerName")]
    public string BuyerName { get; set; }

    [BsonElement("hq")]
    public bool HQ { get; set; }

    [BsonElement("onMannequin")]
    public bool OnMannequin { get; set; }

    [BsonElement("pricePerUnit")]
    public int PricePerUnit { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("timestamp")]
    public long Timestamp { get; set; }

    [BsonElement("total")]
    public int Total { get; set; }

    [BsonElement("worldID")]
    public int? WorldID { get; set; }

    [BsonElement("worldName")]
    public string WorldName { get; set; }
}
