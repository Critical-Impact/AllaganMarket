using System.Globalization;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;
using Dalamud.Game.Text;

using ImGuiNET;

namespace AllaganMarket.Tables.Columns;

public class UnitPriceColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "UnitPrice";

    public override string Name { get; set; } = "Unit Price";

    public override string? RenderName { get; set; }

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The unit price of the item being sold.";

    public override string Version { get; } = "1.0.1";

    public override string? CurrentValue(SearchResult item)
    {
        NumberFormatInfo numberFormatInfo = new CultureInfo("en-US", false).NumberFormat;
        numberFormatInfo.NumberDecimalDigits = 0;
        return SeIconChar.Gil.ToIconString() + (item.SoldItem?.UnitPrice.ToString("n", numberFormatInfo) ?? item.SaleItem?.UnitPrice.ToString("n", numberFormatInfo) ?? null);
    }
}
