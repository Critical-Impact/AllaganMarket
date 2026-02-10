using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using Dalamud.Bindings.ImGui;

namespace AllaganMarket.Tables.Columns;

public class IsHQColumn(ImGuiService imGuiService)
    : BooleanColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService)
{
    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "IsHQ";

    public override string Name { get; set; } = "Is HQ?";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 70;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string HelpText { get; set; } = "Is the item high quality?";

    public override string Version => "1.0.0";

    public override string? CurrentValue(SearchResult item)
    {
        if (item.SaleItem != null)
        {
            return item.SaleItem.IsHq ? "true" : "false";
        }

        if (item.SoldItem != null)
        {
            return item.SoldItem.IsHq ? "true" : "false";
        }

        if (item.SaleSummaryItem != null)
        {
            if (item.SaleSummaryItem.Grouping is { IsGrouped: true, IsHq: not null })
            {
                return item.SaleSummaryItem.Grouping.IsHq.Value ? "true" : "false";
            }

            if (item.SaleSummaryItem.Grouping.IsGrouped == false && item.SaleSummaryItem.IsHq != null)
            {
                return item.SaleSummaryItem.IsHq.Value ? "true" : "false";
            }

            return "N/A";
        }

        return null;
    }
}
