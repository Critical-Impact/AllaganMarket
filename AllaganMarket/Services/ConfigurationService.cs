// <copyright file="ConfigurationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Models;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class ConfigurationService : IHostedService, IMediatorSubscriber
{
    private readonly Configuration configuration;
    private readonly IFramework framework;
    private readonly MediatorService mediatorService;
    private TimeSpan defaultSaveTime = TimeSpan.FromSeconds(10);
    private DateTime? nextSaveTime;
    private bool pluginLoaded;

    public ConfigurationService(Configuration configuration, IFramework framework, MediatorService mediatorService)
    {
        this.configuration = configuration;
        this.framework = framework;
        this.mediatorService = mediatorService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.mediatorService.Subscribe<PluginLoaded>(this, this.PluginLoaded);
        this.framework.Update += this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    private void PluginLoaded(PluginLoaded obj)
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
                this.configuration.Save();
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
