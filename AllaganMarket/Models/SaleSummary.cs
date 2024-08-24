using System;
using System.Collections.Generic;
using System.Linq;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Widgets;

using AllaganMarket.Extensions;
using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Tables;

namespace AllaganMarket.Models;

public class SaleSummary : IDisposable, IConfigurable<SaleSummaryGroup>, IConfigurable<(DateTime, DateTime)?>,
                           IConfigurable<(TimeUnit, int)?>
{
    private readonly SaleTrackerService saleTrackerService;
    private readonly ICharacterMonitorService characterMonitorService;
    private bool isDirty;
    private List<SaleSummaryItem> summaryItems = [];
    private List<SearchResult>? searchResults;

    public SaleSummary(SaleTrackerService saleTrackerService, ICharacterMonitorService characterMonitorService)
    {
        this.saleTrackerService = saleTrackerService;
        this.characterMonitorService = characterMonitorService;
        this.saleTrackerService.SnapshotCreated += this.SaleTrackerServiceOnSnapshotCreated;
        this.isDirty = true;
    }

    public SaleSummaryGroup GroupBy { get; set; } = SaleSummaryGroup.Item;

    public uint? ItemId { get; set; }

    public uint? WorldId { get; set; }

    public bool? IsHq { get; set; }

    public ulong? OwnerId { get; set; }

    public ulong? RetainerId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public TimeUnit? TimeUnit { get; set; }

    public int? TimeValue { get; set; }

    public ulong TotalQuantity { get; private set; }

    public ulong TotalEarned { get; private set; }

    public ulong TotalTaxPaid { get; private set; }

    public List<SaleSummaryItem> GetSummaryItems()
    {
        if (this.isDirty)
        {
            this.CalculateSummaryItems();
            this.searchResults = null;
        }

        return this.summaryItems;
    }

    public List<SearchResult> GetSearchResults()
    {
        if (this.searchResults == null || this.isDirty)
        {
            this.searchResults =
                this.GetSummaryItems().Select(c => new SearchResult() { SaleSummaryItem = c }).ToList();
        }

        return this.searchResults;
    }

    public void CalculateSummaryItems()
    {
        var items = this.saleTrackerService.GetSalesHistory(null, null).ToList();
        var characterRetainerMap = this.characterMonitorService.Characters
                                       .Where(c => c.Value.CharacterType == CharacterType.Retainer).ToDictionary(
                                           c => c.Value.CharacterId,
                                           c => c.Value.OwnerId);
        var startDate = this.FromDate;

        if (this.TimeUnit.HasValue && this.TimeValue.HasValue)
        {
            startDate = this.TimeUnit.Value switch
            {
                AllaganLib.Interface.Widgets.TimeUnit.Seconds => (DateTime?)(DateTime.Now -
                                                                             TimeSpan.FromSeconds(
                                                                                 this.TimeValue.Value)),
                AllaganLib.Interface.Widgets.TimeUnit.Minutes => (DateTime?)(DateTime.Now -
                                                                             TimeSpan.FromMinutes(
                                                                                 this.TimeValue.Value)),
                AllaganLib.Interface.Widgets.TimeUnit.Hours => (DateTime?)(DateTime.Now -
                                                                           TimeSpan.FromHours(this.TimeValue.Value)),
                AllaganLib.Interface.Widgets.TimeUnit.Days => (DateTime?)(DateTime.Now -
                                                                          TimeSpan.FromDays(this.TimeValue.Value)),
                AllaganLib.Interface.Widgets.TimeUnit.Months => (DateTime?)(DateTime.Now -
                                                                            TimeSpan.FromDays(
                                                                                this.TimeValue.Value * 30)),
                AllaganLib.Interface.Widgets.TimeUnit.Years => (DateTime?)(DateTime.Now -
                                                                           TimeSpan.FromDays(
                                                                               this.TimeValue.Value * 365)),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        var filteredItems = items.Where(
            item =>
                (!this.ItemId.HasValue || item.ItemId == this.ItemId.Value) &&
                (!this.WorldId.HasValue || item.WorldId == this.WorldId.Value) &&
                (!this.IsHq.HasValue || item.IsHq == this.IsHq.Value) &&
                (!this.OwnerId.HasValue || item.GetOwnerId(characterRetainerMap) == this.OwnerId.Value) &&
                (!this.RetainerId.HasValue || item.RetainerId == this.RetainerId.Value) &&
                (!startDate.HasValue || item.SoldAt >= startDate.Value) &&
                (!this.ToDate.HasValue || item.SoldAt <= this.ToDate.Value)).ToList();

        this.summaryItems = filteredItems
                            .GroupBy(
                                item =>
                                {
                                    return SaleSummaryKey.FromSoldItem(item, this.GroupBy, characterRetainerMap);
                                })
                            .Select(
                                g => new SaleSummaryItem(
                                    g.Key,
                                    SaleSummaryKey.FromSoldItem(
                                        g.FirstOrDefault()!,
                                        this.GroupBy,
                                        characterRetainerMap))
                                {
                                    ItemId = g.FirstOrDefault()?.ItemId,
                                    WorldId = g.FirstOrDefault()?.WorldId,
                                    IsHq = g.FirstOrDefault()?.IsHq,
                                    OwnerId = g.FirstOrDefault()?.GetOwnerId(characterRetainerMap),
                                    RetainerId = g.FirstOrDefault()?.RetainerId,
                                    Quantity = (uint)g.Sum(i => i.Quantity),
                                    Earned = (uint)g.Sum(i => i.Total),
                                    TaxPaid = (uint)g.Sum(i => i.TotalIncTax - i.Total),
                                })
                            .ToList();

        this.TotalQuantity = (ulong)this.summaryItems.Sum(item => item.Quantity);
        this.TotalEarned = (ulong)this.summaryItems.Sum(item => item.Earned);
        this.TotalTaxPaid = (ulong)this.summaryItems.Sum(item => item.TaxPaid);

        this.isDirty = false;
    }

    public void Dispose()
    {
        this.saleTrackerService.SnapshotCreated -= this.SaleTrackerServiceOnSnapshotCreated;
    }

    public SaleSummaryGroup Get(string key)
    {
        return this.GroupBy;
    }

    public void Set(string key, (TimeUnit, int)? newValue)
    {
        if (newValue == null)
        {
            this.TimeUnit = null;
            this.TimeValue = null;
            this.isDirty = true;
        }
        else
        {
            this.TimeUnit = newValue.Value.Item1;
            this.TimeValue = newValue.Value.Item2;
            this.FromDate = null;
            this.ToDate = null;
            this.isDirty = true;
        }
    }

    public void Set(string key, (DateTime, DateTime)? newValue)
    {
        if (newValue == null)
        {
            this.TimeUnit = null;
            this.TimeValue = null;
            this.FromDate = null;
            this.ToDate = null;
            this.isDirty = true;
        }
        else
        {
            this.FromDate = newValue.Value.Item1;
            this.ToDate = newValue.Value.Item2;
            this.isDirty = true;
        }
    }

    public void Set(string key, SaleSummaryGroup newValue)
    {
        this.GroupBy = newValue;
        this.isDirty = true;
    }

    (DateTime, DateTime)? IConfigurable<(DateTime, DateTime)?>.Get(string key)
    {
        if (this.FromDate == null || this.ToDate == null)
        {
            return null;
        }

        return (this.FromDate.Value, this.ToDate.Value);
    }

    (TimeUnit, int)? IConfigurable<(TimeUnit, int)?>.Get(string key)
    {
        return this.TimeUnit != null && this.TimeValue != null ? (this.TimeUnit.Value, this.TimeValue.Value) : null;
    }

    private void SaleTrackerServiceOnSnapshotCreated()
    {
        this.isDirty = true;
    }

    private void MarkDirty()
    {
        this.isDirty = true;
    }
}
