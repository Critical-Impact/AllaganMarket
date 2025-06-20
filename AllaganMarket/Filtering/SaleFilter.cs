using System.Collections.Generic;
using System.Linq;

using AllaganMarket.Mediator;
using AllaganMarket.Models;
using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;
using AllaganMarket.Tables;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AllaganMarket.Filtering;

public class SaleFilter(
    IDataManager dataManager,
    SaleTrackerService saleTrackerService,
    ICharacterMonitorService characterMonitorService,
    ItemUpdatePeriodSetting itemUpdatePeriodSetting,
    Configuration configuration,
    MediatorService mediatorService)
{
    private readonly IDataManager dataManager = dataManager;
    private readonly MediatorService mediatorService = mediatorService;
    private readonly ExcelSheet<Item> itemSheet = dataManager.GetExcelSheet<Item>()!;
    private long aggSalesTotalGil;
    private long aggSoldTotalGil;
    private List<SaleItem>? cachedSales;
    private List<SoldItem>? cachedSoldItems;
    private List<SearchResult>? cachedSaleResults;
    private List<SearchResult>? cachedSoldResults;
    private ulong? characterId;
    private bool? showEmpty;
    private bool? needUpdating;
    private bool needsRefresh;
    private uint? worldId;

    public long AggregateSalesTotalGil => this.aggSalesTotalGil;

    public long AggregateSoldTotalGil => this.aggSoldTotalGil;

    public ulong? CharacterId
    {
        get => this.characterId;
        set
        {
            this.needsRefresh = true;
            this.characterId = value;
        }
    }

    public uint? WorldId
    {
        get => this.worldId;
        set
        {
            this.needsRefresh = true;
            this.worldId = value;
        }
    }

    public bool? ShowEmpty
    {
        get => this.showEmpty;
        set
        {
            this.needsRefresh = true;
            this.showEmpty = value;
        }
    }

    public void Clear()
    {
        this.characterId = null;
        this.worldId = null;
        this.showEmpty = null;
        this.needUpdating = null;
        this.needsRefresh = true;
    }

    public void RequestRefresh()
    {
        this.needsRefresh = true;
    }

    public Item? GetItem(uint rowId)
    {
        return this.itemSheet.GetRow(rowId);
    }

    private List<SearchResult> recalculateSaleResults()
    {
        var saleItems = this.GetSaleItems();
        var searchResults = new List<SearchResult>();
        searchResults.AddRange(saleItems.Select(c => new SearchResult() { SaleItem = c }));
        return searchResults;
    }

    private List<SearchResult> recalculateSoldResults()
    {
        var soldItems = this.GetSoldItems();
        var searchResults = new List<SearchResult>();
        searchResults.AddRange(soldItems.Select(c => new SearchResult() { SoldItem = c }));
        return searchResults;
    }

    public void NotifyRefresh()
    {
        this.mediatorService.Publish(new SaleFilterRefreshedMessage());
    }

    public List<SaleItem> GetSaleItems()
    {
        if (this.needsRefresh || this.cachedSales == null)
        {
            this.cachedSales = this.RecalculateSaleItems();
            this.cachedSoldItems = this.RecalculateSoldItems();
            this.cachedSaleResults = this.recalculateSaleResults();
            this.cachedSoldResults = this.recalculateSoldResults();
            this.NotifyRefresh();
        }

        return this.cachedSales;
    }

    public List<SoldItem> GetSoldItems()
    {
        if (this.needsRefresh || this.cachedSoldItems == null)
        {
            this.cachedSales = this.RecalculateSaleItems();
            this.cachedSoldItems = this.RecalculateSoldItems();
            this.cachedSaleResults = this.recalculateSaleResults();
            this.cachedSoldResults = this.recalculateSoldResults();
            this.NotifyRefresh();
        }

        return this.cachedSoldItems;
    }

    public List<SearchResult> GetSaleResults()
    {
        if (this.needsRefresh || this.cachedSaleResults == null)
        {
            this.cachedSales = this.RecalculateSaleItems();
            this.cachedSoldItems = this.RecalculateSoldItems();
            this.cachedSaleResults = this.recalculateSaleResults();
            this.cachedSoldResults = this.recalculateSoldResults();
            this.NotifyRefresh();
        }

        return this.cachedSaleResults;
    }

    public List<SearchResult> GetSoldResults()
    {
        if (this.needsRefresh || this.cachedSoldResults == null)
        {
            this.cachedSales = this.RecalculateSaleItems();
            this.cachedSoldItems = this.RecalculateSoldItems();
            this.cachedSaleResults = this.recalculateSaleResults();
            this.cachedSoldResults = this.recalculateSoldResults();
            this.NotifyRefresh();
        }

        return this.cachedSoldResults;
    }

    private List<SaleItem> RecalculateSaleItems()
    {
        var sales = saleTrackerService.GetSales(this.characterId, this.worldId);
        if (this.ShowEmpty != true)
        {
            sales = sales.Where(c => !c.IsEmpty());
        }

        sales = sales.Where(c => characterMonitorService.IsCharacterKnown(c.RetainerId));

        this.needsRefresh = false;
        this.cachedSales = sales.ToList();
        this.aggSalesTotalGil = this.cachedSales.Select(c => c.Total).Sum(c => c);
        return this.cachedSales;
    }

    private List<SoldItem> RecalculateSoldItems()
    {
        var sales = saleTrackerService.GetSalesHistory(this.characterId, this.worldId);

        sales = sales.Where(c => characterMonitorService.IsCharacterKnown(c.RetainerId));

        this.needsRefresh = false;
        this.cachedSoldItems = sales.ToList();
        this.aggSoldTotalGil = this.cachedSoldItems.Select(c => c.Total).Sum(c => c);
        return this.cachedSoldItems;
    }
}
