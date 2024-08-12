namespace AllaganMarket.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agents;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Microsoft.Extensions.Hosting;
using Models;

/// <summary>
/// Keeps track of market events by consuming data from various other services.
/// </summary>
public class RetainerMarketService : IHostedService, IDisposable
{
    public delegate void ItemEventDelegate(RetainerMarketListEvent listEvent);

    public delegate void MarketEventDelegate();

    public delegate void UpdatedEventDelegate(RetainerMarketListEventType listEvent);

    private ulong retainerId;

    /// <summary>
    /// This comes from Client::UI::Agent::AgentRetainer_ReceiveEvent
    /// </summary>
    [Signature(
        "40 53 55 56 57 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 84 24 ?? ?? ?? ??",
        DetourName = nameof(RetainerItemCommandDetour),
        Fallibility = Fallibility.Fallible)]
    internal Hook<RetainerItemCommandDelegate>? RetainerItemCommandHook;

    public RetainerMarketService(
        IGameInteropProvider gameInteropProvider,
        IPluginLog pluginLog,
        IInventoryService inventoryService,
        IAddonLifecycle addonLifecycle,
        IRetainerService retainerService,
        IFramework framework,
        MarketPriceUpdaterService marketPriceUpdaterService)
    {
        this.GameInteropProvider = gameInteropProvider;
        this.PluginLog = pluginLog;
        this.InventoryService = inventoryService;
        this.AddonLifecycle = addonLifecycle;
        this.RetainerService = retainerService;
        this.Framework = framework;
        this.MarketPriceUpdaterService = marketPriceUpdaterService;
    }

    public IGameInteropProvider GameInteropProvider { get; }

    public IPluginLog PluginLog { get; }

    public IInventoryService InventoryService { get; }

    public IAddonLifecycle AddonLifecycle { get; }

    public IRetainerService RetainerService { get; }

    public IFramework Framework { get; }

    public MarketPriceUpdaterService MarketPriceUpdaterService { get; }

    private RetainerMarketListEvent? MarketListEvent { get; set; }

    private HashSet<short> ActiveSlots { get; } = [];

    public SaleItem?[] SaleItems { get; private set; } = new SaleItem[20];

    public bool InBadState { get; private set; }

    public void Dispose()
    {
        this.RetainerItemCommandHook?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.GameInteropProvider.InitializeFromAttributes(this);
        this.RetainerItemCommandHook?.Enable();
        this.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.PostRefreshList);
        this.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        this.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "RetainerSell", this.RetainerSellReceiveEvent);
        this.Framework.Update += this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Framework.Update -= this.FrameworkOnUpdate;
        this.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        this.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "RetainerSellList", this.PostRefreshList);
        this.AddonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "RetainerSell", this.RetainerSellReceiveEvent);
        this.RetainerItemCommandHook?.Disable();
        return Task.CompletedTask;
    }

    private unsafe void RetainerSellReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is AddonReceiveEventArgs receiveEventArgs)
        {
            this.PluginLog.Verbose($"Event Param: {receiveEventArgs.EventParam}");
            this.PluginLog.Verbose($"Atk Event Param: {receiveEventArgs.AtkEventType}");
            var value = (AtkEventData*)receiveEventArgs.Data;
            this.PluginLog.Verbose(value->ListItemData.SelectedIndex.ToString());
        }
    }

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

    internal unsafe nint RetainerItemCommandDetour(
        nint agentRetainerItemCommandModule,
        nint result,
        byte* a3,
        uint a4,
        nint command)
    {
        this.PluginLog.Verbose("Item added to market.");
         if (!this.InventoryService.HasSeenInventory((uint)InventoryType.RetainerMarket))
         {
             this.PluginLog.Verbose("RetainerMarket has not been seen.");
             this.InBadState = true;
             return this.RetainerItemCommandHook!.Original(agentRetainerItemCommandModule, result, a3, a4, command);
         }
         try
         {
             var selectedItemContainer = this.InventoryService.GetInventoryContainer(InventoryType.DamagedGear);
             var selectedItem = selectedItemContainer->GetInventorySlot(0);
        
             var agentRetainer = (AgentRetainer*)agentRetainerItemCommandModule;
        
             var itemId = selectedItem->ItemId;
             var value = (AtkValue*)a3; //Value is the event sub command, cancel save price, save price, update price
             var subCommand = value->Int;
             var onMarket = selectedItem->Container == InventoryType.RetainerMarket;
             var retainerMarketContainer = this.InventoryService.GetInventoryContainer(InventoryType.RetainerMarket);
        
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
             if (a4 == 5 && command == 5 && subCommand == 0)
             {
                 this.PluginLog.Verbose("Item removed from market.");
                 eventType = RetainerMarketListEventType.Removed;
                 hasEvent = true;
             }
             else
             {
                 if (a4 == 1 && command == 8 && subCommand == 0)
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
                     var saleItem = new SaleItem(*selectedItem, price, this.RetainerService.RetainerId, this.RetainerService.RetainerWorldId);
                     this.MarketListEvent.SaleItem = saleItem;
                 }
             }

             var retainerItemCommandDetour = this.RetainerItemCommandHook!.Original(agentRetainerItemCommandModule, result, a3, a4, command);

             if (this.MarketListEvent?.SaleItem != null)
             {
                 this.MarketListEvent.SaleItem.Quantity = originalQuantity - selectedItem->Quantity;
             }

             return retainerItemCommandDetour;
         }
         catch (Exception e)
         {
             this.PluginLog.Error(e.Message);
         }

         return this.RetainerItemCommandHook!.Original(agentRetainerItemCommandModule, result, a3, a4, command);
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

    private unsafe void LoadInitialItems()
    {
        var retainerPrices = this.MarketPriceUpdaterService.CachedPrices;
        var retainerMarketItems = this.InventoryService.GetInventoryContainer(InventoryType.RetainerMarket);

        var retainerMarketCopy = new InventoryItem[20];
        for (var i = 0; i < retainerMarketItems->Size; i++)
        {
            retainerMarketCopy[i] = retainerMarketItems->Items[i];
        }

        retainerMarketCopy = retainerMarketCopy.ToArray();

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
                var saleItem = new SaleItem(item, retainerMarketItemPrice, this.RetainerService.RetainerId, this.RetainerService.RetainerWorldId);
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
            this.InBadState = false;
            this.retainerId = this.RetainerService.RetainerId;
            this.PluginLog.Verbose("Retainer window opened. Loading initial items");
            this.LoadInitialItems();
            this.OnOpened?.Invoke();
            this.OnUpdated?.Invoke(RetainerMarketListEventType.Initial);
        }
        else
        {
            this.OnUpdated?.Invoke(RetainerMarketListEventType.Updated);
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

    internal unsafe delegate nint RetainerItemCommandDelegate(
        nint agentRetainerItemCommandModule,
        nint result,
        byte* a3,
        uint a4,
        nint command);
}
