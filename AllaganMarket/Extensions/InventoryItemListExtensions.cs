using System.Collections.Generic;
using System.Linq;

using AllaganMarket.Models;

using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AllaganMarket.Extensions;

public static class InventoryItemListExtensions
{
    public static IEnumerable<SaleItem> SortByRetainerMarketOrder(
        this IEnumerable<SaleItem> saleItem)
    {
        return saleItem.OrderBy(c => c.MenuIndex);
    }

    public static Item? GetItem(this InventoryItem inventoryItem, ExcelSheet<Item> itemSheet)
    {
        return itemSheet.GetRow(inventoryItem.ItemId);
    }

    public static Item? GetItem(this SaleItem saleItem, ExcelSheet<Item> itemSheet)
    {
        return itemSheet.GetRow(saleItem.ItemId);
    }

    public static Item? GetItem(this SoldItem soldItem, ExcelSheet<Item> itemSheet)
    {
        return itemSheet.GetRow(soldItem.ItemId);
    }
}
