using AllaganLib.Interface.Grid;

using AllaganMarket.Grids;
using AllaganMarket.Models;

using DalaMock.Host.Mediator;

namespace AllaganMarket;

public record DeleteSoldItem(SoldItem soldItem) : MessageBase;
public record OpenMoreInformation(uint itemId) : MessageBase;
