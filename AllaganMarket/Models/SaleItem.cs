namespace AllaganMarket.Models;

using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using GameInterop;
using Interfaces;

public class SaleItem : IDebuggable, IEquatable<SaleItem>
{
    public SaleItem(InventoryItem inventoryItem, RetainerMarketItemPrice? retainerMarketItemPrice, ulong retainerId, uint worldId)
    {
        this.RetainerId = retainerId;
        this.WorldId = worldId;
        this.ItemId = inventoryItem.ItemId;
        this.IsHq = inventoryItem.Flags.HasFlag(InventoryItem.ItemFlags.HighQuality);
        this.Quantity = inventoryItem.Quantity;
        this.UnitPrice = retainerMarketItemPrice?.UnitPrice ?? 0;
        this.ListedAt = DateTime.Now;
        this.UpdatedAt = DateTime.Now;
    }

    public SaleItem(InventoryItem inventoryItem, uint unitPrice, ulong retainerId, uint worldId)
    {
        this.RetainerId = retainerId;
        this.WorldId = worldId;
        this.ItemId = inventoryItem.ItemId;
        this.IsHq = inventoryItem.Flags.HasFlag(InventoryItem.ItemFlags.HighQuality);
        this.Quantity = inventoryItem.Quantity;
        this.UnitPrice = unitPrice;
        this.ListedAt = DateTime.Now;
        this.UpdatedAt = DateTime.Now;
    }

    public SaleItem(ulong retainerId, uint worldId, uint itemId, bool isHq, uint quantity, uint unitPrice)
    {
        this.RetainerId = retainerId;
        this.WorldId = worldId;
        this.ItemId = itemId;
        this.IsHq = isHq;
        this.Quantity = quantity;
        this.UnitPrice = unitPrice;
        this.ListedAt = DateTime.Now;
        this.UpdatedAt = DateTime.Now;
    }

    public SaleItem(ulong retainerId, uint worldId)
    {
        this.RetainerId = retainerId;
        this.WorldId = worldId;
        this.ItemId = 0;
        this.IsHq = false;
        this.Quantity = 0;
        this.UnitPrice = 0;
        this.ListedAt = DateTime.Now;
        this.UpdatedAt = DateTime.Now;
    }

    public SaleItem(ulong retainerId)
    {
        this.RetainerId = retainerId;
        this.ListedAt = DateTime.Now;
        this.UpdatedAt = DateTime.Now;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleItem"/> class.
    /// Do not remove, used for JSON construction.
    /// </summary>
    public SaleItem()
    {
        this.ListedAt = DateTime.Now;
        this.UpdatedAt = DateTime.Now;
    }

    public ulong RetainerId { get; set; }

    public uint WorldId { get; set; }

    public uint ItemId { get; set; }

    public bool IsHq { get; set; }

    public uint Quantity { get; set; }

    public uint UnitPrice { get; set; }

    public uint? UndercutBy { get; set; }

    public DateTime ListedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsEmpty()
    {
        return this.ItemId == 0;
    }

    public uint Total => this.Quantity * this.UnitPrice;

    public string AsDebugString()
    {
        return
            $"Retainer ID: {this.RetainerId}, World ID: {this.WorldId}, Item ID: {this.ItemId}, Is HQ: {this.IsHq}, Quantity: {this.Quantity}, Unit Price: {this.UnitPrice}";
    }

    public bool Equals(SaleItem other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.RetainerId == other.RetainerId && this.WorldId == other.WorldId && this.ItemId == other.ItemId && this.IsHq == other.IsHq &&
               this.Quantity == other.Quantity && this.UnitPrice == other.UnitPrice;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return this.Equals((SaleItem)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.RetainerId, this.WorldId, this.ItemId, this.IsHq, this.Quantity, this.UnitPrice);
    }
}
