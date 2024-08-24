using AllaganMarket.Interfaces;

namespace AllaganMarket.Models;

public class RetainerMarketListEvent(RetainerMarketListEventType eventType, short slot) : IDebuggable
{
    public RetainerMarketListEventType EventType { get; } = eventType;

    public short Slot { get; set; } = slot;

    public SaleItem? SaleItem { get; set; }

    public string AsDebugString()
    {
        return
            $"Event Type: {this.EventType}, Slot: {this.Slot}, Sale Item: {this.SaleItem?.AsDebugString() ?? "No Sale Item"}";
    }
}
