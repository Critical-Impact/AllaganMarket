namespace AllaganMarket.Models;

using Interfaces;

public class RetainerMarketListEvent : IDebuggable
{
    public RetainerMarketListEvent(RetainerMarketListEventType eventType, short slot)
    {
        this.EventType = eventType;
        this.Slot = slot;
    }

    public RetainerMarketListEventType EventType { get; }

    public short Slot { get; set; }

    public SaleItem? SaleItem { get; set; }

    public string AsDebugString()
    {
        return
            $"Event Type: {this.EventType}, Slot: {this.Slot}, Sale Item: {this.SaleItem?.AsDebugString() ?? "No Sale Item"}";
    }
}