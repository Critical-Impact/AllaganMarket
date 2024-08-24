// Copyright (c) PlaceholderCompany. All rights reserved.

using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;

namespace AllaganMarketMock.Services;

public class MockRetainerService : IRetainerService
{
    public uint RetainerWorldId => 0;

    public ulong RetainerId => 0;

    public uint RetainerGil => 0;
}
