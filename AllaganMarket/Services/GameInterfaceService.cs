using System;
using System.Collections.Generic;
using System.Linq;

using AllaganMarket.Mediator;

using DalaMock.Host.Mediator;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AllaganMarket.Services;

public interface IGameInterfaceService
{
    Dictionary<uint, List<uint>> GetRecipes();

    List<uint>? GetRecipes(uint itemId);

    unsafe bool OpenCraftingLog(uint itemId, uint? recipeId = null);
}

public class GameInterfaceService : IGameInterfaceService
{
    private readonly ICondition condition;
    private readonly ExcelSheet<Recipe> recipeSheet;
    private Dictionary<uint, List<uint>>? recipes;

    public GameInterfaceService(ICondition condition, ExcelSheet<Recipe> recipeSheet)
    {
        this.condition = condition;
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
        if (this.condition[ConditionFlag.Crafting] || this.condition[ConditionFlag.ExecutingCraftingAction])
        {
            if (!this.condition[ConditionFlag.PreparingToCraft])
            {
                return false;
            }
        }

        itemId %= 500_000;
        var recipes = this.GetRecipes(itemId);
        if (recipes != null)
        {
            if (recipeId == null)
            {
                AgentRecipeNote.Instance()->SearchRecipeByItemId(itemId);
            }
            else if(recipes.Any(c => c == recipeId.Value))
            {
                AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipeId.Value);
            }
        }

        return true;
    }
}
