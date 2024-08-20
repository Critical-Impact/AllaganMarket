using AllaganMarket.Models;

namespace AllaganMarket.Extensions;

public static class SaleItemArrayExtensions
{
    public static SaleItem[] FillList(this SaleItem?[] saleItems, ulong retainerId)
    {
        for (var index = 0; index < saleItems.Length; index++)
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
