namespace AllaganMarket.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Interfaces;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Hosting;
using Models;

/// <summary>
/// Keeps track of existing sales, current sales and determines when sales have occurred, also provides this data for other services to consume
/// </summary>
public class SaleTrackerService : IHostedService
{
    private readonly IChatGui chatGui;
    private readonly NumberFormatInfo gilNumberFormat;
    private readonly ExcelSheet<Item> itemSheet;
    private List<SaleItem>? allSales;
    private List<SoldItem>? allSalesHistory;

    private Dictionary<ulong, List<SaleItem>> characterSalesCache = new();

    private Dictionary<ulong, List<SoldItem>> characterSalesHistoryCache = new();
    private Dictionary<uint, List<SaleItem>> worldSalesCache = new();
    private Dictionary<uint, List<SoldItem>> worldSalesHistoryCache = new();

    public delegate void SnapshotCreatedDelegate();
    public delegate void SaleItemEventDelegate(SaleItem saleItem);
    public event SaleItemEventDelegate? SaleItemEvent;

    public event SnapshotCreatedDelegate? SnapshotCreated;

    public SaleTrackerService(
        IClientState clientState,
        IGameInventory gameInventory,
        IFramework framework,
        IInventoryService inventoryService,
        IRetainerService retainerService,
        RetainerMarketService retainerMarketService,
        IAddonLifecycle addonLifecycle,
        IPluginLog pluginLog,
        IDataManager dataManager,
        ICharacterMonitorService characterMonitorService,
        IChatGui chatGui,
        NumberFormatInfo gilNumberFormat)
    {
        this.chatGui = chatGui;
        this.gilNumberFormat = gilNumberFormat;
        this.ClientState = clientState;
        this.GameInventory = gameInventory;
        this.Framework = framework;
        this.InventoryService = inventoryService;
        this.RetainerService = retainerService;
        this.RetainerMarketService = retainerMarketService;
        this.AddonLifecycle = addonLifecycle;
        this.PluginLog = pluginLog;
        this.DataManager = dataManager;
        this.CharacterMonitorService = characterMonitorService;
        this.itemSheet = dataManager.GetExcelSheet<Item>()!;
    }

    public IClientState ClientState { get; }

    public IGameInventory GameInventory { get; }

    public IFramework Framework { get; }

    public IInventoryService InventoryService { get; }

    public IRetainerService RetainerService { get; }

    public RetainerMarketService RetainerMarketService { get; }

    public IAddonLifecycle AddonLifecycle { get; }

    public IPluginLog PluginLog { get; }

    public IDataManager DataManager { get; }

    public ICharacterMonitorService CharacterMonitorService { get; }

    public Dictionary<ulong, SaleItem[]> SaleItems { get; private set; } = new();

    public Dictionary<uint, List<SaleItem>> SaleItemsByItemId { get; private set; } = new();

    public Dictionary<ulong, uint> Gil { get; private set; } = new();

