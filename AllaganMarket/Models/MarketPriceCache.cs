using System;
using System.Globalization;

using AllaganLib.Data.Interfaces;

using Lumina;
using Lumina.Data;

namespace AllaganMarket.Models;

public class MarketPriceCache : ICsv
{
    public MarketPriceCache(uint itemId, bool isHq, uint worldId, MarketPriceCacheType type, DateTime lastUpdated, uint unitCost, bool ownPrice)
    {
        this.ItemId = itemId;
        this.IsHq = isHq;
        this.WorldId = worldId;
        this.Type = type;
        this.LastUpdated = lastUpdated;
        this.UnitCost = unitCost;
        this.OwnPrice = ownPrice;
    }

    public MarketPriceCache()
    {
    }

    public uint ItemId { get; set; }

    public bool IsHq { get; set; }

    public uint WorldId { get; set; }

    public MarketPriceCacheType Type { get; set; }

    public DateTime LastUpdated { get; set; }

    public uint UnitCost { get; set; }

    public bool OwnPrice { get; set; }

    public string GetFormattedType()
    {
        switch (this.Type)
        {
            case MarketPriceCacheType.Game:
                return "In-Game";
            case MarketPriceCacheType.UniversalisWS:
            case MarketPriceCacheType.UniversalisReq:
                return "Universalis";
            case MarketPriceCacheType.Override:
                return "Forced Update";
        }

        return "Unknown";
    }

    public static string[] GetHeaders()
    {
        return ["ItemId", "WorldId", "Type", "LastUpdated", "UnitCost", "OwnPrice"];
    }

    public void FromCsv(string[] lineData)
    {
        this.ItemId = uint.Parse(lineData[0], CultureInfo.InvariantCulture);
        this.IsHq = lineData[1] == "Y";
        this.WorldId = uint.Parse(lineData[2], CultureInfo.InvariantCulture);
        this.Type = (MarketPriceCacheType)uint.Parse(lineData[3], CultureInfo.InvariantCulture);
        this.LastUpdated = DateTime.Parse(lineData[4], CultureInfo.InvariantCulture);
        this.UnitCost = uint.Parse(lineData[5], CultureInfo.InvariantCulture);
        this.OwnPrice = lineData[6] == "Y";
    }

    public string[] ToCsv()
    {
        return
        [
            this.ItemId.ToString(CultureInfo.InvariantCulture),
            this.IsHq == true ? "Y" : "N",
            this.WorldId.ToString(CultureInfo.InvariantCulture),
            ((uint)this.Type).ToString(CultureInfo.InvariantCulture),
            this.LastUpdated.ToString(CultureInfo.InvariantCulture),
            this.UnitCost.ToString(CultureInfo.InvariantCulture),
            this.OwnPrice == true ? "Y" : "N"
        ];
    }

    public bool IncludeInCsv()
    {
        return true;
    }

    public void PopulateData(GameData gameData, Language language)
    {
    }
}
