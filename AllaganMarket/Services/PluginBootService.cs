using AllaganLib.Interface.Wizard;

using AllaganMarket.Models;
using AllaganMarket.Windows;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

namespace AllaganMarket.Services;

using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Handles plugin bootup and teardown.
/// </summary>
public class PluginBootService : IHostedService
{
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly Configuration configuration;
    private readonly SaleTrackerService saleTrackerService;
    private readonly MediatorService mediatorService;
    private readonly WizardWindow wizardWindow;
    private readonly IConfigurationWizardService<Configuration> configurationWizardService;
    private readonly IClientState clientState;
    private bool WizardOpened { get; set; }

    public PluginBootService(
        Configuration configuration,
        ICharacterMonitorService characterMonitorService,
        SaleTrackerService saleTrackerService,
        MediatorService mediatorService,
        WizardWindow wizardWindow,
        IConfigurationWizardService<Configuration> configurationWizardService,
        IClientState clientState)
    {
        this.configuration = configuration;
        this.characterMonitorService = characterMonitorService;
        this.saleTrackerService = saleTrackerService;
        this.mediatorService = mediatorService;
        this.wizardWindow = wizardWindow;
        this.configurationWizardService = configurationWizardService;
        this.clientState = clientState;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.characterMonitorService.LoadExistingData(this.configuration.Characters);
        this.saleTrackerService.LoadExistingData(
            this.configuration.SaleItems,
            this.configuration.Gil,
            this.configuration.Sales);
        this.clientState.Login += this.ClientLoggedIn;
        if (!this.clientState.IsLoggedIn)
        {
            this.ClientLoggedIn();
        }

        this.mediatorService.Publish(new PluginLoadedMessage());
        return Task.CompletedTask;
    }

    private void ClientLoggedIn()
    {
        if (this.configurationWizardService.ShouldShowWizard)
        {
            this.wizardWindow.IsOpen = true;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login -= this.ClientLoggedIn;
        this.configuration.Characters = this.characterMonitorService.Characters;
        this.configuration.SaleItems = this.saleTrackerService.SaleItems;
        this.configuration.Gil = this.saleTrackerService.Gil;
        this.configuration.Sales = this.saleTrackerService.Sales;
        return Task.CompletedTask;
    }
}
