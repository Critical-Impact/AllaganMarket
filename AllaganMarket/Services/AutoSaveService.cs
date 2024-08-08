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
    private readonly IFramework framework;
    private readonly MediatorService mediatorService;
    private TimeSpan defaultSaveTime = TimeSpan.FromSeconds(10);
    private DateTime? nextSaveTime;
    private bool pluginLoaded;

    public AutoSaveService(
        Configuration configuration,
        IFramework framework,
        MediatorService mediatorService)
    {
        this.configuration = configuration;
        this.framework = framework;
        this.mediatorService = mediatorService;
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
            if (this.nextSaveTime == null || this.nextSaveTime < DateTime.Now)
            {
                this.nextSaveTime = DateTime.Now.Add(this.defaultSaveTime);
                //do something
            }
        }
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.framework.Update -= this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    public MediatorService MediatorService { get; set; }
}
