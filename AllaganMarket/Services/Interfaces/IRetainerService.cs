namespace AllaganMarket.Services.Interfaces;

public interface IRetainerService
{
    public uint RetainerWorldId { get; }

    public ulong RetainerId { get; }

    public uint RetainerGil { get; }
}
