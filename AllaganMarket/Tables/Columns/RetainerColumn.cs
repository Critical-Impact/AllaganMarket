using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using AllaganMarket.Services.Interfaces;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Tables.Columns;

public class RetainerColumn(
    ICharacterMonitorService characterMonitorService,
    ImGuiService imGuiService,
    StringColumnFilter stringColumnFilter)
    : StringColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "RetainerName";

    public override string Name { get; set; } = "Retainer";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The name of the retainer";

    public override string Version { get; } = "1.0.0";

    public override string? CurrentValue(SearchResult item)
    {
        var retainerId = item.SaleItem?.RetainerId ?? item.SoldItem?.RetainerId;
        if (retainerId != null)
        {
            return characterMonitorService.GetCharacterById(retainerId.Value)?.Name;
        }

        if (item.SaleSummaryItem != null)
        {
            if (item.SaleSummaryItem.Grouping is { IsGrouped: true, RetainerId: not null })
            {
                return characterMonitorService.GetCharacterById(item.SaleSummaryItem.Grouping.RetainerId.Value)
                                              ?.Name ?? string.Empty;
            }

            if (item.SaleSummaryItem.Grouping.IsGrouped == false && item.SaleSummaryItem.RetainerId != null)
            {
                return characterMonitorService.GetCharacterById(item.SaleSummaryItem.RetainerId.Value)?.Name ??
                       string.Empty;
            }

            return "N/A";
        }

        return string.Empty;
    }
}
