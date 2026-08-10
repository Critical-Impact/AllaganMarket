using System;
using System.Collections.Generic;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AllaganMarket.Services;

public interface IAtkOrderService
{
    /// <summary>
    /// Returns a dictionary that maps the inventory slots to menu indexes, if the slot is missing then it can be assumed there is no item in the list
    /// </summary>
    /// <returns>An array of slot IDs</returns>
    unsafe Dictionary<int, int>? GetCurrentOrder();

    /// <summary>
    /// Returns the set of inventory slots whose item is currently not listed for sale (e.g. gear
    /// displayed on an estate mannequin, shown in-game as "&lt;Not for Sale&gt;"). Returns null when
    /// the RetainerSellList addon is not available to read.
    /// </summary>
    /// <returns>The inventory slots that are not for sale, or null if the addon could not be read.</returns>
    unsafe HashSet<int>? GetNotForSaleSlots();
}

public class AtkOrderService : IAtkOrderService
{
    // Each list row occupies a block of 13 AtkValues starting at index 15.
    private const int RowBlockStart = 15;
    private const int RowBlockSize = 13;

    // Offset within a row block: [0] is the inventory slot, [7] is a bool that is true when the item
    // is listed for sale and false when it is "Not for Sale" (verified in-game against mannequin gear).
    private const int SlotOffset = 0;
    private const int ForSaleOffset = 7;

    private readonly IGameGui gameGui;

    public AtkOrderService(IGameGui gameGui)
    {
        this.gameGui = gameGui;
    }

    public unsafe Dictionary<int, int>? GetCurrentOrder()
    {
        var atkValues = this.GetSellListValues();
        if (atkValues == null)
        {
            return null;
        }

        var currentOrder = new Dictionary<int, int>();
        var atkIndex = RowBlockStart;
        for (var i = 0; i < 20; i++)
        {
            if (atkValues[atkIndex].Type == 0)
            {
                continue;
            }

            currentOrder.TryAdd(atkValues[atkIndex + SlotOffset].Int, i);
            atkIndex += RowBlockSize;
        }

        return currentOrder;
    }

    public unsafe HashSet<int>? GetNotForSaleSlots()
    {
        var atkValues = this.GetSellListValues();
        if (atkValues == null)
        {
            return null;
        }

        var notForSale = new HashSet<int>();
        var atkIndex = RowBlockStart;
        for (var i = 0; i < 20; i++)
        {
            if (atkValues[atkIndex].Type == 0)
            {
                continue;
            }

            if (atkValues[atkIndex + ForSaleOffset].Int == 0)
            {
                notForSale.Add(atkValues[atkIndex + SlotOffset].Int);
            }

            atkIndex += RowBlockSize;
        }

        return notForSale;
    }

    private unsafe AtkValue* GetSellListValues()
    {
        var retainerSellListPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (retainerSellListPtr == IntPtr.Zero)
        {
            return null;
        }

        var retainerSellList = (AtkUnitBase*)retainerSellListPtr.Address;
        return retainerSellList->AtkValues;
    }
}
