using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using Dalamud.Game.Text;

using ImGuiNET;

namespace AllaganMarket.Tables.Columns;

public class TaxColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "Tax";

    public override string Name { get; set; } = "Tax " + SeIconChar.Gil.ToIconString();

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 70;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The total tax paid on the item";

    public override string Version { get; } = "1.0.0";

    public override string? CurrentValue(SearchResult item)
    {
        if (item.SoldItem != null)
        {
            return (item.SoldItem.TotalIncTax - item.SoldItem.Total).ToString();
        }

        if (item.SaleSummaryItem != null)
        {
            return item.SaleSummaryItem.TaxPaid.ToString();
        }

        return null;
    }
}
