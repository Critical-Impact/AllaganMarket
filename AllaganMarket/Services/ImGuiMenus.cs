using System.Collections.Generic;
using System.Linq;

using AllaganMarket.Mediator;
using AllaganMarket.Models;
using AllaganMarket.Settings;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using Dalamud.Bindings.ImGui;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AllaganMarket.Services;

public class ImGuiMenus
{
    private readonly UndercutService undercutService;
    private readonly Configuration configuration;
    private readonly IGameInterfaceService gameInterfaceService;
    private readonly ExcelSheet<Recipe> recipeSheet;
    private readonly HashSet<uint> recipeItemIds;

    public ImGuiMenus(UndercutService undercutService, Configuration configuration, IDataManager dataManager, IGameInterfaceService gameInterfaceService)
    {
        this.undercutService = undercutService;
        this.configuration = configuration;
        this.gameInterfaceService = gameInterfaceService;
        this.recipeSheet = dataManager.GetExcelSheet<Recipe>();
        this.recipeItemIds = this.recipeSheet.Select(c => c.ItemResult.RowId).Distinct().ToHashSet();
    }

    public MessageBase? DrawSoldItemMenu(SoldItem soldItem)
    {
        if (ImGui.Selectable("More Information"))
        {
            return new OpenMoreInformation(soldItem.ItemId);
        }

        if (this.recipeItemIds.Contains(soldItem.ItemId) && ImGui.Selectable("Open Crafting Log"))
        {
            this.gameInterfaceService.OpenCraftingLog(soldItem.ItemId);
        }

        if (ImGui.Selectable("Delete Sold Item"))
        {
            return new DeleteSoldItem(soldItem);
        }

        return null;
    }

    public MessageBase? DrawSaleItemMenu(SaleItem saleItem)
    {
        if (ImGui.Selectable("More Information"))
        {
            return new OpenMoreInformation(saleItem.ItemId);
        }

        if (this.recipeItemIds.Contains(saleItem.ItemId) && ImGui.Selectable("Open Crafting Log"))
        {
            this.gameInterfaceService.OpenCraftingLog(saleItem.ItemId);
        }

        if (ImGui.Selectable("Mark as Updated"))
        {
            this.undercutService.InsertFakeMarketPriceCache(saleItem);
        }

        ImGui.NewLine();
        ImGui.Text("Undercut Settings:");
        ImGui.Separator();


        if (ImGui.Selectable("Use Default", this.configuration.GetUndercutComparison(saleItem.ItemId) == null))
        {
            this.configuration.RemoveUndercutComparison(saleItem.ItemId);
        }

        if (ImGui.Selectable(
                "Any",
                this.configuration.GetUndercutComparison(saleItem.ItemId) == UndercutComparison.Any))
        {
            this.configuration.SetUndercutComparison(saleItem.ItemId, UndercutComparison.Any);
        }

        if (ImGui.Selectable(
                "NQ Only",
                this.configuration.GetUndercutComparison(saleItem.ItemId) == UndercutComparison.NqOnly))
        {
            this.configuration.SetUndercutComparison(saleItem.ItemId, UndercutComparison.NqOnly);
        }

        if (ImGui.Selectable(
                "HQ Only",
                this.configuration.GetUndercutComparison(saleItem.ItemId) == UndercutComparison.HqOnly))
        {
            this.configuration.SetUndercutComparison(saleItem.ItemId, UndercutComparison.HqOnly);
        }

        if (ImGui.Selectable(
                "Matching Quality",
                this.configuration.GetUndercutComparison(saleItem.ItemId) == UndercutComparison.MatchingQuality))
        {
            this.configuration.SetUndercutComparison(saleItem.ItemId, UndercutComparison.MatchingQuality);
        }

        return null;
    }
}
