namespace AllaganMarket.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using GameInterop;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Decodes the market price packets and caches them 
/// </summary>
public class MarketPriceUpdaterService : IHostedService, IDisposable
{
    private uint currentSequenceId;

    [Signature(
        "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D3 8B CE E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 53 10",
        DetourName = nameof(ItemMarketBoardInfoDetour))]
    private Hook<ItemMarketBoardInfoData>? itemMarketBoardInfoHook = null;

    public MarketPriceUpdaterService(IGameInteropProvider gameInteropProvider, IPluginLog pluginLog)
    {
        this.GameInteropProvider = gameInteropProvider;
        this.PluginLog = pluginLog;
    }

    public RetainerMarketItemPrice[] CachedPrices { get; private set; } = new RetainerMarketItemPrice[20];

    public IGameInteropProvider GameInteropProvider { get; }

    public IPluginLog PluginLog { get; }

    public bool HasCachedPrices { get; } = false;

    public void Dispose()
    {
        this.itemMarketBoardInfoHook?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.GameInteropProvider.InitializeFromAttributes(this);
        this.itemMarketBoardInfoHook?.Enable();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.itemMarketBoardInfoHook?.Disable();
        return Task.CompletedTask;
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

    private unsafe delegate void* ItemMarketBoardInfoData(int a2, int* a3);
}