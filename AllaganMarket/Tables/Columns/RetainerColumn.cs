using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Services;

using AllaganMarket.Services.Interfaces;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarket.Grids.Columns;

public class RetainerColumn : StringColumn<SearchResultConfiguration, SearchResult, MessageBase>
{
    private readonly ICharacterMonitorService characterMonitorService;

    public RetainerColumn(ICharacterMonitorService characterMonitorService, ImGuiService imGuiService, StringColumnFilter stringColumnFilter) : base(imGuiService, stringColumnFilter)
    {
        this.characterMonitorService = characterMonitorService;
    }

    public override string DefaultValue { get; set; } = "";

    public override string Key { get; set; } = "RetainerName";

    public override string Name { get; set; } = "Retainer";

    public override string? RenderName { get; set; } = null;

    public override int Width { get; set; } = 100;

    public override bool HideFilter { get; set; } = false;

    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.None;

    public override string EmptyText { get; set; } = "";

    public override string? CurrentValue(SearchResult item)
    {
        var retainerId = item.SaleItem?.RetainerId ?? item.SoldItem?.RetainerId;
        if (retainerId != null)
        {
            return this.characterMonitorService.GetCharacterById(retainerId.Value)?.Name;
        }

        return "";
    }

    public override string HelpText { get; set; } = "The name of the retainer";

    public override string Version { get; } = "1.0.0";
}
