using System.Threading;
using System.Threading.Tasks;

using AllaganLib.Interface.Wizard;

using AllaganMarket.Mediator;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Windows;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

/// <summary>
/// Handles plugin bootup and teardown.
/// </summary>
public class PluginBootService(
    Configuration configuration,
    ICharacterMonitorService characterMonitorService,
    SaleTrackerService saleTrackerService,
    MediatorService mediatorService,
    WizardWindow wizardWindow,
    IConfigurationWizardService<Configuration> configurationWizardService,
    IClientState clientState) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        characterMonitorService.LoadExistingData(configuration.Characters);
        saleTrackerService.LoadExistingData(
            configuration.SaleItems,
            configuration.Gil,
            configuration.Sales);
        clientState.Login += this.ClientLoggedIn;
        if (clientState.IsLoggedIn)
        {
            this.ClientLoggedIn();
        }

        mediatorService.Publish(new PluginLoadedMessage());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        clientState.Login -= this.ClientLoggedIn;
        configuration.Characters = characterMonitorService.Characters;
        configuration.SaleItems = saleTrackerService.SaleItems;
        configuration.Gil = saleTrackerService.Gil;
        configuration.Sales = saleTrackerService.Sales;
        return Task.CompletedTask;
    }

    private void ClientLoggedIn()
    {
        if (configurationWizardService.ShouldShowWizard || !configurationWizardService.ConfiguredOnce)
        {
            wizardWindow.IsOpen = true;
        }
    }
}
