using AllaganMarket.Services;

using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AllaganMarketMock;

public class MockGameInterfaceService : IGameInterfaceService
{
    private readonly IPluginLog pluginLog;
    private readonly ExcelSheet<Recipe> recipeSheet;
    private Dictionary<uint, List<uint>>? recipes;

    public MockGameInterfaceService(IPluginLog pluginLog, ExcelSheet<Recipe> recipeSheet)
    {
        this.pluginLog = pluginLog;
        this.recipeSheet = recipeSheet;
    }

    public Dictionary<uint, List<uint>> GetRecipes()
    {
        return this.recipes ??= this.recipeSheet.GroupBy(c => c.ItemResult.RowId).ToDictionary(c => c.Key, c => c.Select(c => c.RowId).ToList());
    }

    public List<uint>? GetRecipes(uint itemId)
    {
        return this.GetRecipes().TryGetValue(itemId, out var result) ? result : null;
    }

    public unsafe bool OpenCraftingLog(uint itemId, uint? recipeId = null)
    {
        this.pluginLog.Verbose($"Would have opened crafting log for item with ID {itemId}");

        return true;
    }
}
