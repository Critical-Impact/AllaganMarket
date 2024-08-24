using FFXIVClientStructs.FFXIV.Client.Game;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services.Interfaces;

public unsafe interface IInventoryService : IHostedService
{
    public delegate void RetainerInventoryLoadedDelegate();

    public event RetainerInventoryLoadedDelegate OnRetainerInventoryLoaded;

    public InventoryContainer* GetInventoryContainer(InventoryType inventoryType);

    public InventoryItem* GetInventorySlot(InventoryType inventoryType, int index);

    public int GetInventoryItemCount(
        uint itemId,
        bool isHq = false,
        bool checkEquipped = true,
        bool checkArmory = true,
        short minCollectability = 0);

    public int GetItemCountInContainer(
        uint itemId,
        InventoryType inventoryType,
        bool isHq = false,
        short minCollectability = 0);

    short? GetNextFreeSlot(InventoryType inventoryType);

    bool HasSeenInventory(uint inventoryType);
}
