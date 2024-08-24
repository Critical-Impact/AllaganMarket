using System;

namespace AllaganMarket.Models;

[Flags]
public enum SaleSummaryGroup
{
    None = 0,
    Item = 1,
    World = 2,
    IsHq = 4,
    Owner = 8,
    Retainer = 16,
}
