using System;
using System.Collections.Generic;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid;
using AllaganMarket.Filtering;
using AllaganMarket.Services;
using AllaganMarket.Tables.Columns;
using DalaMock.Host.Mediator;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

using static Dalamud.Bindings.ImGui.ImGuiTableFlags;

namespace AllaganMarket.Tables;

public class SoldItemTable : RenderTable<SearchResultConfiguration, SearchResult, MessageBase>
{
    private readonly ICommandManager commandManager;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly SaleFilter saleFilter;
    private readonly ImGuiMenus imGuiMenus;

    public SoldItemTable(
        CsvLoaderService csvLoaderService,
        ICommandManager commandManager,
        ExcelSheet<Item> itemSheet,
        NameColumn nameColumn,
        QuantityColumn quantityColumn,
        UnitPriceColumn unitPriceColumn,
        SearchResultConfiguration searchResultConfiguration,
        SaleFilter saleFilter,
        WorldColumn worldColumn,
        RetainerColumn retainerColumn,
        SoldAtColumn soldAtColumn,
        ItemIconColumn iconColumn,
        ImGuiMenus imGuiMenus)
        : base(
            csvLoaderService,
            searchResultConfiguration,
            [iconColumn, nameColumn, quantityColumn, unitPriceColumn, worldColumn, retainerColumn, soldAtColumn],
            None | Resizable | Hideable | Sortable | RowBg | BordersInnerH | BordersOuterH | BordersInnerV | BordersOuterV | BordersH | BordersV | BordersInner | BordersOuter | Borders | ScrollX | ScrollY,
            "Sold Item Table",
            "SoldItemTable")
    {
        this.commandManager = commandManager;
        this.itemSheet = itemSheet;
        this.saleFilter = saleFilter;
        this.imGuiMenus = imGuiMenus;
        this.ShowFilterRow = true;
    }

    public override Func<SearchResult, List<MessageBase>>? RightClickFunc => this.OpenMenu;

    public override List<SearchResult> GetItems()
    {
        return this.saleFilter.GetSoldResults();
    }

    private List<MessageBase> OpenMenu(SearchResult arg)
    {
        var messages = new List<MessageBase>();

        var result = this.imGuiMenus.DrawSoldItemMenu(arg.SoldItem!);
        if (result != null)
        {
            messages.Add(result);
        }

        return messages;
    }
}
