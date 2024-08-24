using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AllaganMarket.Tables.Columns;

public class WorldColumn(ExcelSheet<World> worldSheet, ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : StringColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "World";

    public override string Name { get; set; } = "World";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The world";

    public override string Version { get; } = "1.0.0";

    public override string? CurrentValue(SearchResult item)
    {
        var worldId = item.SaleItem?.WorldId ?? item.SoldItem?.WorldId;
        if (worldId != null && worldId != 0)
        {
            return worldSheet.GetRow(worldId.Value)?.Name.AsReadOnly().ExtractText() ?? string.Empty;
        }

        if (item.SaleSummaryItem != null)
        {
            if (item.SaleSummaryItem.Grouping is { IsGrouped: true, WorldId: not null })
            {
                return worldSheet.GetRow(item.SaleSummaryItem.Grouping.WorldId.Value)?.Name.AsReadOnly()
                                 .ExtractText() ?? string.Empty;
            }

            if (item.SaleSummaryItem.Grouping.IsGrouped == false && item.SaleSummaryItem.WorldId != null)
            {
                return worldSheet.GetRow(item.SaleSummaryItem.WorldId.Value)?.Name.AsReadOnly().ExtractText() ??
                       string.Empty;
            }

            return "N/A";
        }

        return string.Empty;
    }
}
