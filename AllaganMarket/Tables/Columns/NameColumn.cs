using System.Collections.Generic;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;

using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;

using DalaMock.Host.Mediator;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using ImGuiService = AllaganLib.Interface.Services.ImGuiService;

namespace AllaganMarket.Grids.Columns;

public class NameColumn : StringColumn<SearchResultConfiguration,SearchResult, MessageBase>
{
    private readonly ExcelSheet<Item> itemSheet;
    private readonly ExcelSheet<World> worldSheet;
    private readonly ICharacterMonitorService characterMonitorService;

    public NameColumn(ImGuiService imGuiService, StringColumnFilter stringColumnFilter, ExcelSheet<Item> itemSheet, ExcelSheet<World> worldSheet, ICharacterMonitorService characterMonitorService) : base(imGuiService, stringColumnFilter)
    {
        this.itemSheet = itemSheet;
        this.worldSheet = worldSheet;
        this.characterMonitorService = characterMonitorService;
    }

    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "Name";

    public override string Name { get; set; } = "Name";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    private Dictionary<SaleSummaryKey, string> formattedNames = new();

    public string GetFormattedSaleSummaryName(SaleSummaryItem saleSummaryItem)
    {
        if (!saleSummaryItem.Grouping.IsGrouped && saleSummaryItem.ItemId != null)
        {
            return this.itemSheet.GetRow(saleSummaryItem.ItemId.Value)?.Name.AsReadOnly().ExtractText() ?? "Unknown Item";
        }

        if (!this.formattedNames.ContainsKey(saleSummaryItem.Grouping))
        {
            List<string> pieces = new List<string>();
            if (saleSummaryItem.Grouping.WorldId != null)
            {
                var world = this.worldSheet.GetRow(saleSummaryItem.Grouping.WorldId.Value);
                if (world != null)
                {
                    pieces.Add(world.Name.AsReadOnly().ExtractText());
                }
            }

            if (saleSummaryItem.Grouping.OwnerId != null)
            {
                var character = this.characterMonitorService.GetCharacterById(saleSummaryItem.Grouping.OwnerId.Value);
                if (character != null)
                {
                    pieces.Add(character.Name);
                }
            }

            if (saleSummaryItem.Grouping.RetainerId != null)
            {
                var character = this.characterMonitorService.GetCharacterById(saleSummaryItem.Grouping.RetainerId.Value);
                if (character != null)
                {
                    pieces.Add(character.Name);
                }   
            }

            if (saleSummaryItem.Grouping.ItemId != null)
            {
                var item = this.itemSheet.GetRow(saleSummaryItem.Grouping.ItemId.Value);
                if (item != null)
                {
                    pieces.Add(item.Name.AsReadOnly().ExtractText());
                }
            }

            if (saleSummaryItem.Grouping.IsHq != null)
            {
                pieces.Add(saleSummaryItem.Grouping.IsHq.Value ? "HQ" : "NQ");
            }

            this.formattedNames[saleSummaryItem.SaleSummaryKey] = string.Join(" - ", pieces);
        }

        return this.formattedNames[saleSummaryItem.SaleSummaryKey];
    }

    public override string? CurrentValue(SearchResult item)
    {
        if (item.SaleItem != null)
        {
            if (item.SaleItem.ItemId == 0)
            {
                return "Empty Slot";
            }

            return this.itemSheet.GetRow(item.SaleItem.ItemId)?.Name.AsReadOnly().ToString() ?? string.Empty;
        }
        else if (item.SoldItem != null)
        {
            return this.itemSheet.GetRow(item.SoldItem.ItemId)?.Name.AsReadOnly().ToString() ?? string.Empty;
        }
        else if (item.SaleSummaryItem != null)
        {
            return this.GetFormattedSaleSummaryName(item.SaleSummaryItem);
        }

        return string.Empty;
    }

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The name of the item";

    public override string Version { get; } = "1.0.0";
}
