using AllaganMarket.Grids;
using AllaganMarket.Settings;

namespace AllaganMarket.Filtering;

using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Models;
using Services;
using Services.Interfaces;

public class SaleFilter
{
    private readonly IDataManager dataManager;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly SaleTrackerService saleTrackerService;
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly ItemUpdatePeriodSetting itemUpdatePeriodSetting;
    private readonly Configuration configuration;
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

    public SaleFilter(
        IDataManager dataManager,
        SaleTrackerService saleTrackerService,
        ICharacterMonitorService characterMonitorService,
        ItemUpdatePeriodSetting itemUpdatePeriodSetting,
        Configuration configuration)
    {
        this.dataManager = dataManager;
        this.saleTrackerService = saleTrackerService;
        this.characterMonitorService = characterMonitorService;
        this.itemUpdatePeriodSetting = itemUpdatePeriodSetting;
        this.configuration = configuration;
        this.itemSheet = dataManager.GetExcelSheet<Item>()!;
    }

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


    public bool? NeedUpdating
    {
        get => this.needUpdating;
        set
        {
            this.needsRefresh = true;
            this.needUpdating = value;
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

    private List<SaleItem> RecalculateSaleItems()
    {
        var sales = this.saleTrackerService.GetSales(this.characterId, this.worldId);
        if (this.ShowEmpty != true)
        {
            sales = sales.Where(c => !c.IsEmpty());
        }

        if (this.NeedUpdating != null)
        {
            sales = sales.Where(
                c =>
            {
                var needsUpdate = c.NeedsUpdate(this.itemUpdatePeriodSetting.CurrentValue(this.configuration));
                return (needsUpdate && this.NeedUpdating.Value) || (!needsUpdate && this.NeedUpdating.Value);
            });
        }

        sales = sales.Where(c => this.characterMonitorService.IsCharacterKnown(c.RetainerId));

        this.needsRefresh = false;
        this.cachedSales = sales.ToList();
        this.aggSalesTotalGil = this.cachedSales.Select(c => c.Total).Sum(c => c);
        return this.cachedSales;
    }

    private List<SoldItem> RecalculateSoldItems()
    {
        var sales = this.saleTrackerService.GetSalesHistory(this.characterId, this.worldId);

        sales = sales.Where(c => this.characterMonitorService.IsCharacterKnown(c.RetainerId));

        this.needsRefresh = false;
        this.cachedSoldItems = sales.ToList();
        this.aggSoldTotalGil = this.cachedSoldItems.Select(c => c.Total).Sum(c => c);
        return this.cachedSoldItems;
    }

    public List<SearchResult> RecalculateSaleResults()
    {
        var saleItems = this.GetSaleItems();
        var searchResults = new List<SearchResult>();
        searchResults.AddRange(saleItems.Select(c => new SearchResult() { SaleItem = c }));
        return searchResults;
    }

    public List<SearchResult> RecalculateSoldResults()
    {
        var soldItems = this.GetSoldItems();
        var searchResults = new List<SearchResult>();
        searchResults.AddRange(soldItems.Select(c => new SearchResult() { SoldItem = c }));
        return searchResults;
    }

    public List<SaleItem> GetSaleItems()
    {
        if (this.needsRefresh || this.cachedSales == null)
        {
            this.cachedSales = this.RecalculateSaleItems();
            this.cachedSoldItems = this.RecalculateSoldItems();
        }

        return this.cachedSales;
    }

    public List<SoldItem> GetSoldItems()
    {
        if (this.needsRefresh || this.cachedSoldItems == null)
        {
            this.cachedSales = this.RecalculateSaleItems();
            this.cachedSoldItems = this.RecalculateSoldItems();
        }

        return this.cachedSoldItems;
    }

    public List<SearchResult> GetSaleResults()
    {
        if (this.needsRefresh || this.cachedSaleResults == null)
        {
            this.cachedSaleResults = this.RecalculateSaleResults();
        }

        return this.cachedSaleResults;
    }

    public List<SearchResult> GetSoldResults()
    {
        if (this.needsRefresh || this.cachedSoldResults == null)
        {
            this.cachedSoldResults = this.RecalculateSoldResults();
        }

        return this.cachedSoldResults;
    }
}
