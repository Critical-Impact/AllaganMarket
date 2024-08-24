using System.Collections.Generic;
using System.Linq;

using AllaganMarket.Models;

using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AllaganMarket.Extensions;

public static class InventoryItemListExtensions
{
    public static IEnumerable<InventoryItem> SortByRetainerMarketOrder(
        this IEnumerable<InventoryItem> item,
        ExcelSheet<Item> itemSheet)
    {
        return item.OrderBy(c => c.GetItem(itemSheet)?.ItemUICategory.Value?.OrderMajor ?? 0)
                   .ThenBy(c => c.GetItem(itemSheet)?.ItemUICategory.Value?.OrderMinor ?? 0)
                   .ThenBy(c => c.GetItem(itemSheet)?.Unknown19)
                   .ThenBy(c => c.GetItem(itemSheet)?.RowId);
    }

    public static IEnumerable<SaleItem> SortByRetainerMarketOrder(
        this IEnumerable<SaleItem> saleItem,
        ExcelSheet<Item> itemSheet)
    {
        return saleItem.OrderBy(c => c.ItemId == 0 ? 0 : -1)
                       .ThenBy(c => c.GetItem(itemSheet)?.ItemUICategory.Value?.OrderMajor ?? 0)
                       .ThenBy(c => c.GetItem(itemSheet)?.ItemUICategory.Value?.OrderMinor ?? 0)
                       .ThenBy(c => c.GetItem(itemSheet)?.Unknown19)
                       .ThenBy(c => c.GetItem(itemSheet)?.RowId);
    }

    public static Item? GetItem(this InventoryItem inventoryItem, ExcelSheet<Item> itemSheet)
    {
        return itemSheet.GetRow(inventoryItem.ItemId);
    }

    public static Item? GetItem(this SaleItem saleItem, ExcelSheet<Item> itemSheet)
    {
        return itemSheet.GetRow(saleItem.ItemId);
    }
}
