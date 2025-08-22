using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Extensions;
using AllaganMarket.Mediator;
using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllaganMarket.Services;

/// <summary>
/// Keeps track of existing sales, current sales and determines when sales have occurred, also provides this data for other services to consume.
/// </summary>
public class SaleTrackerService(
    IClientState clientState,
    IGameInventory gameInventory,
    IFramework framework,
    IInventoryService inventoryService,
    IRetainerService retainerService,
    IRetainerMarketService retainerMarketService,
    IAddonLifecycle addonLifecycle,
    ILogger<SaleTrackerService> logger,
    IDataManager dataManager,
    ICharacterMonitorService characterMonitorService,
    IChatGui chatGui,
    NumberFormatInfo gilNumberFormat,
    MediatorService mediatorService) : DisposableMediatorSubscriberBase(logger, mediatorService), IHostedService
{
    private readonly ExcelSheet<Item> itemSheet = dataManager.GetExcelSheet<Item>()!;
    private readonly Dictionary<ulong, List<SaleItem>> characterSalesCache = [];
    private readonly Dictionary<ulong, List<SoldItem>> characterSalesHistoryCache = [];
    private readonly Dictionary<uint, List<SaleItem>> worldSalesCache = [];
    private readonly Dictionary<uint, List<SoldItem>> worldSalesHistoryCache = [];
    private List<SaleItem>? allSales;
    private List<SoldItem>? allSalesHistory;

    public delegate void SnapshotCreatedDelegate();

    public delegate void ItemSoldDelegate(SaleItem saleItem, SoldItem soldItem);

    public event SnapshotCreatedDelegate? SnapshotCreated;

    public event ItemSoldDelegate? ItemSold;

    public IClientState ClientState { get; } = clientState;

    public IGameInventory GameInventory { get; } = gameInventory;

    public IFramework Framework { get; } = framework;

    public IInventoryService InventoryService { get; } = inventoryService;

    public IRetainerService RetainerService { get; } = retainerService;

    public IRetainerMarketService RetainerMarketService { get; } = retainerMarketService;

    public IAddonLifecycle AddonLifecycle { get; } = addonLifecycle;


    public IDataManager DataManager { get; } = dataManager;

    public ICharacterMonitorService CharacterMonitorService { get; } = characterMonitorService;

    public Dictionary<ulong, SaleItem[]> SaleItems { get; private set; } = [];

    public Dictionary<uint, List<SaleItem>> SaleItemsByItemId { get; private set; } = [];

    public Dictionary<ulong, uint> Gil { get; private set; } = [];

    public Dictionary<ulong, List<SoldItem>> Sales { get; private set; } = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Stop();
        return Task.CompletedTask;
    }

    public SaleItem[]? GetRetainerSales(ulong retainerId)
    {
        return this.SaleItems.TryGetValue(retainerId, out var sales) ? sales : null;
    }

    public uint? GetRetainerGil(ulong retainerId)
    {
        return this.Gil.TryGetValue(retainerId, out var gil) ? gil : null;
    }

    public int CalculateTax(double taxRate, int unitPrice, int quantity)
    {
        return (int)Math.Floor(quantity * unitPrice * taxRate);
    }

    public SaleItem? GetSaleItem(uint itemId, uint? worldId)
    {
        if (this.SaleItemsByItemId.TryGetValue(itemId, out var saleItems))
        {
            return saleItems.FirstOrDefault(c => c.ItemId == itemId && (c.WorldId == worldId || worldId == null));
        }

        return null;
    }

    public IEnumerable<SaleItem> GetSales(ulong? characterId, uint? worldId)
    {
        if (characterId != null && this.characterSalesCache.TryGetValue(characterId.Value, out var characterSales))
        {
            return characterSales;
        }

        if (worldId != null && this.worldSalesCache.TryGetValue(worldId.Value, out var worldSales))
        {
            return worldSales;
        }

        if (characterId == null && worldId == null && this.allSales != null)
        {
            return this.allSales;
        }

        if (characterId != null)
        {
            var character = this.CharacterMonitorService.GetCharacterById(characterId.Value);
            if (character is { CharacterType: CharacterType.Character })
            {
                var ownedCharacters = this.CharacterMonitorService.GetOwnedCharacters(
                    characterId.Value,
                    CharacterType.Retainer);
                var items = ownedCharacters
                            .SelectMany(c => this.SaleItems.TryGetValue(c.CharacterId, out var item) ? item : [])
                            .Select(c => c).ToList();
                this.characterSalesCache[character.CharacterId] = items;
                return items;
            }

            if (character is { CharacterType: CharacterType.Retainer })
            {
                var items = (this.SaleItems.TryGetValue(character.CharacterId, out var item)
                                 ? item
                                 : []).Select(c => c).ToList();
                this.characterSalesCache[character.CharacterId] = items;
                return items;
            }
        }

        if (worldId != null)
        {
            var worldCharacters =
                this.CharacterMonitorService.GetCharactersByType(CharacterType.Retainer, worldId.Value);
            var items = worldCharacters
                        .SelectMany(c => this.SaleItems.TryGetValue(c.CharacterId, out var item) ? item : [])
                        .Select(c => c).ToList();
            this.worldSalesCache[worldId.Value] = items;
            return items;
        }

        this.allSales = this.SaleItems.SelectMany(c => c.Value).Select(c => c).ToList();
        return this.allSales;
    }

    public IEnumerable<SoldItem> GetSalesHistory(ulong? characterId, uint? worldId)
    {
        if (characterId != null && this.characterSalesHistoryCache.TryGetValue(characterId.Value, out var history))
        {
            return history;
        }

        if (worldId != null && this.worldSalesHistoryCache.TryGetValue(worldId.Value, out var salesHistory))
        {
            return salesHistory;
        }

        if (characterId == null && worldId == null && this.allSalesHistory != null)
        {
            return this.allSalesHistory;
        }

        if (characterId != null)
        {
            var character = this.CharacterMonitorService.GetCharacterById(characterId.Value);
            if (character is { CharacterType: CharacterType.Character })
            {
                var ownedCharacters = this.CharacterMonitorService.GetOwnedCharacters(
                    characterId.Value,
                    CharacterType.Retainer);
                var items = ownedCharacters.SelectMany(
                    c => this.Sales.TryGetValue(c.CharacterId, out var item) ? item : []).ToList();
                this.characterSalesHistoryCache[character.CharacterId] = items;
                return items;
            }

            if (character is { CharacterType: CharacterType.Retainer })
            {
                var items = (this.Sales.TryGetValue(character.CharacterId, out var item) ? item : [])
                    .ToList();
                this.characterSalesHistoryCache[character.CharacterId] = items;
                return items;
            }
        }

        if (worldId != null)
        {
            var worldCharacters =
                this.CharacterMonitorService.GetCharactersByType(CharacterType.Retainer, worldId.Value);
            var items = worldCharacters.SelectMany(c => this.Sales.TryGetValue(c.CharacterId, out var item) ? item : [])
                                       .ToList();
            this.worldSalesHistoryCache[worldId.Value] = items;
            return items;
        }

        this.allSalesHistory = this.Sales.SelectMany(c => c.Value).ToList();
        return this.allSalesHistory;
    }

    public void Start()
    {
        this.RetainerMarketService.OnUpdated += this.MarketOpened;
        this.MediatorService.Subscribe<DeleteSoldItem>(this, this.DeleteSoldItem);
    }

    public void LoadExistingData(
        Dictionary<ulong, SaleItem[]> saleItems,
        Dictionary<ulong, uint> gil,
        Dictionary<ulong, List<SoldItem>> sales)
    {
        this.SaleItems = saleItems;
        this.Gil = gil;
        this.Sales = sales;
        this.ClearSalesCache();
    }

    public void Stop()
    {
        this.RetainerMarketService.OnUpdated -= this.MarketOpened;
    }

    private void ClearSalesCache()
    {
        this.characterSalesCache.Clear();
        this.worldSalesCache.Clear();
        this.characterSalesHistoryCache.Clear();
        this.characterSalesHistoryCache.Clear();
        this.allSales = null;
        this.allSalesHistory = null;
        this.UpdateSalesDictionary();
    }

    private void DeleteSoldItem(DeleteSoldItem obj)
    {
        if (this.Sales.TryGetValue(obj.SoldItem.RetainerId, out var currentSales))
        {
            currentSales.Remove(obj.SoldItem);
            this.Sales[obj.SoldItem.RetainerId] = currentSales;
            this.ClearSalesCache();
            this.SnapshotCreated?.Invoke();
        }
    }

    private void MarketOpened(RetainerMarketListEventType eventType)
    {
        var retainerId = this.RetainerService.RetainerId;
        var newRetainerGil = this.RetainerService.RetainerGil;
        var newSaleItems = this.RetainerMarketService.SaleItems;

        if (!this.SaleItems.ContainsKey(retainerId))
        {
            this.SaleItems.TryAdd(retainerId, new SaleItem[20].FillList(retainerId));
        }

        var previousItems = this.SaleItems[retainerId];
        var oldGil = this.GetRetainerGil(retainerId);

        this.CreateSnapshot(retainerId, previousItems, newSaleItems.FillList(retainerId), oldGil, newRetainerGil);
    }

    private void CreateSnapshot(
        ulong retainerId,
        SaleItem[] previousItems,
        SaleItem[] newItems,
        uint? oldGil,
        uint newGil)
    {
        for (var i = 0; i < 20; i++)
        {
            var previousItem = previousItems[i];
            var newItem = newItems[i];
            var potentialSales = oldGil == null ? 0 : newGil - oldGil;

            if (!previousItem.IsEmpty() && newItem.IsEmpty())
            {
                // TODO: Use real percents later
                var retainer = this.CharacterMonitorService.GetCharacterById(retainerId);
                var taxRate = 0.05d;
                if (retainer != null)
                {
                    switch (retainer.RetainerTown)
                    {
                        case RetainerManager.RetainerTown.Kugane:
                        case RetainerManager.RetainerTown.Crystarium:
                        case RetainerManager.RetainerTown.OldSharlayan:
                            taxRate = 0.03d;
                            break;
                        case null:
                            break;
                    }
                }

                if (oldGil == newGil)
                {
                    this.Logger.LogTrace("Item removed from market.");

                    // The assumption is that they took the item off the market
                }
                else
                {
                    var actualSalesAmount = previousItem.Total - this.CalculateTax(
                                                taxRate,
                                                (int)previousItem.UnitPrice,
                                                (int)previousItem.Quantity);
                    if (potentialSales - actualSalesAmount >= 0)
                    {
                        var newSale = new SoldItem(previousItem);
                        if (!this.Sales.TryGetValue(retainerId, out var value))
                        {
                            value = [];
                            this.Sales[retainerId] = value;
                        }

                        value.Add(newSale);

                        var item = this.itemSheet.GetRowOrDefault(newSale.ItemId);
                        if (item != null)
                        {
                            this.ItemSold?.Invoke(previousItem, newSale);
                        }

                        this.Logger.LogTrace("Sale created!");
                    }
                }

                // Item has been removed and we don't know about it. Try to account for the gil, if we have enough gil, mark it as sold and create a sold entry
            }
            else if (!previousItem.IsEmpty() && !newItem.IsEmpty())
            {
                if (!previousItem.Equals(newItem))
                {
                    if (previousItem.ItemId != newItem.ItemId)
                    {
                        this.Logger.LogError("Item has been switched, AM does not know about this.");
                    }
                    else if (previousItem.UnitPrice != newItem.UnitPrice)
                    {
                        this.Logger.LogTrace("Item price mismatch, item was probably updated.");
                    }
                    else
                    {
                        // TODO: Add reconcilation tab
                        this.Logger.LogError("Could not reconcile item.");
                    }
                }
            }
        }

        this.Gil[retainerId] = newGil;

        for (var index = 0; index < newItems.Length; index++)
        {
            var item = newItems[index];
            var oldItem = this.SaleItems[retainerId][index];

            if (item.IsEmpty() || oldItem.IsEmpty() || !item.Equals(oldItem) || item.MenuIndex != oldItem.MenuIndex)
            {
                if (!oldItem.IsEmpty() && !item.IsEmpty())
                {
                    item.ListedAt = oldItem.ListedAt;

                    // We don't know the new menu index yet so copy the old index until we do
                    if (item.MenuIndex == 1000)
                    {
                        item.MenuIndex = oldItem.MenuIndex;
                    }
                }

                this.SaleItems[retainerId][index] = item;
                this.ClearSalesCache();
            }
        }

        this.Logger.LogTrace("A snapshot was created.");
        this.SnapshotCreated?.Invoke();
    }

    private void UpdateSalesDictionary()
    {
        var characters = this.CharacterMonitorService.GetCharactersByType(CharacterType.Retainer, null);
        foreach (var character in characters)
        {
            if (!this.SaleItems.ContainsKey(character.CharacterId))
            {
                this.SaleItems[character.CharacterId] = new SaleItem[20].FillList(character.CharacterId);
            }
        }

        var newDict = new Dictionary<uint, List<SaleItem>>();
        foreach (var retainerSales in this.SaleItems)
        {
            foreach (var sale in retainerSales.Value)
            {
                if (!sale.IsEmpty())
                {
                    newDict.TryAdd(sale.ItemId, []);
                    newDict[sale.ItemId].Add(sale);
                }
            }
        }

        this.SaleItemsByItemId = newDict;
    }
}
