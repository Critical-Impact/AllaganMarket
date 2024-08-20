using System.Collections.Generic;

using AllaganMarket.Models;

namespace AllaganMarket.Extensions;

public static class SoldItemExtensions
{
    public static ulong GetOwnerId(this SoldItem soldItem, Dictionary<ulong, ulong?> characterRetainerMap)
    {
        return characterRetainerMap.GetValueOrDefault(soldItem.RetainerId) ?? 0;
    }
}
