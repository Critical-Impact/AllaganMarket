using System.Collections.Generic;

using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;

using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;

using DalaMock.Host.Mediator;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using ImGuiService = AllaganLib.Interface.Services.ImGuiService;

namespace AllaganMarket.Tables.Columns;

public class NameColumn(
    ImGuiService imGuiService,
    StringColumnFilter stringColumnFilter,
    ExcelSheet<Item> itemSheet,
    ExcelSheet<World> worldSheet,
    ICharacterMonitorService characterMonitorService)
    : StringColumn<SearchResultConfiguration, SearchResult, MessageBase>(imGuiService, stringColumnFilter)
{
    private readonly Dictionary<SaleSummaryKey, string> formattedNames = [];

    public override string DefaultValue { get; set; } = string.Empty;

    public override string Key { get; set; } = "Name";

    public override string Name { get; set; } = "Name";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = string.Empty;

    public override string HelpText { get; set; } = "The name of the item";

    public override string Version { get; } = "1.0.0";

    public string GetFormattedSaleSummaryName(SaleSummaryItem saleSummaryItem)
    {
        if (!saleSummaryItem.Grouping.IsGrouped && saleSummaryItem.ItemId != null)
        {
            return itemSheet.GetRowOrDefault(saleSummaryItem.ItemId.Value)?.Name.ExtractText() ??
                   "Unknown Item";
        }

        if (!this.formattedNames.ContainsKey(saleSummaryItem.Grouping))
        {
            List<string> pieces = [];
            if (saleSummaryItem.Grouping.WorldId != null)
            {
                var world = worldSheet.GetRowOrDefault(saleSummaryItem.Grouping.WorldId.Value);
                if (world != null)
                {
                    pieces.Add(world.Value.Name.ExtractText());
                }
            }

            if (saleSummaryItem.Grouping.OwnerId != null)
            {
                var character = characterMonitorService.GetCharacterById(saleSummaryItem.Grouping.OwnerId.Value);
                if (character != null)
                {
                    pieces.Add(character.Name);
                }
            }

            if (saleSummaryItem.Grouping.RetainerId != null)
            {
                var character =
                    characterMonitorService.GetCharacterById(saleSummaryItem.Grouping.RetainerId.Value);
                if (character != null)
                {
                    pieces.Add(character.Name);
                }
            }

            if (saleSummaryItem.Grouping.ItemId != null)
            {
                var item = itemSheet.GetRowOrDefault(saleSummaryItem.Grouping.ItemId.Value);
                if (item != null)
                {
                    pieces.Add(item.Value.Name.ExtractText());
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

            return itemSheet.GetRowOrDefault(item.SaleItem.ItemId)?.Name.ToString() ?? string.Empty;
        }
        else if (item.SoldItem != null)
        {
            return itemSheet.GetRowOrDefault(item.SoldItem.ItemId)?.Name.ToString() ?? string.Empty;
        }
        else if (item.SaleSummaryItem != null)
        {
            return this.GetFormattedSaleSummaryName(item.SaleSummaryItem);
        }

        return string.Empty;
    }
}
