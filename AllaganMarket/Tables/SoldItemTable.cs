using System;
using System.Collections.Generic;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid;

using AllaganMarket.Filtering;
using AllaganMarket.Grids.Columns;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using static ImGuiNET.ImGuiTableFlags;

namespace AllaganMarket.Grids;

public class SoldItemTable : RenderTable<SearchResultConfiguration, SearchResult, MessageBase>
{
    private readonly ICommandManager commandManager;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly SaleFilter saleFilter;

    public SoldItemTable(CsvLoaderService csvLoaderService, ICommandManager commandManager, ExcelSheet<Item> itemSheet, NameColumn nameColumn, QuantityColumn quantityColumn, UnitPriceColumn unitPriceColumn, SearchResultConfiguration searchResultConfiguration, SaleFilter saleFilter, WorldColumn worldColumn, RetainerColumn retainerColumn, SoldAtColumn soldAtColumn)
        : base(csvLoaderService, searchResultConfiguration,[nameColumn, quantityColumn, unitPriceColumn, worldColumn, retainerColumn, soldAtColumn], None | Resizable | Hideable | Sortable | RowBg | BordersInnerH | BordersOuterH | BordersInnerV | BordersOuterV | BordersH | BordersV | BordersInner | BordersOuter | Borders | ScrollX | ScrollY, "Sold Item Table", "SoldItemTable")
    {
        this.commandManager = commandManager;
        this.itemSheet = itemSheet;
        this.saleFilter = saleFilter;
        this.ShowFilterRow = true;
    }
    
    private List<MessageBase> OpenMenu(SearchResult arg)
    {
        var messages = new List<MessageBase>();

        if (ImGui.Selectable("More Information"))
        {
            messages.Add(new OpenMoreInformation(arg.SoldItem!.ItemId));
        }

        if (ImGui.Selectable("Delete Sold Item"))
        {
            messages.Add(new DeleteSoldItem(arg.SoldItem!));
        }

        return messages;
    }

    public override List<SearchResult> GetItems()
    {
        return this.saleFilter.GetSoldResults();
    }
}
