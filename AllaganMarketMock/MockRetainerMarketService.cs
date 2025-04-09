using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Models;
using AllaganMarket.Services;

namespace AllaganMarketMock;

public class MockRetainerMarketService : IRetainerMarketService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {

    }

    public event RetainerMarketService.ItemEventDelegate? OnItemAdded;

    public event RetainerMarketService.ItemEventDelegate? OnItemRemoved;

    public event RetainerMarketService.ItemEventDelegate? OnItemUpdated;

    public event RetainerMarketService.UpdatedEventDelegate? OnUpdated;

    public event RetainerMarketService.MarketEventDelegate? OnOpened;

    public event RetainerMarketService.MarketEventDelegate? OnClosed;

    public bool InBadState { get; set; }

    public SaleItem?[] SaleItems { get; set; } = [];
}
