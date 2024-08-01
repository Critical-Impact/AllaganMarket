using AllaganMarket.Services;

namespace AllaganMarketMock.Services;

public class MockRetainerService : IRetainerService
{
    public uint RetainerWorldId => 0;

    public ulong RetainerId => 0;

    public uint RetainerGil => 0;
}
