using System.Globalization;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Tables.Columns;

public class AverageSalePriceColumn(NumberFormatInfo gilFormat, ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : GilColumn(gilFormat, imGuiService, stringColumnFilter)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "AverageSalePrice";

    public override string Name { get; set; } = "Avg. Sale Price";

    public override string HelpText { get; set; } = "The average sale price of this item.";

    public override string Version { get; } = "1.0.0";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 80;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string? CurrentValue(SearchResult item)
    {
        if (item.SaleSummaryItem != null)
        {
            return ((int)(item.SaleSummaryItem.Earned / item.SaleSummaryItem.Quantity)).ToString();
        }

        return null;
    }
}
