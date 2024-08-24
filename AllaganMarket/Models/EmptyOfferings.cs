using System.Collections.Generic;

using Dalamud.Game.Network.Structures;

namespace AllaganMarket.Models;

public class EmptyOfferings : IMarketBoardCurrentOfferings
{
    public IReadOnlyList<IMarketBoardItemListing> ItemListings { get; set; } = new List<IMarketBoardItemListing>();

    public int RequestId { get; set; } = 0;
}
