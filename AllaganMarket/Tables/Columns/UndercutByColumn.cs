using System.Globalization;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using Dalamud.Game.Text;

using ImGuiNET;

namespace AllaganMarket.Tables.Columns;

public class UndercutByColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : IntegerColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "UndercutBy";

    public override string Name { get; set; } = "Undercut by";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "How much the item was undercut by.";

    public override string Version { get; } = "1.0.1";

    public override string? CurrentValue(SearchResult item)
    {
        NumberFormatInfo numberFormatInfo = new CultureInfo("en-US", false).NumberFormat;
        numberFormatInfo.NumberDecimalDigits = 0;
        if (item.SaleItem?.UndercutBy != null)
        {
            return SeIconChar.Gil.ToIconString() + item.SaleItem?.UndercutBy?.ToString("n", numberFormatInfo);
        } else { return string.Empty; }
    }
 }
