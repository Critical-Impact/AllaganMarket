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

namespace AllaganMarket.Grids;

public class SaleItemTable : RenderTable<SearchResultConfiguration, SearchResult, MessageBase>
{
    private readonly ICommandManager commandManager;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly SaleFilter saleFilter;

    public SaleItemTable(CsvLoaderService csvLoaderService, ICommandManager commandManager, ExcelSheet<Item> itemSheet, NameColumn nameColumn, QuantityColumn quantityColumn, UnitPriceColumn unitPriceColumn, SearchResultConfiguration searchResultConfiguration, SaleFilter saleFilter, WorldColumn worldColumn, RetainerColumn retainerColumn, UndercutByColumn undercutByColumn, ListedAtColumn listedAtColumn, UpdatedAtColumn updatedAtColumn)
        : base(
            csvLoaderService,
            searchResultConfiguration,
            [
                nameColumn,
                quantityColumn,
                unitPriceColumn,
                worldColumn,
                retainerColumn,
                undercutByColumn,
                listedAtColumn,
                updatedAtColumn
            ],
            ImGuiTableFlags.None | ImGuiTableFlags.Resizable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable |
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersOuterH |
            ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.BordersH |
            ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersInner | ImGuiTableFlags.BordersOuter |
            ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY,
            "Sale Item Table",
            "SaleItemTable")
    {
        this.commandManager = commandManager;
        this.itemSheet = itemSheet;
        this.saleFilter = saleFilter;
        this.ShowFilterRow = true;
    }

    /// <inheritdoc/>
    public override Func<SearchResult, List<MessageBase>>? RightClickFunc => this.OpenMenu;

    private List<MessageBase> OpenMenu(SearchResult arg)
    {
        var messages = new List<MessageBase>();

        if (ImGui.Selectable("More Information"))
        {
            messages.Add(new OpenMoreInformation(arg.SaleItem!.ItemId));
        }

        if (ImGui.Selectable("Mark as Updated"))
        {
            arg.SaleItem!.UpdatedAt = DateTime.Now;
        }

        return messages;
    }

    public override List<SearchResult> GetItems()
    {
        return this.saleFilter.GetSaleResults();
    }
}
