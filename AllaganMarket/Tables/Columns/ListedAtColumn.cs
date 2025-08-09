using System;
using System.Globalization;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using DalaMock.Host.Mediator;

using Dalamud.Bindings.ImGui;

namespace AllaganMarket.Tables.Columns;

public class ListedAtColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter)
    : DateTimeColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    public override string? DefaultValue { get; set; } = null;

    public override string Key { get; set; } = "ListedAt";

    public override string Name { get; set; } = "Listed At";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } =
        "The date the item was listed on the market.";

    public override string Version { get; } = "1.0.0";


    public override DateTime? CurrentDateValue(SearchResult item)
    {
        if (item.SaleItem != null && item.SaleItem.IsEmpty())
        {
            return null;
        }

        return item.SaleItem?.ListedAt ?? null;
    }
}
