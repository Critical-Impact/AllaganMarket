using System;
using System.Collections.Generic;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid;

using AllaganMarket.Filtering;
using AllaganMarket.Mediator;
using AllaganMarket.Services;
using AllaganMarket.Tables.Columns;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AllaganMarket.Tables;

public class SaleItemTable : RenderTable<SearchResultConfiguration, SearchResult, MessageBase>
{
    private readonly ICommandManager commandManager;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly SaleFilter saleFilter;
    private readonly UndercutService undercutService;
    private readonly ImGuiMenus imGuiMenus;

    public SaleItemTable(
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
        UndercutByColumn undercutByColumn,
        ListedAtColumn listedAtColumn,
        UpdatedAtColumn updatedAtColumn,
        UndercutService undercutService,
        ItemIconColumn itemIconColumn,
        ImGuiMenus imGuiMenus)
        : base(
            csvLoaderService,
            searchResultConfiguration,
            [
                itemIconColumn,
                nameColumn,
                quantityColumn,
                unitPriceColumn,
                worldColumn,
                retainerColumn,
                undercutByColumn,
                listedAtColumn,
                updatedAtColumn
            ],
            ImGuiTableFlags.None | ImGuiTableFlags.Resizable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersInner | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY,
            "Sale Item Table",
            "SaleItemTable")
    {
        this.commandManager = commandManager;
        this.itemSheet = itemSheet;
        this.saleFilter = saleFilter;
        this.undercutService = undercutService;
        this.imGuiMenus = imGuiMenus;
        this.ShowFilterRow = true;
    }

    /// <inheritdoc/>
    public override Func<SearchResult, List<MessageBase>>? RightClickFunc => this.OpenMenu;

    public override List<SearchResult> GetItems()
    {
        return this.saleFilter.GetSaleResults();
    }

    private List<MessageBase> OpenMenu(SearchResult arg)
    {
        var messages = new List<MessageBase>();

        var result = this.imGuiMenus.DrawSaleItemMenu(arg.SaleItem!);
        if (result != null)
        {
            messages.Add(result);
        }

        return messages;
    }
}
