using System.Linq;

using AllaganMarket.Interfaces;

namespace AllaganMarket.Models;

public class RetainerSnapshot(uint gil, SaleItem[] saleItems) : IDebuggable
{
    public uint Gil { get; set; } = gil;

    public SaleItem[] SaleItems { get; set; } = saleItems;

    public string AsDebugString()
    {
        return $"Retainer Gil: {this.Gil}, Sale Items: {this.SaleItems.Count(c => c.ItemId != 0)}";
    }
}
