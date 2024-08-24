using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Mediator;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class AutoSaveService(
    Configuration configuration,
    ConfigurationLoaderService configurationLoaderService,
    IFramework framework,
    MediatorService mediatorService,
    IPluginLog pluginLog) : DisposableMediatorSubscriberBase(pluginLog, mediatorService), IHostedService
{
    private readonly IPluginLog pluginLog = pluginLog;
    private bool pluginLoaded;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.MediatorService.Subscribe<PluginLoadedMessage>(this, this.PluginLoaded);
        framework.Update += this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        framework.Update -= this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    private void PluginLoaded(PluginLoadedMessage obj)
    {
        this.pluginLoaded = true;
    }

    private void FrameworkOnUpdate(IFramework fWork)
    {
        if (!this.pluginLoaded)
        {
            return;
        }

        if (configuration.IsDirty)
        {
            this.pluginLog.Verbose("Configuration is dirty, saving.");
            configurationLoaderService.Save();
            this.MediatorService.Publish(new ConfigurationModifiedMessage());
        }
    }
}
