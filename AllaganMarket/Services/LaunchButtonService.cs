using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Mediator;
using AllaganMarket.Models;
using AllaganMarket.Settings;
using AllaganMarket.Windows;

using DalaMock.Host.Mediator;

using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllaganMarket.Services;

public class LaunchButtonService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly ITitleScreenMenu titleScreenMenu;
    private readonly IDalamudPluginInterface pluginInterfaceService;
    private readonly PluginStateService pluginStateService;
    private readonly Configuration configuration;
    private readonly AddTitleMenuButtonSetting addTitleMenuButtonSetting;
    private readonly ITextureProvider textureProvider;
    private readonly string fileName;
    private IReadOnlyTitleScreenMenuEntry? entry;

    public LaunchButtonService(
        ILogger<LaunchButtonService> pluginLog,
        MediatorService mediatorService,
        ITextureProvider textureProvider,
        ITitleScreenMenu titleScreenMenu,
        IDalamudPluginInterface pluginInterfaceService,
        PluginStateService pluginStateService,
        Configuration configuration,
        AddTitleMenuButtonSetting addTitleMenuButtonSetting)
        : base(pluginLog, mediatorService)
    {
        this.titleScreenMenu = titleScreenMenu;
        this.pluginInterfaceService = pluginInterfaceService;
        this.pluginStateService = pluginStateService;
        this.configuration = configuration;
        this.addTitleMenuButtonSetting = addTitleMenuButtonSetting;
        this.textureProvider = textureProvider;
        var assemblyLocation = pluginInterfaceService.AssemblyLocation.DirectoryName!;
        this.fileName = Path.Combine(assemblyLocation, Path.Combine("Images", "logo_menu.png"));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.MediatorService.Subscribe<ConfigurationModifiedMessage>(
            this,
            (_) => this.ConfigurationManagerServiceOnConfigurationChanged());
        this.ConfigurationManagerServiceOnConfigurationChanged();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        this.RemoveEntry();
    }

    private void CreateEntry()
    {
        if (this.entry != null)
        {
            this.pluginInterfaceService.UiBuilder.Draw -= this.CreateEntry;
            return;
        }

        try
        {
            this.entry = this.titleScreenMenu.AddEntry("Allagan Market", this.textureProvider.GetFromFile(this.fileName), this.OnTriggered);

            this.pluginInterfaceService.UiBuilder.Draw -= this.CreateEntry;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Could not register title screen menu entry");
        }
    }

    private void OnTriggered()
    {
        this.pluginStateService.ShowWindows = !this.pluginStateService.ShowWindows;
        if (this.pluginStateService.ShowWindows)
        {
            this.MediatorService.Publish(new OpenWindowMessage(typeof(MainWindow)));
        }
    }

    private void RemoveEntry()
    {
        this.pluginInterfaceService.UiBuilder.Draw -= this.RemoveEntry;
        if (this.entry != null)
        {
            this.titleScreenMenu.RemoveEntry(this.entry);
            this.entry = null;
        }
    }

    private void ConfigurationManagerServiceOnConfigurationChanged()
    {
        if (this.addTitleMenuButtonSetting.CurrentValue(this.configuration))
        {
            this.pluginInterfaceService.UiBuilder.Draw += this.CreateEntry;
        }
        else
        {
            this.pluginInterfaceService.UiBuilder.Draw += this.RemoveEntry;
        }
    }
}
