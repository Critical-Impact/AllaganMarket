using System.Collections.Generic;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;
using AllaganLib.Shared.Extensions;

using DalaMock.Host.Mediator;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AllaganMarket.Grids.Columns;

public class NameColumn : StringColumn<SearchResultConfiguration,SearchResult, MessageBase>
{
    private readonly ExcelSheet<Item> itemSheet;

    public NameColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter, ExcelSheet<Item> itemSheet) : base(imGuiService, stringColumnFilter)
    {
        this.itemSheet = itemSheet;
    }

    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "Name";

    public override string Name { get; set; } = "Item";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string? CurrentValue(SearchResult item)
    {
        if (item.SaleItem != null)
        {
            if (item.SaleItem.ItemId == 0)
            {
                return "Empty Slot";
            }

            return this.itemSheet.GetRow(item.SaleItem.ItemId)?.Name.AsReadOnly().ToString() ?? string.Empty;
        }
        else if (item.SoldItem != null)
        {
            return this.itemSheet.GetRow(item.SoldItem.ItemId)?.Name.AsReadOnly().ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The name of the item";

    public override string Version { get; } = "1.0.0";
}
