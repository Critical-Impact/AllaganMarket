using AllaganMarket.Models;

namespace AllaganMarket.Tables;

public class SearchResult
{
    public SaleItem? SaleItem { get; set; }

    public SoldItem? SoldItem { get; set; }

    public SaleSummaryItem? SaleSummaryItem { get; set; }
}
