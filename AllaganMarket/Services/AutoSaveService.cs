using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganLib.Data.Service;

using AllaganMarket.Models;

using DalaMock.Host.Mediator;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class AutoSaveService : IHostedService, IMediatorSubscriber
{
    private readonly Configuration configuration;
    private readonly ConfigurationLoaderService configurationLoaderService;
    private readonly IFramework framework;
    private readonly MediatorService mediatorService;
    private readonly IPluginLog pluginLog;
    private bool pluginLoaded;

    public AutoSaveService(
        Configuration configuration,
        ConfigurationLoaderService configurationLoaderService,
        IFramework framework,
        MediatorService mediatorService,
        IPluginLog pluginLog)
    {
        this.configuration = configuration;
        this.configurationLoaderService = configurationLoaderService;
        this.framework = framework;
        this.mediatorService = mediatorService;
        this.pluginLog = pluginLog;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.mediatorService.Subscribe<PluginLoadedMessage>(this, this.PluginLoaded);
        this.framework.Update += this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    private void PluginLoaded(PluginLoadedMessage obj)
    {
        this.pluginLoaded = true;
    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        if (!this.pluginLoaded)
        {
            return;
        }

        if (this.configuration.IsDirty)
        {
            this.pluginLog.Verbose("Configuration is dirty, saving.");
            this.configurationLoaderService.Save();
            this.mediatorService.Publish(new ConfigurationModifiedMessage());
        }
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.framework.Update -= this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    public MediatorService MediatorService { get; set; }
}
