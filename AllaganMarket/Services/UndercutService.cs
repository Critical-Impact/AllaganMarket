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
using Lumina.Excel.Sheets;

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
    private readonly IRetainerMarketService retainerMarketService;
    private readonly UndercutComparisonSetting undercutComparisonSetting;
    private readonly UndercutBySetting undercutBySetting;
    private readonly UndercutAllowFallbackSetting undercutAllowFallbackSetting;
    private readonly RoundUpDownSetting roundUpDownSetting;
    private readonly RoundToSetting roundToSetting;
    private readonly IFramework framework;
    private readonly IObjectTable objectTable;
    private uint activeHomeWorld;

    public delegate void ItemUndercutDelegate(ulong retainerId, uint itemId);

    public event ItemUndercutDelegate? ItemUndercut;

    public readonly record struct UndercutResult(uint Amount, bool UsedFallback);

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
        IRetainerMarketService retainerMarketService,
        UndercutComparisonSetting undercutComparisonSetting,
        UndercutBySetting undercutBySetting,
        UndercutAllowFallbackSetting undercutAllowFallbackSetting,
        RoundUpDownSetting roundUpDownSetting,
        RoundToSetting roundToSetting,
        IFramework framework,
        IObjectTable objectTable)
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
        this.undercutBySetting = undercutBySetting;
        this.undercutAllowFallbackSetting = undercutAllowFallbackSetting;
        this.roundUpDownSetting = roundUpDownSetting;
        this.roundToSetting = roundToSetting;
        this.framework = framework;
        this.objectTable = objectTable;
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
        this.retainerMarketService.OnItemAdded += this.LocalRetainerMarketItemAdded;

        // Check to see if they are logged in already
        this.framework.RunOnFrameworkThread(this.OnLogin);
        return Task.CompletedTask;
    }

    // When an item is added and we don't have any pricing it means nobody was selling it so we should make this the lowest price
    private void LocalRetainerMarketItemAdded(RetainerMarketListEvent listEvent)
    {
        this.pluginLog.Verbose("Undercut Service: Has detected an item added to the market list.");
        if(listEvent.SaleItem != null)
        {
            var hasExistingCache = this.GetMarketPriceCache(
                listEvent.SaleItem.WorldId,
                listEvent.SaleItem.ItemId,
                listEvent.SaleItem.IsHq);
            if (hasExistingCache == null)
            {
                this.pluginLog.Verbose("Undercut Service: No existing cache so adding a new entry to the cache.");
                this.UpdateMarketPriceCache(
                    listEvent.SaleItem.ItemId,
                    listEvent.SaleItem.IsHq,
                    listEvent.SaleItem.WorldId,
                    MarketPriceCacheType.Game,
                    DateTime.Now,
                    listEvent.SaleItem.UnitPrice,
                    true);
            }
        }
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
        this.retainerMarketService.OnItemAdded -= this.LocalRetainerMarketItemAdded;
        return Task.CompletedTask;
    }

    public bool? IsItemUndercut(SaleItem saleItem)
    {
        // Calls a new overload with sanity defaults
        return this.IsItemUndercut(saleItem, 1, false);
    }
    // New overload to accept rounding parameters from the SaleItem context.
    public bool? IsItemUndercut(SaleItem saleItem, uint roundToVal, bool roundUpDown)
    {
        return this.IsItemUndercut(saleItem.WorldId, saleItem.ItemId, saleItem.UnitPrice, saleItem.IsHq, roundToVal, roundUpDown);
    }

    // Original overload calling new method with rounding values just in case something calls it
    public bool? IsItemUndercut(uint worldId, uint itemId, uint currentPrice, bool isHq)
    {
        // Calls the overload with default rounding parameters.
        return this.IsItemUndercut(worldId, itemId, currentPrice, isHq, 1, false);
    }
    public bool? IsItemUndercut(uint worldId, uint itemId, uint currentPrice, bool isHq, uint roundToVal, bool roundUpDown)
    {
        var recommendedUnitPrice = this.GetRecommendedUnitPrice(worldId, itemId, isHq, roundToVal, roundUpDown);
        if (recommendedUnitPrice != null)
        {
            return recommendedUnitPrice.Value.Amount < currentPrice;
        }

        return null;
    }

    public UndercutResult? GetRecommendedUnitPrice(SaleItem saleItem)
    {
        return this.GetRecommendedUnitPrice(saleItem.WorldId, saleItem.ItemId, saleItem.IsHq, 1, false);
    }

    public UndercutResult? GetRecommendedUnitPrice(uint worldId, uint itemId, bool isHq, uint roundToVal, bool roundUpDown)
    {
        var undercutComparison = this.undercutComparisonSetting.CurrentValue(this.configuration);
        var undercutAmount = this.undercutBySetting.CurrentValue(this.configuration);

        // Config details here
        var roundDir = this.roundUpDownSetting.CurrentValue(this.configuration);
        var roundTo = this.roundToSetting.CurrentValue(this.configuration);

        // If the user has set a custom value, use that instead of the default. Prevent potential issues with evaluating null variables below.
        if (roundDir)
        {
            roundUpDown = true;
        }

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

        if (this.itemSheet.TryGetRow(itemId, out var row))
        {
            if (!row.CanBeHq)
            {
                requestedQuality = false;
            }
        }

        var marketPriceCache = this.GetMarketPriceCache(worldId, itemId, requestedQuality);
        var wasFallback = false;
        if (this.undercutAllowFallbackSetting.CurrentValue(this.configuration) && marketPriceCache == null)
        {
            marketPriceCache = this.GetMarketPriceCache(worldId, itemId, null);
            if (marketPriceCache != null)
            {
                wasFallback = true;
            }
        }

        if (marketPriceCache != null)
        {
            decimal recommendedPrice = marketPriceCache.UnitCost;
            if (marketPriceCache.OwnPrice)
            {
                return new UndercutResult((uint)recommendedPrice, wasFallback);
            }

            // Rounding logic slapped in, make sure we do any undercut adjustment before the rounding happens.
            recommendedPrice -= undercutAmount;

            if (recommendedPrice < 0)
            {
                recommendedPrice = 0;
            }

            if (roundTo < 1)
            {
                roundTo = (int)roundToVal; // sanity check in case somebody decides to put less than 1 in a field, replaces with default of 1
            }

            if (roundTo > 1)
            {
                // False is down and default.
                if (roundUpDown)
                {
                    recommendedPrice = Math.Ceiling(recommendedPrice / roundTo) * roundTo;
                }
                else
                {
                    recommendedPrice = Math.Floor(recommendedPrice / roundTo) * roundTo;
                }
            }

            return new UndercutResult(Math.Max(1, (uint)recommendedPrice), wasFallback);
        }

        return null;
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

        if (!this.itemSheet.GetRowOrDefault(itemId)?.CanBeHq ?? true)
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

            if (marketPriceCache.UnitCost <= currentPrice)
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

        if (!this.itemSheet.GetRowOrDefault(saleItem.ItemId)?.CanBeHq ?? true)
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
        var lastUpdateTimeNq = this.GetLastUpdateTime(worldId, itemId, false);
        var lastUpdateTimeHq = this.GetLastUpdateTime(worldId, itemId, true);
        if (lastUpdateTimeNq == null && lastUpdateTimeHq == null)
        {
            return true;
        }

        var lastUpdateTime = DateTime.Now;

        if (lastUpdateTimeNq != null && lastUpdateTimeHq != null)
        {
            lastUpdateTime = lastUpdateTimeNq.Value > lastUpdateTimeHq.Value ? lastUpdateTimeNq.Value : lastUpdateTimeHq.Value;
        }
        else if (lastUpdateTimeNq != null)
        {
            lastUpdateTime = lastUpdateTimeNq.Value;
        }
        else if (lastUpdateTimeHq != null)
        {
            lastUpdateTime = lastUpdateTimeHq.Value;
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

    private uint? expectedAmountToArrive = null;
    private int? currentRequestId = null;
    private List<IMarketBoardItemListing> accumulatedListings = new List<IMarketBoardItemListing>();

    private void MarketBoardItemRequestReceived(MarketBoardItemRequest request)
    {
        if (this.expectedAmountToArrive != null)
        {
            this.pluginLog.Error("MarketBoard offerings received did not match the expected amount to arrive : " + request.AmountToArrive);
            this.expectedAmountToArrive = null;
        }

        this.accumulatedListings = new();
        this.currentRequestId = null;
        if (request.AmountToArrive == 0)
        {
            this.expectedAmountToArrive = 0;
            this.OfferingsReceived(new EmptyOfferings());
        }
        else
        {
            this.expectedAmountToArrive = request.AmountToArrive;
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
                this.UpdateMarketPriceCache(
                    itemId,
                    false,
                    worldId,
                    MarketPriceCacheType.UniversalisReq,
                    response.LastUpdate,
                    (uint)minNqListing.pricePerUnit,
                    this.characterMonitorService.IsCharacterKnown(minNqListing.retainerName, worldId));
            }
            else
            {
                this.RemoveMarketPriceCache(itemId, false, worldId, response.LastUpdate);
            }

            if (minHqListing != null)
            {
                this.UpdateMarketPriceCache(
                    itemId,
                    true,
                    worldId,
                    MarketPriceCacheType.UniversalisReq,
                    response.LastUpdate,
                    (uint)minHqListing.pricePerUnit,
                    this.characterMonitorService.IsCharacterKnown(minHqListing.retainerName, worldId));
            }
            else
            {
                this.RemoveMarketPriceCache(itemId, true, worldId, response.LastUpdate);
            }
        }
    }

    /// <summary>
    /// If we are logged in, retrieve our active sales for the world we are in, find the lowest offering from a retainer we don't own.
    /// </summary>
    /// <param name="offerings"></param>
    private unsafe void OfferingsReceived(IMarketBoardCurrentOfferings offerings)
    {
        if (this.expectedAmountToArrive == null)
        {
            this.pluginLog.Error("No offerings request was expected, ignoring these offerings.");
            return;
        }

        if (this.currentRequestId == null)
        {
            this.currentRequestId = offerings.RequestId;
        }
        else if (this.currentRequestId != offerings.RequestId)
        {
            this.pluginLog.Error("The offerings request ID did not match the current request ID.");
            return;
        }

        this.accumulatedListings.AddRange(offerings.ItemListings);

        if (this.accumulatedListings.Count >= this.expectedAmountToArrive)
        {
            this.pluginLog.Verbose($"All offerings received for {this.currentRequestId}.");
            this.expectedAmountToArrive = null;
            this.currentRequestId = null;
            var listings = this.accumulatedListings;
            this.accumulatedListings = new();

            var currentPlayer = this.objectTable.LocalPlayer;
            if (currentPlayer == null)
            {
                return;
            }

            var offeringDate = DateTime.Now;
            if (listings.Count == 0)
            {
                this.pluginLog.Verbose(
                    "No item listings provided, marking item as updated as our price is more than likely correct.");
                var selectedItem = this.inventoryService.GetInventorySlot(InventoryType.BlockedItems, 0);
                if (selectedItem != null && selectedItem->ItemId != 0)
                {
                    var activeSales = this.saleTrackerService.GetSales(null, currentPlayer.HomeWorld.RowId)
                                          .GroupBy(c => c.ItemId).ToDictionary(c => c.Key, c => c.ToList());
                    if (activeSales.TryGetValue(selectedItem->ItemId, out var currentSales))
                    {
                        var minCost = currentSales.Min(c => c.UnitPrice);
                        this.pluginLog.Verbose(
                            $"No item listings provided, saving minimum sale price of item to cache as {minCost}.");
                        this.UpdateMarketPriceCache(selectedItem->ItemId, selectedItem->Flags != InventoryItem.ItemFlags.None, currentPlayer.HomeWorld.RowId, MarketPriceCacheType.Game, offeringDate, minCost, true);
                    }
                    //TODO: Remove both cached entries
                }
            }
            else
            {
                var itemId = listings[0].ItemId;

                // Find the lowest NQ listing from a character that is not ours
                var lowestOfferingNq = listings.Where(
                                                   c => !this.characterMonitorService.IsCharacterKnown(c.RetainerId))
                                               .Where(c => c.IsHq == false)
                                               .DefaultIfEmpty()
                                               .Min(c => c?.PricePerUnit);

                if (lowestOfferingNq != null)
                {
                    this.pluginLog.Verbose($"Found an existing NQ offering stored for {itemId}, updating.");
                    this.UpdateMarketPriceCache(itemId, false, currentPlayer.HomeWorld.RowId, MarketPriceCacheType.Game, offeringDate, lowestOfferingNq.Value, false);
                }
                else
                {
                    var lowestOfferingOwnNq = listings.Where(
                                                          c => this.characterMonitorService.IsCharacterKnown(c.RetainerId))
                                                      .Where(c => c.IsHq == false)
                                                      .DefaultIfEmpty()
                                                      .Min(c => c?.PricePerUnit);
                    if (lowestOfferingOwnNq != null)
                    {
                        this.pluginLog.Verbose($"Found an existing NQ offering stored for {itemId} owned by the player, updating.");
                        this.UpdateMarketPriceCache(itemId, false, currentPlayer.HomeWorld.RowId, MarketPriceCacheType.Game, offeringDate, lowestOfferingOwnNq.Value, true);
                    }
                    else
                    {
                        this.pluginLog.Verbose($"Could not find an existing NQ offering stored for {itemId}, clearing the last cached figure.");
                        this.RemoveMarketPriceCache(itemId, false, currentPlayer.HomeWorld.RowId, offeringDate);
                    }
                }

                var lowestOfferingHq = listings.Where(
                                                   c => !this.characterMonitorService.IsCharacterKnown(c.RetainerId))
                                               .Where(c => c.IsHq == true)
                                               .DefaultIfEmpty()
                                               .Min(c => c?.PricePerUnit);
                if (lowestOfferingHq != null)
                {
                    this.pluginLog.Verbose($"Found an existing HQ offering stored for {itemId}, updating.");
                    this.UpdateMarketPriceCache(itemId, true, currentPlayer.HomeWorld.RowId, MarketPriceCacheType.Game, offeringDate, lowestOfferingHq.Value, false);
                }
                else
                {
                    var lowestOfferingOwnHq = listings.Where(
                                                          c => this.characterMonitorService.IsCharacterKnown(c.RetainerId))
                                                      .Where(c => c.IsHq == true)
                                                      .DefaultIfEmpty()
                                                      .Min(c => c?.PricePerUnit);
                    if (lowestOfferingOwnHq != null)
                    {
                        this.pluginLog.Verbose($"Found an existing HQ offering stored for {itemId} owned by the player, updating.");
                        this.UpdateMarketPriceCache(itemId, true, currentPlayer.HomeWorld.RowId, MarketPriceCacheType.Game, offeringDate, lowestOfferingOwnHq.Value, true);
                    }
                    else
                    {
                        this.pluginLog.Verbose($"Could not find an existing HQ offering stored for {itemId}, clearing the last cached figure.");
                        this.RemoveMarketPriceCache(itemId, true, currentPlayer.HomeWorld.RowId, offeringDate);
                    }
                }
            }
        }
        else
        {
            this.pluginLog.Verbose($"Waiting for more offerings for request {this.currentRequestId}.");
        }
    }

    private void OnLogout(int type, int code)
    {
        if (this.activeHomeWorld != 0)
        {
            this.pluginLog.Verbose($"Unsubscribing from universalis websocket for world {this.activeHomeWorld}.");
            this.websocketService.UnsubscribeFromChannel(
                UniversalisWebsocketService.EventType.ListingsAdd,
                this.activeHomeWorld);
            this.websocketService.UnsubscribeFromChannel(
                UniversalisWebsocketService.EventType.ListingsRemove,
                this.activeHomeWorld);
            this.activeHomeWorld = 0;
        }
    }

    private void OnLogin()
    {
        if (this.objectTable.LocalPlayer != null)
        {
            this.pluginLog.Verbose($"Subscribing to universalis websocket for world {this.objectTable.LocalPlayer.HomeWorld.RowId}.");
            this.websocketService.SubscribeToChannel(
                UniversalisWebsocketService.EventType.ListingsAdd,
                this.objectTable.LocalPlayer.HomeWorld.RowId);
            this.websocketService.SubscribeToChannel(
                UniversalisWebsocketService.EventType.ListingsRemove,
                this.objectTable.LocalPlayer.HomeWorld.RowId);
            this.activeHomeWorld = this.objectTable.LocalPlayer.HomeWorld.RowId;
        }
    }

    private void RemoveMarketPriceCache(uint itemId, bool isHq, uint worldId, DateTime lastUpdated)
    {
        this.configuration.MarketPriceCache.TryAdd(worldId, []);

        var itemKey = (itemId, isHq);

        if (this.configuration.MarketPriceCache[worldId].TryGetValue(itemKey, out var oldMarketPrice))
        {
            if (oldMarketPrice.LastUpdated < lastUpdated)
            {
                this.configuration.MarketPriceCache[worldId].Remove(itemKey);
            }
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
        if (itemId == 0)
        {
            this.pluginLog.Error($"{type} tried to update the market cache with a 0 ID item.");
            return;
        }

        if (this.itemSheet.TryGetRow(itemId, out var itemRow))
        {
            if (isHq && !itemRow.CanBeHq)
            {
                this.pluginLog.Error($"{type} tried to update the market cache with an update that is HQ but cannot be HQ.");
                return;
            }
        }
        else
        {
            this.pluginLog.Error($"{type} provided an invalid item ID: {itemId} on {worldId}.");
            return;
        }

        var wasUpdated = false;
        this.configuration.MarketPriceCache.TryAdd(worldId, []);
        var itemKey = (itemId, isHq);
        if (this.configuration.MarketPriceCache[worldId].TryGetValue(itemKey, out var oldMarketPrice))
        {
            var newMarketPrice = new MarketPriceCache(itemId, isHq, worldId, type, lastUpdated, newUnitCost, ownPrice);
            var isBatchUpdate = Math.Truncate((newMarketPrice.LastUpdated - oldMarketPrice.LastUpdated).TotalSeconds) < 2;

            this.pluginLog.Verbose($"Old Last Updated Date: {oldMarketPrice.LastUpdated.ToString(CultureInfo.CurrentCulture)}");
            this.pluginLog.Verbose($"New Last Updated Date: {newMarketPrice.LastUpdated.ToString(CultureInfo.CurrentCulture)}");
            // Always use the game's prices as they are always going to be newer.
            if ((oldMarketPrice.LastUpdated < newMarketPrice.LastUpdated && !isBatchUpdate) || (isBatchUpdate && oldMarketPrice.UnitCost > newMarketPrice.UnitCost) || type == MarketPriceCacheType.Game)
            {
                if (oldMarketPrice.UnitCost > newMarketPrice.UnitCost)
                {
                    wasUpdated = true;
                }
                this.configuration.MarketPriceCache[worldId][itemKey] = newMarketPrice;
            }
            else
            {
                this.pluginLog.Verbose($"Price for {itemId} on {worldId} was not newer than currently stored, was not cheaper and was not sourced from the game, ignoring.");
            }
        }
        else
        {
            this.pluginLog.Verbose($"Price for {itemId} on {worldId} was not cached, saving to cache.");
            this.configuration.MarketPriceCache[worldId][itemKey] = new MarketPriceCache(itemId, isHq, worldId, type, lastUpdated, newUnitCost, ownPrice);
            wasUpdated = true;
        }

        if (!this.configuration.MarketPriceCache[worldId][itemKey].OwnPrice)
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
                        if (!currentCheapestPrice.OwnPrice && wasUpdated)
                        {
                            this.ItemUndercut?.Invoke(saleItem.RetainerId, saleItem.ItemId);
                        }

                        if (saleItem.UpdatedAt < lastUpdated)
                        {
                            saleItem.UpdatedAt = lastUpdated;
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
                var cheapestNqListing = message.Listings.Where(c => !c.HQ).DefaultIfEmpty(null).MinBy(c => c?.PricePerUnit ?? 0);
                var oldestReviewTimeNq = message.Listings.Where(c => !c.HQ).DefaultIfEmpty(null).Max(c => c?.LastReviewTime);
                if (oldestReviewTimeNq != null && cheapestNqListing != null)
                {
                    var ownsListing = this.characterMonitorService.Characters.Any(c => c.Value.Name == cheapestNqListing.RetainerName &&
                             c.Value.WorldId == message.World);
                    var listingDate =
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(oldestReviewTimeNq.Value).ToLocalTime().AddSeconds(-10);
                    this.pluginLog.Verbose($"Adding NQ listing received from universalis WS for {itemId}");
                    this.UpdateMarketPriceCache(
                        itemId,
                        false,
                        message.World,
                        MarketPriceCacheType.UniversalisWS,
                        listingDate,
                        (uint)cheapestNqListing.PricePerUnit,
                        ownsListing);
                }

                var cheapestHqListing = message.Listings.Where(c => c.HQ).DefaultIfEmpty(null).MinBy(c => c?.PricePerUnit ?? 0);
                var oldestReviewTimeHq = message.Listings.Where(c => c.HQ).DefaultIfEmpty(null).Max(c => c?.LastReviewTime);
                if (oldestReviewTimeHq != null && cheapestHqListing != null)
                {
                    var ownsListing = this.characterMonitorService.Characters.Any(c => c.Value.Name == cheapestHqListing.RetainerName &&
                             c.Value.WorldId == message.World);
                    var listingDate =
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(oldestReviewTimeHq.Value).ToLocalTime().AddSeconds(-10);
                    this.UpdateMarketPriceCache(
                        itemId,
                        true,
                        message.World,
                        MarketPriceCacheType.UniversalisWS,
                        listingDate,
                        (uint)cheapestHqListing.PricePerUnit,
                        ownsListing);
                }
            }
        }

        if (message.EventType == UniversalisWebsocketService.EventType.ListingsRemove)
        {
            var itemId = message.Item;
            if (this.saleTrackerService.SaleItemsByItemId.TryGetValue(itemId, out var value))
            {
                this.pluginLog.Verbose($"Requesting new prices from universalis for {itemId}");
                //We have no idea what the lowest price is anymore, request the data again(ideally we'd cache all of the data but we currently don't and it'd require some restructuring)
                this.universalisApiService.QueuePriceCheck(itemId, message.World);
            }
        }
    }
}
