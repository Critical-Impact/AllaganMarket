using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganLib.Universalis.Models;
using AllaganLib.Universalis.Models.Bson;
using AllaganLib.Universalis.Services;

using AllaganMarket.Extensions;
using AllaganMarket.GameInterop;
using AllaganMarket.Mediator;
using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;

using DalaMock.Host.Mediator;

using Dalamud.Game.Network.Structures;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

/// <summary>
/// Keeps track of which items have been undercut.
/// Connects to universalis's websocket for live updates
/// Connects to universalis's API to get an initial read on what the prices of items are
/// Listens for marketboard events to see if they've been undercut when viewing the marketboad listing.
/// </summary>
public class UndercutService : IHostedService, IMediatorSubscriber
{
    private readonly UniversalisWebsocketService websocketService;
    private readonly MediatorService mediatorService;
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly IMarketBoard marketBoard;
    private readonly UniversalisApiService universalisApiService;
    private readonly SaleTrackerService saleTrackerService;
    private readonly IPluginLog pluginLog;
    private readonly IClientState clientState;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly Configuration configuration;
    private readonly MarketPriceUpdaterService marketPriceUpdaterService;
    private readonly IInventoryService inventoryService;
    private readonly RetainerMarketService retainerMarketService;
    private readonly UndercutComparisonSetting undercutComparisonSetting;
    private uint activeHomeWorld;

    public delegate void ItemUndercutDelegate(ulong retainerId, uint itemId);

    public event ItemUndercutDelegate? ItemUndercut;

    public UndercutService(
        UniversalisWebsocketService websocketService,
        MediatorService mediatorService,
        ICharacterMonitorService characterMonitorService,
        IMarketBoard marketBoard,
        UniversalisApiService universalisApiService,
        SaleTrackerService saleTrackerService,
        IPluginLog pluginLog,
        IClientState clientState,
        ExcelSheet<Item> itemSheet,
        Configuration configuration,
        MarketPriceUpdaterService marketPriceUpdaterService,
        IInventoryService inventoryService,
        RetainerMarketService retainerMarketService,
        UndercutComparisonSetting undercutComparisonSetting)
    {
        this.websocketService = websocketService;
        this.mediatorService = mediatorService;
        this.characterMonitorService = characterMonitorService;
        this.marketBoard = marketBoard;
        this.universalisApiService = universalisApiService;
        this.saleTrackerService = saleTrackerService;
        this.pluginLog = pluginLog;
        this.clientState = clientState;
        this.itemSheet = itemSheet;
        this.configuration = configuration;
        this.marketPriceUpdaterService = marketPriceUpdaterService;
        this.inventoryService = inventoryService;
        this.retainerMarketService = retainerMarketService;
        this.undercutComparisonSetting = undercutComparisonSetting;
        this.mediatorService.Subscribe<PluginLoadedMessage>(this, this.PluginLoaded);
    }

    public MediatorService MediatorService => this.mediatorService;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login += this.OnLogin;
        this.clientState.Logout += this.OnLogout;
        this.websocketService.OnUniversalisEvent += this.OnUniversalisMessage;
        this.marketBoard.OfferingsReceived += this.OfferingsReceived;
        this.universalisApiService.PriceRetrieved += this.UniversalisApiPriceRetrieved;
        this.marketPriceUpdaterService.MarketBoardItemRequestReceived += this.MarketBoardItemRequestReceived;
        this.retainerMarketService.OnItemUpdated += this.LocalRetainerMarketUpdated;

