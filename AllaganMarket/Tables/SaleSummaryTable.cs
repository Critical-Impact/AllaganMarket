using System.Collections.Generic;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid;

using AllaganMarket.Models;
using AllaganMarket.Tables.Columns;

using DalaMock.Host.Mediator;

using static ImGuiNET.ImGuiTableFlags;

namespace AllaganMarket.Tables;

public class SaleSummaryTable : RenderTable<SearchResultConfiguration, SearchResult, MessageBase>
{
    public SaleSummaryTable(
        CsvLoaderService csvLoaderService,
        SaleSummary saleSummary,
        NameColumn nameColumn,
        QuantityColumn quantityColumn,
        SearchResultConfiguration searchResultConfiguration,
        WorldColumn worldColumn,
        RetainerColumn retainerColumn,
        IsHQColumn isHqColumn,
        AverageSalePriceColumn averageSalePriceColumn,
        TotalColumn totalColumn)
        : base(
            csvLoaderService,
            searchResultConfiguration,
            [nameColumn, quantityColumn, worldColumn, retainerColumn, isHqColumn, averageSalePriceColumn, totalColumn],
            None | Resizable | Hideable | Sortable | RowBg | BordersInnerH | BordersOuterH | BordersInnerV | BordersOuterV | BordersH | BordersV | BordersInner | BordersOuter | Borders | ScrollX | ScrollY,
            "Sold Item Table",
            "SoldItemTable")
    {
        this.SaleSummary = saleSummary;
        this.ShowFilterRow = true;
    }

    public SaleSummary SaleSummary { get; }

    public override List<SearchResult> GetItems()
    {
        return this.SaleSummary.GetSearchResults();
    }
}
