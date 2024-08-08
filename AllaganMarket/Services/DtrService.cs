using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Models;
using AllaganMarket.Settings;
using AllaganMarket.Windows;

using DalaMock.Host.Mediator;

using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class DtrService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly SaleTrackerService saleTrackerService;
    private readonly MediatorService mediatorService;
    private readonly AddDtrBarEntrySetting addDtrBarEntrySetting;
    private readonly Configuration configuration;
    private readonly IDtrBar dtrBar;
    private IDtrBarEntry? dtrBarEntry;
    private string barTitle = "Allagan Market(Undercuts)";

    public DtrService(
        IDtrBar dtrBar,
        SaleTrackerService saleTrackerService,
        MediatorService mediatorService,
        AddDtrBarEntrySetting addDtrBarEntrySetting,
        Configuration configuration,
        IPluginLog pluginLog)
        : base(pluginLog, mediatorService)
    {
        this.dtrBar = dtrBar;
        this.saleTrackerService = saleTrackerService;
        this.mediatorService = mediatorService;
        this.addDtrBarEntrySetting = addDtrBarEntrySetting;
        this.configuration = configuration;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.saleTrackerService.SnapshotCreated += this.SaleTrackerServiceOnSnapshotCreated;
        this.MediatorService.Subscribe<ConfigurationModifiedMessage>(this, (_) => this.ConfigurationManagerServiceOnConfigurationChanged());
        this.ConfigurationManagerServiceOnConfigurationChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.saleTrackerService.SnapshotCreated -= this.SaleTrackerServiceOnSnapshotCreated;
        this.RemoveDtrBar();
        return Task.CompletedTask;
    }

    private void AddDtrBar()
    {
        if (this.dtrBarEntry == null)
        {
            this.dtrBarEntry = this.dtrBar.Get(this.barTitle);
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
            this.dtrBar.Remove(this.barTitle);
            this.dtrBarEntry = null;
        }
    }

    private void SetDtrBar()
    {
        if (this.dtrBarEntry != null)
        {
            var underCuts = this.saleTrackerService.GetSales(null, null).Count(c => c.UndercutBy != null);
            this.dtrBarEntry.Shown = underCuts != 0;
            var text = "undercuts";
            if (underCuts == 1)
            {
                text = "undercut";
            }

            this.dtrBarEntry.Text = new SeStringBuilder().AddText($"{underCuts} {text}").BuiltString;
            this.dtrBarEntry.Tooltip = new SeStringBuilder().AddText($"You currently have {underCuts} undercut items").BuiltString;
        }
    }



    private void ConfigurationManagerServiceOnConfigurationChanged()
    {
        if (this.addDtrBarEntrySetting.CurrentValue(this.configuration))
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
