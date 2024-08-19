using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Grids.Columns;

public class QuantityColumn : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>
{
    public QuantityColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
        : base(imGuiService, stringColumnFilter)
    {
    }

    public override string DefaultValue { get; set; }

    public override string Key { get; set; } = "quantity";

    public override string Name { get; set; } = "Quantity";

    public override string? RenderName { get; set; }

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; }

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; }

    public override string? CurrentValue(SearchResult item)
    {
        return item.SaleItem?.Quantity.ToString() ?? item.SoldItem?.Quantity.ToString();
    }

    public override string HelpText { get; set; } = "The quantity of item being sold.";

    public override string Version { get; } = "1.0.0";
}
