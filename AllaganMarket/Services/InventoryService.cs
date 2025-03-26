using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.GameInterop;
using AllaganMarket.Services.Interfaces;

using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace AllaganMarket.Services;

/// <summary>
/// Wraps InventoryManager and provides the same functionality.
/// </summary>
public class InventoryService(
    IGameInteropProvider gameInteropProvider,
    IPluginLog pluginLog,
    IRetainerService retainerService) : IInventoryService, IDisposable
{
    [Signature(
        "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D6 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D6 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D6 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 56 10",
        DetourName = nameof(ContainerInfoDetour),
        UseFlags = SignatureUseFlags.Hook)]
    private readonly Hook<ContainerInfoNetworkData>? containerInfoNetworkHook = null;
    private readonly HashSet<uint> loadedInventories = [];
    private ulong currentRetainer;

    private unsafe delegate void* ContainerInfoNetworkData(int a2, int* a3);

    public event IInventoryService.RetainerInventoryLoadedDelegate? OnRetainerInventoryLoaded;

    public IGameInteropProvider GameInteropProvider { get; } = gameInteropProvider;

    public IPluginLog PluginLog { get; } = pluginLog;

    public IRetainerService RetainerService { get; } = retainerService;

    public void Dispose()
    {
        this.containerInfoNetworkHook?.Dispose();
    }

    public bool HasSeenInventory(uint inventoryType)
    {
        return this.loadedInventories.Contains(inventoryType);
    }

    public unsafe short? GetNextFreeSlot(InventoryType inventoryType)
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);
        if (container->IsLoaded)
        {
            for (short i = 0; i < container->Size; i++)
            {
                if (container->Items[i].ItemId == 0)
                {
                    return i;
                }
            }
        }

        return null;
    }

    public unsafe InventoryContainer* GetInventoryContainer(InventoryType inventoryType)
    {
        return InventoryManager.Instance()->GetInventoryContainer(inventoryType);
    }

    public unsafe InventoryItem* GetInventorySlot(InventoryType inventoryType, int index)
    {
        return InventoryManager.Instance()->GetInventorySlot(inventoryType, index);
    }

    public unsafe int GetInventoryItemCount(
        uint itemId,
        bool isHq = false,
        bool checkEquipped = true,
        bool checkArmory = true,
        short minCollectability = 0)
    {
        return InventoryManager.Instance()->GetInventoryItemCount(
            itemId,
            isHq,
            checkEquipped,
            checkArmory,
            minCollectability);
    }

    public unsafe int GetItemCountInContainer(
        uint itemId,
        InventoryType inventoryType,
        bool isHq = false,
        short minCollectability = 0)
    {
        return InventoryManager.Instance()->GetItemCountInContainer(itemId, inventoryType, isHq, minCollectability);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.GameInteropProvider.InitializeFromAttributes(this);
        this.containerInfoNetworkHook?.Enable();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.containerInfoNetworkHook?.Disable();
        return Task.CompletedTask;
    }

    private unsafe void* ContainerInfoDetour(int seq, int* a3)
    {
        try
        {
            if (this.currentRetainer != this.RetainerService.RetainerId)
            {
                this.PluginLog.Verbose("Tracking new retainer, resetting loaded inventories.");
                this.loadedInventories.Clear();
                this.currentRetainer = this.RetainerService.RetainerId;
            }

            if (a3 != null)
            {
                var ptr = (IntPtr)a3 + 16;
                var containerInfo = ContainerInfo.Read(ptr);
                if (Enum.IsDefined(typeof(InventoryType), containerInfo.ContainerId))
                {
                    this.loadedInventories.Add(containerInfo.ContainerId);
                }
            }
        }
        catch (Exception e)
        {
            this.PluginLog.Error(e, "Something went wrong while decoding the container info");
        }

        return this.containerInfoNetworkHook!.Original(seq, a3);
    }
}
