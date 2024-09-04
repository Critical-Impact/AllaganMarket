using System.Globalization;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Tables.Columns;

public class QuantityColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "quantity";

    public override string Name { get; set; } = "Quantity";

    public override string? RenderName { get; set; }

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; }

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The quantity of item being sold.";

    public override string Version => "1.0.1";

    public override string? CurrentValue(SearchResult item)
    {
        NumberFormatInfo numberFormatInfo = new CultureInfo("en-US", false).NumberFormat;
        numberFormatInfo.NumberDecimalDigits = 0;
        if (item.SaleItem != null || item.SoldItem != null)
        {
            return item.SaleItem?.Quantity.ToString("n", numberFormatInfo) ?? item.SoldItem?.Quantity.ToString("n", numberFormatInfo);
        }

        if (item.SaleSummaryItem != null)
        {
            return item.SaleSummaryItem.Quantity.ToString("n", numberFormatInfo);
        }

        return string.Empty;
    }
}
