namespace AllaganMarket.Models.Bson;

using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

public class Listing
{
    [BsonElement("creatorID")]
    public int? CreatorID { get; set; }

    [BsonElement("creatorName")]
    public string CreatorName { get; set; }

    [BsonElement("hq")]
    public bool HQ { get; set; }

    [BsonElement("isCrafted")]
    public bool IsCrafted { get; set; }

    [BsonElement("lastReviewTime")]
    public long LastReviewTime { get; set; }

    [BsonElement("listingID")]
    public string ListingID { get; set; }

    [BsonElement("materia")]
    public List<Materia> Materia { get; set; }

    [BsonElement("onMannequin")]
    public bool OnMannequin { get; set; }

    [BsonElement("pricePerUnit")]
    public int PricePerUnit { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("tax")]
    public int Tax { get; set; }

    [BsonElement("retainerCity")]
    public int RetainerCity { get; set; }

    [BsonElement("retainerID")]
    public string RetainerID { get; set; }

    [BsonElement("retainerName")]
    public string RetainerName { get; set; }

    [BsonElement("sellerID")]
    public string SellerID { get; set; }

    [BsonElement("stainID")]
    public int StainID { get; set; }

    [BsonElement("total")]
    public int Total { get; set; }

    [BsonElement("worldID")]
    public int? WorldID { get; set; }

    [BsonElement("worldName")]
    public string WorldName { get; set; }
}
