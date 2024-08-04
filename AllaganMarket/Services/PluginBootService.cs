using AllaganMarket.Models;

namespace AllaganMarket.Services;

using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Handles plugin bootup and teardown
/// </summary>
public class PluginBootService : IHostedService
{
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly Configuration configuration;
    private readonly SaleTrackerService saleTrackerService;
    private readonly MediatorService mediatorService;

    public PluginBootService(
        Configuration configuration,
        ICharacterMonitorService characterMonitorService,
        SaleTrackerService saleTrackerService,
        MediatorService mediatorService)
    {
        this.configuration = configuration;
        this.characterMonitorService = characterMonitorService;
        this.saleTrackerService = saleTrackerService;
        this.mediatorService = mediatorService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.characterMonitorService.LoadExistingData(this.configuration.Characters);
        this.saleTrackerService.LoadExistingData(
            this.configuration.SaleItems,
            this.configuration.Gil,
            this.configuration.Sales);
        this.mediatorService.Publish(new PluginLoaded());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.configuration.Characters = this.characterMonitorService.Characters;
        this.configuration.SaleItems = this.saleTrackerService.SaleItems;
        this.configuration.Gil = this.saleTrackerService.Gil;
        this.configuration.Sales = this.saleTrackerService.Sales;
        this.configuration.Save();
        return Task.CompletedTask;
    }
}
