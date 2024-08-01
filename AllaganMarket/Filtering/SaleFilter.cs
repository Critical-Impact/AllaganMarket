namespace AllaganMarket.Filtering;

using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Extensions;
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
    private long aggSalesTotalGil;
    private long aggSoldTotalGil;
    private List<SaleItem>? cachedSales;
    private List<SoldItem>? cachedSoldItems;
    private ulong? characterId;
    private bool? isHq;
    private bool? showEmpty;

    private uint? itemId;
    private string? itemName;
    private string? listedAt;

    private bool needsRefresh;
    private string? quantity;
    private string? soldAt;
    private string? total;
    private string? unitPrice;
    private string? updatedAt;
    private uint? worldId;

    public SaleFilter(IDataManager dataManager, SaleTrackerService saleTrackerService, ICharacterMonitorService characterMonitorService)
    {
        this.dataManager = dataManager;
        this.saleTrackerService = saleTrackerService;
        this.characterMonitorService = characterMonitorService;
        this.itemSheet = dataManager.GetExcelSheet<Item>()!;
    }

    public long AggregateSalesTotalGil => this.aggSalesTotalGil;

    public long AggregateSoldTotalGil => this.aggSoldTotalGil;

    public string? ItemName
    {
        get => this.itemName;
        set
        {
            this.needsRefresh = true;
            this.itemName = value;
        }
    }

    public ulong? CharacterId
    {
        get => this.characterId;
        set
        {
            this.needsRefresh = true;
            this.characterId = value;
        }
    }

    public uint? ItemId
    {
        get => this.itemId;
        set
        {
            this.needsRefresh = true;
            this.itemId = value;
        }
    }

    public bool? IsHq
    {
        get => this.isHq;
        set
        {
            this.needsRefresh = true;
            this.isHq = value;
        }
    }

    public string? Quantity
    {
        get => this.quantity;
        set
        {
            this.needsRefresh = true;
            this.quantity = value;
        }
    }

    public string? UnitPrice
    {
        get => this.unitPrice;
        set
        {
            this.needsRefresh = true;
            this.unitPrice = value;
        }
    }

    public string? ListedAt
    {
        get => this.listedAt;
        set
        {
            this.needsRefresh = true;
            this.listedAt = value;
        }
    }

    public string? UpdatedAt
    {
        get => this.updatedAt;
        set
        {
            this.needsRefresh = true;
            this.updatedAt = value;
        }
    }

    public string? SoldAt
    {
        get => this.soldAt;
        set
        {
            this.needsRefresh = true;
            this.soldAt = value;
        }
    }

    public string? Total
    {
        get => this.total;
        set
        {
            this.needsRefresh = true;
            this.total = value;
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
        this.itemId = null;
        this.isHq = null;
        this.quantity = null;
        this.unitPrice = null;
        this.listedAt = null;
        this.updatedAt = null;
        this.soldAt = null;
        this.total = null;
        this.itemName = null;
        this.showEmpty = null;
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

        if (this.itemId != null)
        {
            sales = sales.Where(c => c.ItemId == this.itemId);
        }

        if (this.isHq != null)
        {
            sales = sales.Where(c => c.IsHq == this.isHq);
        }

        if (this.quantity != null)
        {
            sales = sales.Where(c => c.Quantity.PassesFilter(this.quantity));
        }

        if (this.unitPrice != null)
        {
            sales = sales.Where(c => c.UnitPrice.PassesFilter(this.unitPrice));
        }

        if (this.listedAt != null)
        {
            sales = sales.Where(c => c.ListedAt.PassesFilter(this.listedAt));
        }

        if (this.updatedAt != null)
        {
            sales = sales.Where(c => c.UpdatedAt.PassesFilter(this.updatedAt));
        }

        if (this.total != null)
        {
            sales = sales.Where(c => c.Total.PassesFilter(this.total));
        }

        if (this.itemName != null)
        {
            sales = sales.Where(
                c => this.GetItem(c.ItemId)?.Name.AsReadOnly().ExtractText().ToLowerInvariant()
                    .PassesFilter(this.itemName.ToLowerInvariant()) ?? false);
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
        if (this.itemId != null)
        {
            sales = sales.Where(c => c.ItemId == this.itemId);
        }

        if (this.isHq != null)
        {
            sales = sales.Where(c => c.IsHq == this.isHq);
        }

        if (this.quantity != null)
        {
            sales = sales.Where(c => c.Quantity.PassesFilter(this.quantity));
        }

        if (this.unitPrice != null)
        {
            sales = sales.Where(c => c.UnitPrice.PassesFilter(this.unitPrice));
        }

        if (this.soldAt != null)
        {
            sales = sales.Where(c => c.SoldAt.PassesFilter(this.soldAt));
        }

        if (this.total != null)
        {
            sales = sales.Where(c => c.Total.PassesFilter(this.total));
        }

        if (this.itemName != null)
        {
            sales = sales.Where(
                c => this.GetItem(c.ItemId)?.Name.AsReadOnly().ExtractText().ToLowerInvariant()
                    .PassesFilter(this.itemName.ToLowerInvariant()) ?? false);
        }

        sales = sales.Where(c => this.characterMonitorService.IsCharacterKnown(c.RetainerId));

        this.needsRefresh = false;
        this.cachedSoldItems = sales.ToList();
        this.aggSoldTotalGil = this.cachedSoldItems.Select(c => c.Total).Sum(c => c);
        return this.cachedSoldItems;
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
}
