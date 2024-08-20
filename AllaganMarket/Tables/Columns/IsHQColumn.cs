using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

namespace AllaganMarket.Grids.Columns;

public class IsHQColumn : BooleanColumn<SearchResultConfiguration, SearchResult, MessageBase>
{
    public IsHQColumn(ImGuiService imGuiService)
        : base(imGuiService)
    {
    }

    public override string DefaultValue { get; set; } = "";

    public override string Key { get; set; } = "IsHQ";

    public override string Name { get; set; } = "Is HQ?";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 70;

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

    public override string HelpText { get; set; }

    public override string Version { get; }
}
