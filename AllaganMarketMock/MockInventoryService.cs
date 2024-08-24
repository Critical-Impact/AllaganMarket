using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace AllaganMarketMock;

public class MockInventoryService : IInventoryService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public event IInventoryService.RetainerInventoryLoadedDelegate? OnRetainerInventoryLoaded;

    public unsafe InventoryContainer* GetInventoryContainer(InventoryType inventoryType)
    {
        return null;
    }

    public unsafe InventoryItem* GetInventorySlot(InventoryType inventoryType, int index)
    {
        if (inventoryType == InventoryType.DamagedGear)
        {
            var item = default(InventoryItem);
            item.ItemId = 28992;
            item.Slot = (short)index;
            var itemPtr = &item;
            return itemPtr;
        }

        return null;
    }

    public int GetInventoryItemCount(
        uint itemId,
        bool isHq = false,
        bool checkEquipped = true,
        bool checkArmory = true,
        short minCollectability = 0)
    {
        return 0;
    }

    public int GetItemCountInContainer(
        uint itemId,
        InventoryType inventoryType,
        bool isHq = false,
        short minCollectability = 0)
    {
        return 0;
    }

    public short? GetNextFreeSlot(InventoryType inventoryType)
    {
        return null;
    }

    public bool HasSeenInventory(uint inventoryType)
    {
        return false;
    }
}
