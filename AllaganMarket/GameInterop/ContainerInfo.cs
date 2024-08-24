using System;
using System.IO;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace AllaganMarket.GameInterop;

public class ContainerInfo
{
    public uint ContainerSequence { get; private set; }

    public uint NumItems { get; private set; }

    public uint ContainerId { get; private set; }

    public uint StartOrFinish { get; private set; }

    public InventoryType InventoryType => (InventoryType)this.ContainerId;

    /// <summary>
    /// Read a packet off the wire.
    /// </summary>
    /// <param name="dataPtr">Packet data.</param>
    /// <returns>An object representing the data read.</returns>
    public static unsafe ContainerInfo Read(IntPtr dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 640L);
        using var reader = new BinaryReader(stream);

        var output = new ContainerInfo();
        output.ContainerSequence = reader.ReadUInt32();
        output.NumItems = reader.ReadUInt32();
        output.ContainerId = reader.ReadUInt32();
        output.StartOrFinish = reader.ReadUInt32();

        return output;
    }

    public string AsDebugString()
    {
        return
            $"Sequence: {this.ContainerSequence}, Number of Items: {this.NumItems}, Container ID: {this.ContainerId}, Start/Finish: {this.StartOrFinish}, InventoryType: {this.InventoryType}";
    }
}