    public Dictionary<ulong, List<SoldItem>> Sales { get; private set; } = new();

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
                    .SelectMany(
                        c => this.SaleItems.TryGetValue(c.CharacterId, out var item) ? item : Array.Empty<SaleItem>()).Select(c => c).ToList();
                this.characterSalesCache[character.CharacterId] = items;
                return items;
            }

            if (character is { CharacterType: CharacterType.Retainer })
            {
                var items = (this.SaleItems.TryGetValue(character.CharacterId, out var item)
                    ? item
                    : Array.Empty<SaleItem>()).Select(c => c).ToList();
                this.characterSalesCache[character.CharacterId] = items;
                return items;
            }
        }

        if (worldId != null)
        {
            var worldCharacters =
                this.CharacterMonitorService.GetCharactersByType(CharacterType.Retainer, worldId.Value);
            var items = worldCharacters
                .SelectMany(
                    c => this.SaleItems.TryGetValue(c.CharacterId, out var item) ? item : Array.Empty<SaleItem>()).Select(c => c).ToList();
            this.worldSalesCache[worldId.Value] = items;
            return items;
        }

        this.allSales = this.SaleItems.SelectMany(c => c.Value).Select(c => c!).ToList();
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
                    c => this.Sales.TryGetValue(c.CharacterId, out var item) ? item : new List<SoldItem>()).ToList();
                this.characterSalesHistoryCache[character.CharacterId] = items;
                return items;
            }

            if (character is { CharacterType: CharacterType.Retainer })
            {
                var items = (this.Sales.TryGetValue(character.CharacterId, out var item) ? item : new List<SoldItem>())
                    .ToList();
                this.characterSalesHistoryCache[character.CharacterId] = items;
                return items;
            }
        }

        if (worldId != null)
        {
            var worldCharacters =
                this.CharacterMonitorService.GetCharactersByType(CharacterType.Retainer, worldId.Value);
            var items = worldCharacters.SelectMany(
                c => this.Sales.TryGetValue(c.CharacterId, out var item) ? item : new List<SoldItem>()).ToList();
            this.worldSalesHistoryCache[worldId.Value] = items;
            return items;
        }

        this.allSalesHistory = this.Sales.SelectMany(c => c.Value).ToList();
        return this.allSalesHistory;
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

        //let fam know that the sales cache was cleared OR notify an update happened
    }

    public void Start()
    {
        this.RetainerMarketService.OnUpdated += this.MarketOpened;
    }

    private void MarketOpened(RetainerMarketListEventType eventType)
    {
        var retainerId = this.RetainerService.RetainerId;
        var newRetainerGil = this.RetainerService.RetainerGil;
        var newSaleItems = this.RetainerMarketService.SaleItems;

        var previousItems = this.SaleItems.GetValueOrDefault(retainerId);
        uint? oldGil = this.GetRetainerGil(retainerId);

        this.CreateSnapshot(retainerId, previousItems, newSaleItems, oldGil, newRetainerGil);
    }

    public void CreateSnapshot(
        ulong retainerId,
        SaleItem[]? previousItems,
        SaleItem[] newItems,
        uint? oldGil,
        uint newGil)
    {
        if (previousItems == null)
        {
            this.SaleItems[retainerId] = newItems;
            for (var i = 0; i < 20; i++)
            {
                this.SaleItems[retainerId][i] = new SaleItem();
                this.SaleItems[retainerId][i].RetainerId = retainerId;
            }

            this.Gil[retainerId] = newGil;
        }
        else
        {
            for (var i = 0; i < 20; i++)
            {
                var previousItem = previousItems[i];
                var newItem = newItems[i];
                var potentialSales = oldGil == null ? 0 : newGil - oldGil;

                if (!previousItem.IsEmpty() && newItem.IsEmpty())
                {
                    //TODO: Use real percents later
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
                        this.PluginLog.Verbose("Item removed from market.");
                        //The assumption is that they took the item off the market
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
                                value = new List<SoldItem>();
                                this.Sales[retainerId] = value;
                            }

                            value.Add(newSale);

                            //TODO: make message optional
                            var item = this.itemSheet.GetRow(newSale.ItemId);
                            if (item != null)
                            {
                                this.chatGui.Print(
                                    $"You sold {newSale.Quantity} {item.Name.AsReadOnly().ExtractText()} for {newSale.TotalIncTax.ToString("C", this.gilNumberFormat)}");
                            }

                            this.PluginLog.Verbose("Sale created!");
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
                            this.PluginLog.Error("Item has been switched, AM does not know about this.");

                            //The item has changed entirely, they could have had the plugin off?
                        }
                        else if (previousItem.UnitPrice != newItem.UnitPrice)
                        {
                            this.PluginLog.Error("Item price mismatch.");
                        }
                        else
                        {
                            this.PluginLog.Error("Could not reconcile item.");
                        }

                        //not really an error
                        

                        //Item has changed in some way, we can't reconcile it, create a confirm entry
                    }
                }
            }

            this.Gil[retainerId] = newGil;

            for (var index = 0; index < newItems.Length; index++)
            {
                var item = newItems[index];
                var oldItem = this.SaleItems[retainerId][index];

                if (item.IsEmpty() || oldItem.IsEmpty() || !item.Equals(oldItem))
                {
                    if (!oldItem.IsEmpty() && !item.IsEmpty())
                    {
                        item.ListedAt = oldItem.ListedAt;
                    }

                    this.SaleItems[retainerId][index] = item;
                    this.ClearSalesCache();
                }
            }

            SnapshotCreated?.Invoke();
        }
    }

    private void UpdateSalesDictionary()
    {
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

    public void LoadExistingData(
        Dictionary<ulong, SaleItem[]> saleItems,
        Dictionary<ulong, uint> gil,
        Dictionary<ulong, List<SoldItem>> sales)
    {
        foreach (var retainer in saleItems)
        {
            for (var index = 0; index < retainer.Value.Length; index++)
            {
                var saleItem = retainer.Value[index];
                if (saleItem == null!)
                {
                    saleItem = new();
                    saleItem.RetainerId = retainer.Key;
                    retainer.Value[index] = saleItem;
                }
            }
        }
        this.SaleItems = saleItems;
        this.Gil = gil;
        this.Sales = sales;
        this.UpdateSalesDictionary();
    }

    public void Stop()
    {
        this.RetainerMarketService.OnUpdated -= this.MarketOpened;
    }
}
