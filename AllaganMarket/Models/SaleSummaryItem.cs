namespace AllaganMarket.Models;

public class SaleSummaryItem(SaleSummaryKey saleSummaryKey, SaleSummaryKey grouping)
{
    public SaleSummaryKey SaleSummaryKey { get; set; } = saleSummaryKey;

    public SaleSummaryKey Grouping { get; set; } = grouping;

    public uint? ItemId { get; set; }

    public uint? WorldId { get; set; }

    public bool? IsHq { get; set; }

    public ulong? OwnerId { get; set; }

    public ulong? RetainerId { get; set; }

    public uint Quantity { get; set; }

    public uint Earned { get; set; }

    public uint TaxPaid { get; set; }
}
