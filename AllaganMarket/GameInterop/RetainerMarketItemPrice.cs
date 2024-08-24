using System;
using System.IO;

using AllaganMarket.Interfaces;

namespace AllaganMarket.GameInterop;

/// <summary>
/// This represents the unit price of a particular item a retainer has for sale.
/// </summary>
public struct RetainerMarketItemPrice(uint slot, uint unitPrice) : IDebuggable
{
    public uint Sequence { get; private set; }

    public uint ContainerId { get; private set; }

    public uint Slot { get; private set; } = slot;

    public uint Unknown { get; private set; }

    public uint UnitPrice { get; private set; } = unitPrice;

    /// <summary>
    /// Read a packet off the wire.
    /// </summary>
    /// <param name="dataPtr">Packet data.</param>
    /// <returns>An object representing the data read.</returns>
    public static unsafe RetainerMarketItemPrice Read(IntPtr dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 640L);
        using var reader = new BinaryReader(stream);

        var output = default(RetainerMarketItemPrice);

        output.Sequence = reader.ReadUInt32();
        output.ContainerId = reader.ReadUInt32();
        output.Slot = reader.ReadUInt32();
        output.Unknown = reader.ReadUInt32();
        output.UnitPrice = reader.ReadUInt32();
        return output;
    }

    public readonly string AsDebugString()
    {
        return
            $"Slot: {this.Slot}, Unit Price: {this.UnitPrice}, Sequence: {this.Sequence}, Container ID: {this.ContainerId}";
    }
}
