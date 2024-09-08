using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Mediator;
using AllaganMarket.Settings;
using AllaganMarket.Windows;

using DalaMock.Host.Mediator;

using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class DtrService(
    IDtrBar dtrBar,
    SaleTrackerService saleTrackerService,
    MediatorService mediatorService,
    AddDtrBarEntrySetting addDtrBarEntrySetting,
    UndercutService undercutService,
    Configuration configuration,
    IPluginLog pluginLog) : DisposableMediatorSubscriberBase(pluginLog, mediatorService), IHostedService
{
    private readonly MediatorService mediatorService = mediatorService;
    private readonly UndercutService undercutService = undercutService;
    private readonly string barTitle = "Allagan Market(Undercuts)";
    private IDtrBarEntry? dtrBarEntry;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        saleTrackerService.SnapshotCreated += this.SaleTrackerServiceOnSnapshotCreated;
        this.MediatorService.Subscribe<ConfigurationModifiedMessage>(
            this,
            (_) => this.ConfigurationManagerServiceOnConfigurationChanged());
        this.ConfigurationManagerServiceOnConfigurationChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        saleTrackerService.SnapshotCreated -= this.SaleTrackerServiceOnSnapshotCreated;
        this.RemoveDtrBar();
        return Task.CompletedTask;
    }

    private void AddDtrBar()
    {
        if (this.dtrBarEntry == null)
        {
            this.dtrBarEntry = dtrBar.Get(this.barTitle);
            this.dtrBarEntry.OnClick = this.DtrClicked;
        }
    }

    private void DtrClicked()
    {
        this.mediatorService.Publish(new OpenWindowMessage(typeof(MainWindow)));
    }

    private void RemoveDtrBar()
    {
        if (this.dtrBarEntry != null)
        {
            dtrBar.Remove(this.barTitle);
            this.dtrBarEntry = null;
        }
    }

    private void SetDtrBar()
    {
        if (this.dtrBarEntry != null)
        {
            var underCuts = saleTrackerService.GetSales(null, null).Count(c => this.undercutService.IsItemUndercut(c) ?? false);
            this.dtrBarEntry.Shown = underCuts != 0;
            var text = "undercuts";
            if (underCuts == 1)
            {
                text = "undercut";
            }

            this.dtrBarEntry.Text = new SeStringBuilder().AddText($"{underCuts} {text}").BuiltString;
            this.dtrBarEntry.Tooltip = new SeStringBuilder().AddText($"You currently have {underCuts} undercut items")
                                                            .BuiltString;
        }
    }

    private void ConfigurationManagerServiceOnConfigurationChanged()
    {
        if (addDtrBarEntrySetting.CurrentValue(configuration))
        {
            this.AddDtrBar();
            this.SetDtrBar();
        }
        else
        {
            this.RemoveDtrBar();
        }
    }

    private void SaleTrackerServiceOnSnapshotCreated()
    {
        this.SetDtrBar();
    }
}
