// Copyright (c) PlaceholderCompany. All rights reserved.

using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;

namespace AllaganMarketMock;

public class MockRetainerService : IRetainerService
{
    public uint RetainerWorldId { get; set; }

    public ulong RetainerId { get; set; }

    public uint RetainerGil { get; set; }
}
