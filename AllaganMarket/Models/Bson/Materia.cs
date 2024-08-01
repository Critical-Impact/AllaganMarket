namespace AllaganMarket.Models.Bson;

using MongoDB.Bson.Serialization.Attributes;

public class Materia
{
    [BsonElement("slotID")]
    public int SlotID { get; set; }

    [BsonElement("materiaID")]
    public int MateriaID { get; set; }
}
