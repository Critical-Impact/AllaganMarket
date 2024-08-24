using System;
using System.Collections.Generic;

using AllaganMarket.Extensions;

namespace AllaganMarket.Models;

public class SaleSummaryKey : IEquatable<SaleSummaryKey>
{
    public uint? ItemId { get; set; }

    public uint? WorldId { get; set; }

    public bool? IsHq { get; set; }

    public ulong? OwnerId { get; set; }

    public ulong? RetainerId { get; set; }

    public bool IsGrouped =>
        this.WorldId != null || this.IsHq != null || this.OwnerId != null || this.RetainerId != null;

    public static SaleSummaryKey FromSoldItem(
        SoldItem item,
        SaleSummaryGroup saleSummaryGroup,
        Dictionary<ulong, ulong?> characterRetainerMap)
    {
        var key = new SaleSummaryKey();
        if (saleSummaryGroup == SaleSummaryGroup.None)
        {
            key.ItemId = item.ItemId;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.Item))
        {
            key.ItemId = item.ItemId;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.World))
        {
            key.WorldId = item.WorldId;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.IsHq))
        {
            key.IsHq = item.IsHq;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.Owner))
        {
            key.OwnerId = item.GetOwnerId(characterRetainerMap);
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.Retainer))
        {
            key.RetainerId = item.RetainerId;
        }

        return key;
    }

    public static SaleSummaryKey FromSaleSummary(SaleSummary item, SaleSummaryGroup saleSummaryGroup)
    {
        var key = new SaleSummaryKey();
        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.Item))
        {
            key.ItemId = item.ItemId;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.World))
        {
            key.WorldId = item.WorldId;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.IsHq))
        {
            key.IsHq = item.IsHq;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.Owner))
        {
            key.OwnerId = item.RetainerId;
        }

        if (saleSummaryGroup.HasFlag(SaleSummaryGroup.Retainer))
        {
            key.RetainerId = item.RetainerId;
        }

        return key;
    }

    public bool Equals(SaleSummaryKey? other)
    {
        return this.ItemId == other?.ItemId && this.WorldId == other?.WorldId &&
               this.IsHq == other?.IsHq && this.OwnerId == other?.OwnerId && this.RetainerId == other?.RetainerId;
    }

    public override bool Equals(object? obj)
    {
        return obj is SaleSummaryKey key && this.ItemId == key.ItemId && this.WorldId == key.WorldId &&
               this.IsHq == key.IsHq && this.OwnerId == key.OwnerId && this.RetainerId == key.RetainerId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.ItemId, this.WorldId, this.IsHq, this.OwnerId, this.RetainerId);
    }
}
