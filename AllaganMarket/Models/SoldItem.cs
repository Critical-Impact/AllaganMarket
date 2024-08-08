using System.Globalization;

using AllaganLib.Data.Interfaces;

using Lumina;
using Lumina.Data;

namespace AllaganMarket.Models;

using System;
using Interfaces;

public class SoldItem : IDebuggable, ICsv
{
    public SoldItem(SaleItem saleItem)
    {
        this.RetainerId = saleItem.RetainerId;
        this.WorldId = saleItem.WorldId;
        this.ItemId = saleItem.ItemId;
        this.IsHq = saleItem.IsHq;
        this.Quantity = saleItem.Quantity;
        this.UnitPrice = saleItem.UnitPrice;
        this.SoldAt = DateTime.Now;
    }

    public SoldItem()
    {
    }

    public ulong RetainerId { get; set; }

    public uint WorldId { get; set; }

    public uint ItemId { get; set; }

    public bool IsHq { get; set; }

    public uint Quantity { get; set; }

    public uint UnitPrice { get; set; }

    public uint TaxRate { get; set; }

    public DateTime SoldAt { get; set; }

    public uint Total => this.Quantity * this.UnitPrice;

    public uint TotalIncTax =>
        (uint)(this.Total - (int)Math.Floor((double)(this.Quantity * this.UnitPrice * this.TaxRate)));

    public string AsDebugString()
    {
        return
            $"Retainer ID: {this.RetainerId}, World ID: {this.WorldId}, Item ID: {this.ItemId}, Is HQ: {this.IsHq}, Quantity: {this.Quantity}, Unit Price: {this.UnitPrice}";
    }

    public static string[] GetHeaders()
    {
        return
        [
            "Retainer ID",
            "World ID",
            "Item ID",
            "Is HQ?",
            "Quantity",
            "Unit Price",
            "Tax Rate",
            "Sold At"
        ];
    }

    public void FromCsv(string[] lineData)
    {
        this.RetainerId = Convert.ToUInt64(lineData[0]);
        this.WorldId = Convert.ToUInt32(lineData[1]);
        this.ItemId = Convert.ToUInt32(lineData[2]);
        this.IsHq = lineData[3] == "1" ? true : false;
        this.Quantity = Convert.ToUInt32(lineData[4], CultureInfo.InvariantCulture);
        this.UnitPrice = Convert.ToUInt32(lineData[5], CultureInfo.InvariantCulture);
        this.TaxRate = Convert.ToUInt32(lineData[6], CultureInfo.InvariantCulture);
        this.SoldAt = Convert.ToDateTime(lineData[7], CultureInfo.InvariantCulture);
    }

    public string[] ToCsv()
    {
        return
        [
            this.RetainerId.ToString(),
            this.WorldId.ToString(),
            this.ItemId.ToString(),
            this.IsHq ? "1" : "0",
            this.Quantity.ToString(CultureInfo.InvariantCulture),
            this.UnitPrice.ToString(CultureInfo.InvariantCulture),
            this.TaxRate.ToString(CultureInfo.InvariantCulture),
            this.SoldAt.ToString(CultureInfo.InvariantCulture)
        ];
    }

    public bool IncludeInCsv()
    {
        return this.ItemId != 0 && this.RetainerId != 0;
    }

    public void PopulateData(GameData gameData, Language language)
    {
        
    }
}
