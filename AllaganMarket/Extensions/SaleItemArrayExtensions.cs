using System;

using AllaganMarket.Models;

namespace AllaganMarket.Extensions;

public static class SaleItemArrayExtensions
{
    public static SaleItem[] FillList(this SaleItem?[] saleItems, ulong retainerId)
    {
        if (saleItems.Length != 20)
        {
            Array.Resize(ref saleItems, 20);
        }


        for (var index = 0; index < 20; index++)
        {
            var saleItem = saleItems[index];
            if (saleItem == null)
            {
                saleItem = new SaleItem(retainerId);
                saleItems[index] = saleItem;
            }
        }

        return saleItems!;
    }
}
