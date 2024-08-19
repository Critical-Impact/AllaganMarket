using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Grids.Columns;


public class UndercutByColumn : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>
{
    // TODO: needs to be a gil column
    public UndercutByColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter) : base(imGuiService, stringColumnFilter)
    {
    }

    public override string DefaultValue { get; set; } = "";

    public override string Key { get; set; } = "UndercutBy";

    public override string Name { get; set; } = "Undercut by";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = "";

    public override string? CurrentValue(SearchResult item)
    {
        return item.SaleItem?.UndercutBy.ToString() ?? string.Empty;
    }

    public override string HelpText { get; set; } = "How much the item was undercut by.";

    public override string Version { get; } = "1.0.0";
}
