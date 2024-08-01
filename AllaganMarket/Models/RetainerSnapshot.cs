namespace AllaganMarket.Models;

using System.Linq;
using Interfaces;

public class RetainerSnapshot : IDebuggable
{
    public RetainerSnapshot(uint gil, SaleItem[] saleItems)
    {
        this.Gil = gil;
        this.SaleItems = saleItems;
    }

    public uint Gil { get; set; }

    public SaleItem[] SaleItems { get; set; }

    public string AsDebugString()
    {
        return $"Retainer Gil: {this.Gil}, Sale Items: {this.SaleItems.Count(c => c.ItemId != 0)}";
    }
}