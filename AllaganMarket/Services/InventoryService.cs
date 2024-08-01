namespace AllaganMarket.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using GameInterop;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Wraps InventoryManager and provides the same functionality
/// </summary>
public class InventoryService : IInventoryService, IDisposable
{
    [Signature(
        "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D3 8B CE E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D3 8B CE E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D3 8B CE E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 53 10 ",
        DetourName = nameof(ContainerInfoDetour),
        UseFlags = SignatureUseFlags.Hook)]
    private Hook<ContainerInfoNetworkData>? containerInfoNetworkHook = null;

    private ulong currentRetainer;
    private HashSet<uint> loadedInventories = new();

    public InventoryService(
        IGameInteropProvider gameInteropProvider,
        IPluginLog pluginLog,
        IRetainerService retainerService)
    {
        this.GameInteropProvider = gameInteropProvider;
        this.PluginLog = pluginLog;
        this.RetainerService = retainerService;
    }

    public IGameInteropProvider GameInteropProvider { get; }

    public IPluginLog PluginLog { get; }

    public IRetainerService RetainerService { get; }

    public void Dispose()
    {
        this.containerInfoNetworkHook?.Dispose();
    }

    public event RetainerInventoryLoadedDelegate? OnRetainerInventoryLoaded;

    public bool HasSeenInventory(uint inventoryType)
    {
        return this.loadedInventories.Contains(inventoryType);
    }

    public unsafe short? GetNextFreeSlot(InventoryType inventoryType)
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);
        if (container->Loaded != 0)
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
                    this.PluginLog.Verbose(containerInfo.AsDebugString());
                }
            }
        }
        catch (Exception e)
        {
            this.PluginLog.Error(e, "Something went wrong while decoding the container info");
        }

        return this.containerInfoNetworkHook!.Original(seq, a3);
    }

    private unsafe delegate void* ContainerInfoNetworkData(int a2, int* a3);
}

public unsafe interface IInventoryService : IHostedService
{
    public event RetainerInventoryLoadedDelegate OnRetainerInventoryLoaded;

    public InventoryContainer* GetInventoryContainer(InventoryType inventoryType);

    public InventoryItem* GetInventorySlot(InventoryType inventoryType, int index);

    public int GetInventoryItemCount(
        uint itemId,
        bool isHq = false,
        bool checkEquipped = true,
        bool checkArmory = true,
        short minCollectability = 0);

    public int GetItemCountInContainer(
        uint itemId,
        InventoryType inventoryType,
        bool isHq = false,
        short minCollectability = 0);

    short? GetNextFreeSlot(InventoryType inventoryType);

    bool HasSeenInventory(uint inventoryType);
}

public delegate void RetainerInventoryLoadedDelegate();