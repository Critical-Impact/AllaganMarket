namespace AllaganMarket.Models;

using System;
using Interfaces;

public class SoldItem : IDebuggable
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
}
