using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Agents;
using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

/// <summary>
/// Keeps track of market events by consuming data from various other services.
/// </summary>
public class RetainerMarketService(
    IGameInteropProvider gameInteropProvider,
    IPluginLog pluginLog,
    IInventoryService inventoryService,
    IAddonLifecycle addonLifecycle,
    IRetainerService retainerService,
    IFramework framework,
    MarketPriceUpdaterService marketPriceUpdaterService,
    IAtkOrderService atkOrderService) : IRetainerMarketService
{
    private ulong retainerId;
    private bool initialLoadingDone;

    private Hook<AgentInterface.Delegates.ReceiveEvent>? onReceiveEventHook;
    public delegate void ItemEventDelegate(RetainerMarketListEvent listEvent);

    public delegate void MarketEventDelegate();

    public delegate void UpdatedEventDelegate(RetainerMarketListEventType listEvent);

    /// <summary>
    /// A item was added to your market listings for the active retainer
    /// </summary>
    public event ItemEventDelegate? OnItemAdded;

    /// <summary>
    /// A item was removed from your market listings for the active retainer
    /// </summary>
    public event ItemEventDelegate? OnItemRemoved;

    /// <summary>
    /// A item was updated on your market listings for the active retainer
    /// </summary>
    public event ItemEventDelegate? OnItemUpdated;

    /// <summary>
    /// Any event happened
    /// </summary>
    public event UpdatedEventDelegate? OnUpdated;

    /// <summary>
    /// The retainer window was opened
    /// </summary>
    public event MarketEventDelegate? OnOpened;

    /// <summary>
    /// The retainer window was closed
    /// </summary>
    public event MarketEventDelegate? OnClosed;

    public IGameInteropProvider GameInteropProvider { get; } = gameInteropProvider;

    public IPluginLog PluginLog { get; } = pluginLog;

    public IInventoryService InventoryService { get; } = inventoryService;

    public IAddonLifecycle AddonLifecycle { get; } = addonLifecycle;

    public IRetainerService RetainerService { get; } = retainerService;

    public IFramework Framework { get; } = framework;

    public MarketPriceUpdaterService MarketPriceUpdaterService { get; } = marketPriceUpdaterService;

    public IAtkOrderService AtkOrderService { get; } = atkOrderService;

    public bool InBadState { get; private set; }

    public SaleItem?[] SaleItems { get; private set; } = new SaleItem[20];

    private RetainerMarketListEvent? MarketListEvent { get; set; }

    private HashSet<short> ActiveSlots { get; } = [];

    public void Dispose()
    {
        this.onReceiveEventHook?.Dispose();
    }

    public unsafe Task StartAsync(CancellationToken cancellationToken)
    {
        this.GameInteropProvider.InitializeFromAttributes(this);
        this.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.PostRefreshList);
        this.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", this.RetainerSellWindowOpened);
        this.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        this.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "RetainerSell", this.RetainerSellReceiveEvent);
        this.onReceiveEventHook ??= this.GameInteropProvider.HookFromAddress<AgentInterface.Delegates.ReceiveEvent>(AgentModule.Instance()->GetAgentByInternalId(AgentId.Retainer)->VirtualTable->ReceiveEvent, this.RetainerItemCommandDetour);
        this.onReceiveEventHook?.Enable();
        this.Framework.Update += this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Framework.Update -= this.FrameworkOnUpdate;
        this.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        this.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.PostRefreshList);
        this.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", this.RetainerSellWindowOpened);
        this.AddonLifecycle.UnregisterListener(
            AddonEvent.PreReceiveEvent,
            "RetainerSell",
            this.RetainerSellReceiveEvent);
        this.onReceiveEventHook?.Disable();
        return Task.CompletedTask;
    }

    private unsafe void RetainerSellReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is AddonReceiveEventArgs receiveEventArgs)
        {
            this.PluginLog.Verbose($"Event Param: {receiveEventArgs.EventParam}");
            this.PluginLog.Verbose($"Atk Event Param: {receiveEventArgs.AtkEventType}");
            if (receiveEventArgs.AtkEventData != nint.Zero)
            {
                var value = (AtkEventData*)receiveEventArgs.AtkEventData;
                this.PluginLog.Verbose(value->ListItemData.SelectedIndex.ToString());
            }
        }
    }

    private unsafe AtkValue* RetainerItemCommandDetour(
        AgentInterface* thisPtr,
        AtkValue* returnValue,
        AtkValue* values,
        uint a4,
        ulong eventKind)
    {
        try
        {
            this.PluginLog.Verbose("Item added to market.");
            var currentOrder = this.AtkOrderService.GetCurrentOrder();
            if (!this.InventoryService.HasSeenInventory((uint)InventoryType.RetainerMarket))
            {
                this.PluginLog.Verbose("RetainerMarket has not been seen.");
                this.InBadState = true;
                return this.onReceiveEventHook!.Original(thisPtr, returnValue, values, a4, eventKind);
            }

            if (currentOrder == null)
            {
                this.PluginLog.Verbose("No market order is available, unable to parse.");
                return this.onReceiveEventHook!.Original(thisPtr, returnValue, values, a4, eventKind);
            }


            var selectedItemContainer = this.InventoryService.GetInventoryContainer(InventoryType.BlockedItems);
            var selectedItem = selectedItemContainer->GetInventorySlot(0);

            var agentRetainer = (AgentRetainer*)thisPtr;

            // Values is the event sub command, cancel save price, save price, update price
            var subCommand = values->Int;
            var onMarket = selectedItem->Container == InventoryType.RetainerMarket;
            var retainerMarketContainer = this.InventoryService.GetInventoryContainer(InventoryType.BlockedItems);

            this.ActiveSlots.Clear();

            for (short i = 0; i < retainerMarketContainer->Size; i++)
            {
                if (retainerMarketContainer->Items[i].ItemId != 0)
                {
                    this.ActiveSlots.Add(i);
                }
            }

            var eventType = RetainerMarketListEventType.Unknown;
            var price = agentRetainer->CurrentPrice;
            var hasEvent = false;
            var isHq = selectedItem->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality);
            var slot = selectedItem->Slot;
            if (a4 == 5 && eventKind == 5 && subCommand == 0)
            {
                this.PluginLog.Verbose("Item removed from market.");
                eventType = RetainerMarketListEventType.Removed;
                hasEvent = true;
            }
            else
            {
                if (a4 == 1 && eventKind == 8 && subCommand == 0)
                {
                    if (onMarket)
                    {
                        this.PluginLog.Verbose("Item updated on market.");
                        eventType = RetainerMarketListEventType.Updated;
                        hasEvent = true;
                    }
                    else
                    {
                        this.PluginLog.Verbose("Item added to market.");
                        eventType = RetainerMarketListEventType.Added;
                        var nextFreeSlot = this.InventoryService.GetNextFreeSlot(InventoryType.RetainerMarket);
                        if (nextFreeSlot == null)
                        {
                            this.PluginLog.Error(
                                "A market item was added, but there was no slot free, this is definitely a bug.");
                        }
                        else
                        {
                            slot = nextFreeSlot.Value;
                            hasEvent = true;
                        }
                    }
                }
            }

            // Store the original quantity of the stack in the inventory, then compare it with the new value once the retainer item command has finished to work out how much they put up
            var originalQuantity = selectedItem->Quantity;

            if (hasEvent)
            {
                this.MarketListEvent = new RetainerMarketListEvent(eventType, slot);
                if (eventType != RetainerMarketListEventType.Removed)
                {
                    var saleItem = new SaleItem(
                        *selectedItem,
                        price,
                        this.RetainerService.RetainerId,
                        this.RetainerService.RetainerWorldId,
                        currentOrder.TryGetValue(slot, out var menuIndex) ? (uint)menuIndex : 999);
                    this.MarketListEvent.SaleItem = saleItem;
                }
            }

            var retainerItemCommandDetour = this.onReceiveEventHook!.Original(thisPtr, returnValue, values, a4, eventKind);

            if (eventType == RetainerMarketListEventType.Added && this.MarketListEvent?.SaleItem != null)
            {
                this.MarketListEvent.SaleItem.Quantity = (uint)(originalQuantity - selectedItem->Quantity);
            }

            return retainerItemCommandDetour;
        }
        catch (Exception e)
        {
            this.PluginLog.Error(e.Message);
        }

        return this.onReceiveEventHook!.Original(thisPtr, returnValue, values, a4, eventKind);
    }

    private unsafe void PostRefreshList(AddonEvent type, AddonArgs args)
    {
        if (!this.InventoryService.HasSeenInventory((uint)InventoryType.RetainerMarket))
        {
            this.InBadState = true;
            return;
        }

        var retainerMarketContainer = this.InventoryService.GetInventoryContainer(InventoryType.RetainerMarket);

        if (this.MarketListEvent != null)
        {
            if (this.MarketListEvent.EventType == RetainerMarketListEventType.Removed)
            {
                var slotFound = false;
                for (short i = 0; i < retainerMarketContainer->Size; i++)
                {
                    if (retainerMarketContainer->Items[i].ItemId == 0 && this.ActiveSlots.Contains(i))
                    {
                        this.MarketListEvent.Slot = i;
                        slotFound = true;
                        break;
                    }
                }

                if (!slotFound)
                {
                    this.PluginLog.Error("Removed an item from the market, but could not determine which slot");
                    return;
                }
            }

            this.PluginLog.Debug("Market list event detected: " + this.MarketListEvent.AsDebugString());
            if (this.MarketListEvent.SaleItem == null)
            {
                this.SaleItems[this.MarketListEvent.Slot] = null;
            }
            else
            {
                this.SaleItems[this.MarketListEvent.Slot] = this.MarketListEvent.SaleItem;
            }

            var currentOrder = this.AtkOrderService.GetCurrentOrder();
            if (currentOrder != null)
            {
                this.PluginLog.Verbose($"Current order has {currentOrder.Count} items");
                foreach (var item in currentOrder)
                {
                    this.PluginLog.Verbose($"Item {item.Key}: {item.Value}");
                }

                for (var index = 0; index < this.SaleItems.Length; index++)
                {
                    var item = this.SaleItems[index];
                    if (item != null && currentOrder.ContainsKey(index))
                    {
                        item.MenuIndex = (uint)currentOrder[index];
                    }
                }
            }

            switch (this.MarketListEvent.EventType)
            {
                case RetainerMarketListEventType.Added:
                    this.OnItemAdded?.Invoke(this.MarketListEvent);
                    break;
                case RetainerMarketListEventType.Removed:
                    this.OnItemRemoved?.Invoke(this.MarketListEvent);
                    break;
                case RetainerMarketListEventType.Updated:
                    this.OnItemUpdated?.Invoke(this.MarketListEvent);
                    break;
            }

            this.OnUpdated?.Invoke(this.MarketListEvent.EventType);
        }
    }

    private unsafe void LoadInitialItems(Dictionary<int, int>? currentOrder)
    {
        var retainerPrices = this.MarketPriceUpdaterService.CachedPrices;
        var retainerMarketItems = this.InventoryService.GetInventoryContainer(InventoryType.RetainerMarket);

        var retainerMarketCopy = new InventoryItem[20];
        for (var i = 0; i < retainerMarketItems->Size; i++)
        {
            retainerMarketCopy[i] = retainerMarketItems->Items[i];
        }

        retainerMarketCopy = [.. retainerMarketCopy];

        var saleItems = new SaleItem?[20];

        for (var index = 0; index < 20; index++)
        {
            var item = retainerMarketCopy[index];
            var retainerMarketItemPrice = retainerPrices[index];
            if (item.ItemId == 0)
            {
                saleItems[index] = null;
            }
            else
            {
                var saleItem = new SaleItem(
                    item,
                    retainerMarketItemPrice,
                    this.RetainerService.RetainerId,
                    this.RetainerService.RetainerWorldId,
                    currentOrder == null ? 1000 : currentOrder.TryGetValue(item.Slot, out var menuIndex) ? (uint)menuIndex : 999);
                saleItems[index] = saleItem;
            }
        }

        this.SaleItems = saleItems;
    }

    private void RetainerWindowOpened(AddonEvent type, AddonArgs args)
    {
        if (this.RetainerService.RetainerId != 0 && this.retainerId == 0 &&
            this.InventoryService.HasSeenInventory((uint)InventoryType.RetainerMarket))
        {
            this.initialLoadingDone = false;
            this.InBadState = false;
            this.retainerId = this.RetainerService.RetainerId;
            this.PluginLog.Verbose("Retainer window opened. Loading initial items");
            this.LoadInitialItems(null);
            this.OnOpened?.Invoke();
            this.OnUpdated?.Invoke(RetainerMarketListEventType.Initial);
        }
        else
        {
            if (this.InventoryService.HasSeenInventory((uint)InventoryType.RetainerMarket))
            {
                this.OnUpdated?.Invoke(RetainerMarketListEventType.Updated);
            }
            else
            {
                this.InBadState = true;
            }
        }
    }

    private void RetainerSellWindowOpened(AddonEvent type, AddonArgs args)
    {
        var currentOrder = this.AtkOrderService.GetCurrentOrder();

        if (this.RetainerService.RetainerId != 0 && !this.initialLoadingDone && this.InventoryService.HasSeenInventory((uint)InventoryType.RetainerMarket) && currentOrder != null)
        {
            this.PluginLog.Verbose($"Current order has {currentOrder.Count} items");
            foreach (var item in currentOrder)
            {
                this.PluginLog.Verbose($"Item {item.Key}: {item.Value}");
            }

            this.InBadState = false;
            this.retainerId = this.RetainerService.RetainerId;
            this.PluginLog.Verbose("Retainer sell list opened. Loading items");
            this.LoadInitialItems(currentOrder);
            this.initialLoadingDone = true;
            this.OnOpened?.Invoke();
            this.OnUpdated?.Invoke(RetainerMarketListEventType.Initial);
        }
        else
        {
            this.PluginLog.Verbose("Retainer sell list opened, could not find order.");
        }
    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        if (this.RetainerService.RetainerId == 0 && this.retainerId != 0 &&
            this.InventoryService.HasSeenInventory((uint)InventoryType.RetainerMarket))
        {
            this.InBadState = false;
            this.retainerId = 0;
            this.SaleItems = new SaleItem?[20];
            this.OnClosed?.Invoke();
            this.OnUpdated?.Invoke(RetainerMarketListEventType.Initial);
        }
    }
}
