using System.Collections.Generic;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid;

using AllaganMarket.Grids.Columns;
using AllaganMarket.Models;

using DalaMock.Host.Mediator;

using static ImGuiNET.ImGuiTableFlags;

namespace AllaganMarket.Grids;

public class SaleSummaryTable : RenderTable<SearchResultConfiguration, SearchResult, MessageBase>
{
    public SaleSummary SaleSummary { get; }

    public SaleSummaryTable(CsvLoaderService csvLoaderService, SaleSummary saleSummary, NameColumn nameColumn, QuantityColumn quantityColumn, SearchResultConfiguration searchResultConfiguration, WorldColumn worldColumn, RetainerColumn retainerColumn, IsHQColumn isHqColumn, TaxColumn taxColumn, TotalColumn totalColumn)
        : base(csvLoaderService, searchResultConfiguration,[nameColumn, quantityColumn, worldColumn, retainerColumn, isHqColumn, taxColumn, totalColumn], None | Resizable | Hideable | Sortable | RowBg | BordersInnerH | BordersOuterH | BordersInnerV | BordersOuterV | BordersH | BordersV | BordersInner | BordersOuter | Borders | ScrollX | ScrollY, "Sold Item Table", "SoldItemTable")
    {
        this.SaleSummary = saleSummary;
        this.ShowFilterRow = true;
    }

    public override List<SearchResult> GetItems()
    {
        return this.SaleSummary.GetSearchResults();
    }
}
