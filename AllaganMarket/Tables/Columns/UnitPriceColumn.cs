using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Grids.Columns;

public class UnitPriceColumn : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>
{
    public UnitPriceColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter) : base(imGuiService, stringColumnFilter)
    {
    }

    public override string DefaultValue { get; set; } = "";

    public override string Key { get; set; } = "UnitPrice";

    public override string Name { get; set; } = "Unit Price";

    public override string? RenderName { get; set; }

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = "";

    public override string? CurrentValue(SearchResult item)
    {
        return item.SoldItem?.UnitPrice.ToString() ?? item.SaleItem?.UnitPrice.ToString() ?? null;
    }

    public override string HelpText { get; set; } = "The unit price of the item being sold.";

    public override string Version { get; } = "1.0.0";
}