        // Check to see if they are logged in already
        this.OnLogin();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Monitors for when a price is updated locally by the player and caches the result.
    /// </summary>
    /// <param name="listEvent">The event from the retainer market service.</param>
    private void LocalRetainerMarketUpdated(RetainerMarketListEvent listEvent)
    {
        if (listEvent.SaleItem != null && listEvent.EventType == RetainerMarketListEventType.Updated) // Doesn't hurt to double-check
        {
            var saleItem = listEvent.SaleItem;
            this.UpdateMarketPriceCache(saleItem.ItemId, listEvent.SaleItem.IsHq, listEvent.SaleItem.WorldId, MarketPriceCacheType.Game, DateTime.Now, saleItem.UnitPrice, true);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login -= this.OnLogin;
        this.clientState.Logout -= this.OnLogout;
        this.websocketService.OnUniversalisEvent -= this.OnUniversalisMessage;
        this.marketBoard.OfferingsReceived -= this.OfferingsReceived;
        this.universalisApiService.PriceRetrieved -= this.UniversalisApiPriceRetrieved;
        this.marketPriceUpdaterService.MarketBoardItemRequestReceived -= this.MarketBoardItemRequestReceived;
        this.retainerMarketService.OnItemUpdated -= this.LocalRetainerMarketUpdated;
        return Task.CompletedTask;
    }

    public bool? IsItemUndercut(SaleItem saleItem)
    {
        return this.IsItemUndercut(saleItem.WorldId, saleItem.ItemId, saleItem.UnitPrice, saleItem.IsHq);
    }

    public bool? IsItemUndercut(uint worldId, uint itemId, uint currentPrice, bool isHq)
    {
        return this.GetUndercutBy(worldId, itemId, currentPrice, isHq) != null;
    }

    public uint? GetRecommendedUnitPrice(SaleItem saleItem)
    {
        return this.GetRecommendedUnitPrice(saleItem.WorldId, saleItem.ItemId, saleItem.IsHq);
    }

    public uint? GetRecommendedUnitPrice(uint worldId, uint itemId, bool isHq)
    {
        var undercutComparison = this.undercutComparisonSetting.CurrentValue(this.configuration);

        undercutComparison = this.configuration.GetUndercutComparison(itemId) ?? undercutComparison;

        bool? requestedQuality = null;
        if (undercutComparison == UndercutComparison.MatchingQuality)
        {
            requestedQuality = isHq;
        }
        else if (undercutComparison == UndercutComparison.NqOnly)
        {
            requestedQuality = false;
        }
        else if (undercutComparison == UndercutComparison.HqOnly)
        {
            requestedQuality = true;
        }

        if (!this.itemSheet.GetRow(itemId)?.CanBeHq ?? true)
        {
            requestedQuality = null;
        }

        var marketPriceCache = this.GetMarketPriceCache(worldId, itemId, requestedQuality) ?? this.GetMarketPriceCache(worldId, itemId, null);

        return marketPriceCache?.UnitCost;
    }

    public uint? GetUndercutBy(SaleItem saleItem)
    {
        return this.GetUndercutBy(saleItem.WorldId, saleItem.ItemId, saleItem.UnitPrice, saleItem.IsHq);
    }

    public uint? GetUndercutBy(uint worldId, uint itemId, uint currentPrice, bool isHq)
    {
        var undercutComparison = this.undercutComparisonSetting.CurrentValue(this.configuration);

        undercutComparison = this.configuration.GetUndercutComparison(itemId) ?? undercutComparison;

        bool? requestedQuality = null;
        if (undercutComparison == UndercutComparison.MatchingQuality)
        {
            requestedQuality = isHq;
        }
        else if (undercutComparison == UndercutComparison.NqOnly)
        {
            requestedQuality = false;
        }
        else if (undercutComparison == UndercutComparison.HqOnly)
        {
            requestedQuality = true;
        }

        if (!this.itemSheet.GetRow(itemId)?.CanBeHq ?? true)
        {
            requestedQuality = null;
        }

        var marketPriceCache = this.GetMarketPriceCache(worldId, itemId, requestedQuality);
        if (marketPriceCache != null)
        {
            if (marketPriceCache.OwnPrice)
            {
                return null;
            }

            if (marketPriceCache.UnitCost < currentPrice)
            {
                return currentPrice - marketPriceCache.UnitCost;
            }
        }

        return null;
    }

    public DateTime? GetLastUpdateTime(SaleItem saleItem)
    {
        var undercutComparison = this.undercutComparisonSetting.CurrentValue(this.configuration);

        undercutComparison = this.configuration.GetUndercutComparison(saleItem.ItemId) ?? undercutComparison;

        bool? requestedQuality = null;
        if (undercutComparison == UndercutComparison.MatchingQuality)
        {
            requestedQuality = saleItem.IsHq;
        }
        else if (undercutComparison == UndercutComparison.NqOnly)
        {
            requestedQuality = false;
        }
        else if (undercutComparison == UndercutComparison.HqOnly)
        {
            requestedQuality = true;
        }

        if (!this.itemSheet.GetRow(saleItem.ItemId)?.CanBeHq ?? true)
        {
            requestedQuality = null;
        }

        return this.GetLastUpdateTime(saleItem.WorldId, saleItem.ItemId, requestedQuality);
    }

    public DateTime? GetLastUpdateTime(uint worldId, uint itemId, bool? isHq = null)
    {
        // We only care about getting a market cache entry as either should be updated with the date we need
        var marketPriceCache = this.GetMarketPriceCache(worldId, itemId, isHq);
        return marketPriceCache?.LastUpdated;
    }

    public bool NeedsUpdate(SaleItem saleItem, int updatePeriodMinutes)
    {
        return this.NeedsUpdate(saleItem.WorldId, saleItem.ItemId, saleItem.IsHq, updatePeriodMinutes);
    }

    public bool NeedsUpdate(uint worldId, uint itemId, bool? isHq, int updatePeriodMinutes)
    {
        var lastUpdateTime = this.GetLastUpdateTime(worldId, itemId, isHq);
        if (lastUpdateTime == null)
        {
            lastUpdateTime = this.GetLastUpdateTime(worldId, itemId, null);
            if (lastUpdateTime == null)
            {
                return true;
            }
        }

        return DateTime.Now > lastUpdateTime + TimeSpan.FromMinutes(updatePeriodMinutes);
    }

    public DateTime NextUpdateDate(SaleItem saleItem, int updatePeriodMinutes)
    {
        var lastUpdateTime = this.GetLastUpdateTime(saleItem);

        if (lastUpdateTime == null)
        {
            return DateTime.Now;
        }

        return lastUpdateTime.Value + TimeSpan.FromMinutes(updatePeriodMinutes);
    }

    public void InsertFakeMarketPriceCache(SaleItem saleItem)
    {
        this.UpdateMarketPriceCache(saleItem.ItemId, saleItem.IsHq, saleItem.WorldId, MarketPriceCacheType.Override, DateTime.Now, saleItem.UnitPrice, true);
    }

    public MarketPriceCache? GetMarketPriceCache(uint worldId, uint itemId, bool? isHq)
    {
        if (this.configuration.MarketPriceCache.TryGetValue(worldId, out var itemCache))
        {
            MarketPriceCache? hqPrice = null;
            MarketPriceCache? nqPrice = null;
            if (isHq is false or null && itemCache.TryGetValue((itemId, false), out nqPrice))
            {
            }

            if (isHq is true or null && itemCache.TryGetValue((itemId, true), out hqPrice))
            {
            }

            if (isHq == null && nqPrice != null && hqPrice != null)
            {
                return nqPrice.UnitCost > hqPrice.UnitCost ? hqPrice : nqPrice;
            }

            if (isHq is false or null && nqPrice != null)
            {
                return nqPrice;
            }

            if (isHq is true or null && hqPrice != null)
            {
                return hqPrice;
            }
        }

        return null;
    }

    private void PluginLoaded(PluginLoadedMessage obj)
    {
        this.pluginLog.Verbose("Plugin has loaded, performing an initial scan of undercuts.");
        var allSales = this.saleTrackerService.GetSales(null, null);
        var toSearch = allSales.Select(c => (c.ItemId, c.WorldId)).Distinct();
        foreach (var item in toSearch)
        {
            this.universalisApiService.QueuePriceCheck(item.ItemId, item.WorldId);
        }
    }

    private void MarketBoardItemRequestReceived(MarketBoardItemRequest request)
    {
        if (request.AmountToArrive == 0)
        {
            this.OfferingsReceived(new EmptyOfferings());
        }
    }

    private void UniversalisApiPriceRetrieved(uint itemId, uint worldId, UniversalisPricing response)
    {
        if (response.listings != null && response.listings.Any())
        {
            var minNqListing = response.listings.Where(c => !c.hq).DefaultIfEmpty().MinBy(c => c?.pricePerUnit);
            var minHqListing = response.listings.Where(c => c.hq).DefaultIfEmpty().MinBy(c => c?.pricePerUnit);
            if (minNqListing != null)
            {
                var listingDate =
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(minNqListing.lastReviewTime);

                this.UpdateMarketPriceCache(itemId, false, worldId, MarketPriceCacheType.UniversalisReq, listingDate, (uint)minNqListing.pricePerUnit, this.characterMonitorService.IsCharacterKnown(minNqListing.retainerName, worldId));
            }

            if (minHqListing != null)
            {
                var listingDate =
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(minHqListing.lastReviewTime);

                this.UpdateMarketPriceCache(itemId, true, worldId, MarketPriceCacheType.UniversalisReq, listingDate, (uint)minHqListing.pricePerUnit, this.characterMonitorService.IsCharacterKnown(minHqListing.retainerName, worldId));
            }
        }
    }

    /// <summary>
    /// If we are logged in, retrieve our active sales for the world we are in, find the lowest offering from a retainer we don't own.
    /// </summary>
    /// <param name="offerings"></param>
    private unsafe void OfferingsReceived(IMarketBoardCurrentOfferings offerings)
    {
        var currentPlayer = this.clientState.LocalPlayer;
        if (currentPlayer != null)
        {
            var offeringDate = DateTime.Now;
            if (offerings.ItemListings.Count == 0)
            {
                this.pluginLog.Verbose(
                    "No item listings provided, marking item as updated as our price is more than likely correct.");
                var selectedItem = this.inventoryService.GetInventorySlot(InventoryType.DamagedGear, 0);
                if (selectedItem != null && selectedItem->ItemId != 0)
                {
                    var activeSales = this.saleTrackerService.GetSales(null, currentPlayer.HomeWorld.Id)
                                          .GroupBy(c => c.ItemId).ToDictionary(c => c.Key, c => c.ToList());
                    if (activeSales.TryGetValue(selectedItem->ItemId, out var currentSales))
                    {
                        this.UpdateMarketPriceCache(selectedItem->ItemId, selectedItem->Flags != InventoryItem.ItemFlags.None, currentPlayer.HomeWorld.Id, MarketPriceCacheType.Game, offeringDate, currentSales.Min(c => c.UnitPrice), true);
                    }
                }
            }
            else
            {
                var itemId = offerings.ItemListings[0].ItemId;
                var salesByItem = this.saleTrackerService.GetSales(null, currentPlayer.HomeWorld.Id)
                                      .GroupBy(c => c.ItemId).ToDictionary(c => c.Key, c => c.ToList());

                var lowestOfferingNq = offerings
                                     .ItemListings.Where(
                                         c => !this.characterMonitorService.IsCharacterKnown(c.RetainerId))
                                     .Where(c => c.IsHq == false)
                                     .DefaultIfEmpty()
                                     .Min(c => c?.PricePerUnit);

                if (lowestOfferingNq != null)
                {
                    this.UpdateMarketPriceCache(itemId, false, currentPlayer.HomeWorld.Id, MarketPriceCacheType.Game, offeringDate, lowestOfferingNq.Value, false);
                }

                var lowestOfferingHq = offerings
                                     .ItemListings.Where(
                                         c => !this.characterMonitorService.IsCharacterKnown(c.RetainerId))
                                     .Where(c => c.IsHq == true)
                                     .DefaultIfEmpty()
                                     .Min(c => c?.PricePerUnit);
                if (lowestOfferingHq != null)
                {
                    this.UpdateMarketPriceCache(itemId, true, currentPlayer.HomeWorld.Id, MarketPriceCacheType.Game, offeringDate, lowestOfferingHq.Value, false);
                }
            }
        }
    }

    private void OnLogout()
    {
        if (this.activeHomeWorld != 0)
        {
            this.websocketService.UnsubscribeFromChannel(
                UniversalisWebsocketService.EventType.ListingsAdd,
                this.activeHomeWorld);
            this.activeHomeWorld = 0;
        }
    }

    private void OnLogin()
    {
        if (this.clientState.LocalPlayer != null)
        {
            this.websocketService.SubscribeToChannel(
                UniversalisWebsocketService.EventType.ListingsAdd,
                this.clientState.LocalPlayer.HomeWorld.Id);
            this.activeHomeWorld = this.clientState.LocalPlayer.HomeWorld.Id;
        }
    }

    /// <summary>
    /// Takes data from the game, universalis's websocket or unviersalis's API. Updates the cache if an entry is newer. Fires of an event that other services can subscribe to.
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="isHq"></param>
    /// <param name="worldId"></param>
    /// <param name="type"></param>
    /// <param name="lastUpdated"></param>
    /// <param name="newUnitCost"></param>
    /// <param name="ownPrice"></param>
    private void UpdateMarketPriceCache(uint itemId, bool isHq, uint worldId, MarketPriceCacheType type, DateTime lastUpdated, uint newUnitCost, bool ownPrice)
    {
        var wasUpdated = false;
        this.configuration.MarketPriceCache.TryAdd(worldId, []);
        var itemKey = (itemId, isHq);
        if (this.configuration.MarketPriceCache[worldId].TryGetValue(itemKey, out var oldMarketPrice))
        {
            var newMarketPrice = new MarketPriceCache(itemId, isHq, worldId, type, lastUpdated, newUnitCost, ownPrice);
            var isBatchUpdate = Math.Truncate((newMarketPrice.LastUpdated - oldMarketPrice.LastUpdated).TotalSeconds) < 2;

            if ((oldMarketPrice.LastUpdated < newMarketPrice.LastUpdated && !isBatchUpdate) || (isBatchUpdate && oldMarketPrice.UnitCost > newMarketPrice.UnitCost))
            {
                this.configuration.MarketPriceCache[worldId][itemKey] = newMarketPrice;
                wasUpdated = true;
            }
        }
        else
        {
            this.configuration.MarketPriceCache[worldId][itemKey] = new MarketPriceCache(itemId, isHq, worldId, type, lastUpdated, newUnitCost, ownPrice);
            wasUpdated = true;
        }

        if (!this.configuration.MarketPriceCache[worldId][itemKey].OwnPrice && wasUpdated)
        {
            if (this.saleTrackerService.SaleItemsByItemId.TryGetValue(itemId, out var currentSales))
            {
                var ourCheapestPrice = currentSales.Where(saleItem => worldId == saleItem.WorldId).DefaultIfEmpty(null)
                                                   .Min(c => c?.UnitPrice ?? 0);
                var currentCheapestPrice = this.configuration.MarketPriceCache[worldId][itemKey];

                if (currentCheapestPrice.UnitCost < ourCheapestPrice || (currentCheapestPrice.UnitCost == ourCheapestPrice && currentCheapestPrice.OwnPrice))
                {
                    var undercutAmount = (uint?)(ourCheapestPrice - currentCheapestPrice.UnitCost);

                    foreach (var saleItem in currentSales.Where(c => c.WorldId == worldId))
                    {
                        if (!currentCheapestPrice.OwnPrice)
                        {
                            this.ItemUndercut?.Invoke(saleItem.RetainerId, saleItem.ItemId);
                        }
                    }

                    this.configuration.IsDirty = true;
                }
            }
        }
    }

    private void OnUniversalisMessage(SubscriptionReceivedMessage message)
    {
        this.pluginLog.Verbose(message.ToDebugString());
        if (message.EventType == UniversalisWebsocketService.EventType.ListingsAdd)
        {
            var itemId = message.Item;
            if (this.saleTrackerService.SaleItemsByItemId.TryGetValue(itemId, out var value))
            {
                var cheapestNqListing = message.Listings.DefaultIfEmpty(null).MinBy(c => c?.PricePerUnit ?? 0);
                var oldestReviewTimeNq = message.Listings.DefaultIfEmpty(null).Max(c => c?.LastReviewTime);
                if (oldestReviewTimeNq != null && cheapestNqListing != null)
                {
                    var ownsListing = this.characterMonitorService.Characters.Any(
                        c => c.Value.Name == cheapestNqListing.RetainerName &&
                             c.Value.WorldId == message.World);
                    var listingDate =
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(oldestReviewTimeNq.Value);
                    this.UpdateMarketPriceCache(itemId, false, message.World, MarketPriceCacheType.UniversalisWS, listingDate, (uint)cheapestNqListing.PricePerUnit, ownsListing);
                }

                var cheapestHqListing = message.Listings.DefaultIfEmpty(null).MinBy(c => c?.PricePerUnit ?? 0);
                var oldestReviewTimeHq = message.Listings.DefaultIfEmpty(null).Max(c => c?.LastReviewTime);
                if (oldestReviewTimeHq != null && cheapestHqListing != null)
                {
                    var ownsListing = this.characterMonitorService.Characters.Any(
                        c => c.Value.Name == cheapestHqListing.RetainerName &&
                             c.Value.WorldId == message.World);
                    var listingDate =
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(oldestReviewTimeHq.Value);
                    this.UpdateMarketPriceCache(itemId, true, message.World, MarketPriceCacheType.UniversalisWS, listingDate, (uint)cheapestHqListing.PricePerUnit, ownsListing);
                }
            }
        }
    }
}
