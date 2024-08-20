using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using Dalamud.Game.Text;

using ImGuiNET;

namespace AllaganMarket.Grids.Columns;

public class TotalColumn : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>
{
    public TotalColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter) : base(imGuiService, stringColumnFilter)
    {
    }

    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "Total";

    public override string Name { get; set; } = "Total " + SeIconChar.Gil.ToIconString();

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 70;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = "";

    public override string? CurrentValue(SearchResult item)
    {
        if (item.SaleItem != null)
        {
            return item.SaleItem.Total.ToString();
        }

        if (item.SoldItem != null)
        {
            return item.SoldItem.Total.ToString();
        }

        if (item.SaleSummaryItem != null)
        {
            return item.SaleSummaryItem.Earned.ToString();
        }

        return null;
    }

    public override string HelpText { get; set; } = "The total amount of the sale item/sold item";

    public override string Version { get; } = "1.0.0";
}
