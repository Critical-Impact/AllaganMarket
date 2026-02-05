using System;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.GameInterop;

using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Network;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

/// <summary>
/// Decodes the market price packets and caches them.
/// </summary>
public class MarketPriceUpdaterService(IGameInteropProvider gameInteropProvider, IPluginLog pluginLog)
    : IHostedService, IDisposable
{
    [Signature(
        "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D7 8B CE E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D7 8B CE E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D7 8B CE E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 57 10",
        DetourName = nameof(ItemMarketBoardInfoDetour))]
    private readonly Hook<ItemMarketBoardInfoData>? itemMarketBoardInfoHook = null;

    [Signature(
        "48 89 5C 24 ?? 57 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ??",
        DetourName = nameof(ItemRequestStartPacketDetour))]
    private readonly Hook<MarketBoardItemRequestStartPacketHandler>? itemRequestStartPacketDetourHook = null;
    private uint currentSequenceId;

    public delegate void MarketBoardItemRequestReceivedDelegate(MarketBoardItemRequest request);

    private unsafe delegate void* ItemMarketBoardInfoData(int a2, int* a3);

    private delegate nint MarketBoardItemRequestStartPacketHandler(nint a1, nint packetRef);

    public event MarketBoardItemRequestReceivedDelegate? MarketBoardItemRequestReceived;

    public RetainerMarketItemPrice[] CachedPrices { get; private set; } = new RetainerMarketItemPrice[20];

    public IGameInteropProvider GameInteropProvider { get; } = gameInteropProvider;

    public IPluginLog PluginLog { get; } = pluginLog;

    public bool HasCachedPrices { get; } = false;

    public uint CurrentSequenceId => this.currentSequenceId;

    public void Dispose()
    {
        this.itemMarketBoardInfoHook?.Dispose();
        this.itemRequestStartPacketDetourHook?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.GameInteropProvider.InitializeFromAttributes(this);
        this.itemMarketBoardInfoHook?.Enable();
        this.itemRequestStartPacketDetourHook?.Enable();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.itemMarketBoardInfoHook?.Disable();
        this.itemRequestStartPacketDetourHook?.Disable();
        return Task.CompletedTask;
    }

    private unsafe nint ItemRequestStartPacketDetour(nint a1, nint packetRef)
    {
        try
        {
            this.MarketBoardItemRequestReceived?.Invoke(MarketBoardItemRequest.Read(packetRef));
        }
        catch (Exception ex)
        {
            this.PluginLog.Error(ex, "ItemRequestStartPacketDetour threw an exception");
        }

        return this.itemRequestStartPacketDetourHook!.OriginalDisposeSafe(a1, packetRef);
    }

    private unsafe void* ItemMarketBoardInfoDetour(int seq, int* a3)
    {
        try
        {
            if (a3 != null)
            {
                var ptr = (IntPtr)a3 + 16;
                var retainerMarketItemPrice = RetainerMarketItemPrice.Read(ptr);
                if (retainerMarketItemPrice.Sequence != this.currentSequenceId)
                {
                    this.PluginLog.Verbose("New sequence ID received, resetting cached prices.");
                    this.currentSequenceId = retainerMarketItemPrice.Sequence;
                    this.CachedPrices = new RetainerMarketItemPrice[20];
                }

                if (retainerMarketItemPrice.ContainerId == (uint)InventoryType.RetainerMarket)
                {
                    this.CachedPrices[retainerMarketItemPrice.Slot] = retainerMarketItemPrice;
                    this.PluginLog.Verbose($"{retainerMarketItemPrice.AsDebugString()}");
                }
            }
        }
        catch (Exception e)
        {
            this.PluginLog.Error(e, "Something went wrong while decoding the retainer market board pricing");
        }

        return this.itemMarketBoardInfoHook!.Original(seq, a3);
    }
}
