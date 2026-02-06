using System;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.GameInterop;
using AllaganMarket.Services.Interfaces;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
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
public class MarketPriceUpdaterService(IGameInteropProvider gameInteropProvider, IAddonLifecycle addonLifecycle, IRetainerService retainerService, IPluginLog pluginLog)
    : IHostedService, IDisposable
{
    private readonly IAddonLifecycle addonLifecycle = addonLifecycle;
    private readonly IRetainerService retainerService = retainerService;

    [Signature(
        "48 89 5C 24 ?? 57 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ??",
        DetourName = nameof(ItemRequestStartPacketDetour))]
    private readonly Hook<MarketBoardItemRequestStartPacketHandler>? itemRequestStartPacketDetourHook = null;
    private uint currentSequenceId;

    public delegate void MarketBoardItemRequestReceivedDelegate(MarketBoardItemRequest request);

    private unsafe delegate void* ItemMarketBoardInfoData(nint networkInstance, int a2, int* a3);

    private delegate nint MarketBoardItemRequestStartPacketHandler(nint a1, nint packetRef);

    public event MarketBoardItemRequestReceivedDelegate? MarketBoardItemRequestReceived;

    public ulong[] CachedPrices { get; private set; } = new ulong[20];

    public IGameInteropProvider GameInteropProvider { get; } = gameInteropProvider;

    public IPluginLog PluginLog { get; } = pluginLog;

    public bool HasCachedPrices { get; } = false;

    public uint CurrentSequenceId => this.currentSequenceId;

    public void Dispose()
    {
        this.itemRequestStartPacketDetourHook?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.GameInteropProvider.InitializeFromAttributes(this);
        this.itemRequestStartPacketDetourHook?.Enable();
        this.addonLifecycle.RegisterListener(AddonEvent.PostShow, "SelectString", this.PostOpen);
        return Task.CompletedTask;
    }

    private unsafe void PostOpen(AddonEvent type, AddonArgs args)
    {
        if (this.retainerService.RetainerId != 0)
        {
            for (short i = 0; i < 20; i++)
            {
                var inventoryManager = InventoryManager.Instance();
                this.CachedPrices[i] = inventoryManager->GetRetainerMarketPrice(i);
                pluginLog.Verbose(inventoryManager->GetRetainerMarketPrice(i).ToString());
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.itemRequestStartPacketDetourHook?.Disable();
        this.addonLifecycle.UnregisterListener(AddonEvent.PostShow, "SelectString", this.PostOpen);
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
}
