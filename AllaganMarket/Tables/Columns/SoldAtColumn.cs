using System.Globalization;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Grids.Columns;

public class SoldAtColumn : DateTimeColumn<SearchResultConfiguration, SearchResult, MessageBase>
{
    public SoldAtColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter) : base(imGuiService, stringColumnFilter)
    {
    }

    public override string? DefaultValue { get; set; } = null;

    public override string Key { get; set; } = "SoldAt";

    public override string Name { get; set; } = "Sold At";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = "";

    public override string? CurrentValue(SearchResult item)
    {
        return item.SoldItem?.SoldAt.ToString(CultureInfo.CurrentCulture) ?? null;
    }

    public override string HelpText { get; set; } =
        "When the item was sold.";

    public override string Version { get; } = "1.0.0";
}
