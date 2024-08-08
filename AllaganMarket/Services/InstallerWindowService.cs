using AllaganMarket.Models;

namespace AllaganMarket.Services;

using System.Threading;
using System.Threading.Tasks;
using Windows;
using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;

public class InstallerWindowService : IHostedService
{
    private readonly PluginStateService pluginStateService;

    public InstallerWindowService(
        IDalamudPluginInterface pluginInterface,
        ConfigWindow configWindow,
        MainWindow mainWindow,
        PluginStateService pluginStateService
        )
    {
        this.pluginStateService = pluginStateService;
        this.PluginInterface = pluginInterface;
        this.ConfigWindow = configWindow;
        this.MainWindow = mainWindow;
    }

    public IDalamudPluginInterface PluginInterface { get; }

    public ConfigWindow ConfigWindow { get; }

    public MainWindow MainWindow { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUi;
        this.PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUi;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        this.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUi;
        return Task.CompletedTask;
    }

    private void ToggleMainUi()
    {
        this.pluginStateService.ShowWindows = !this.pluginStateService.ShowWindows;
        this.MainWindow.Toggle();
    }

    private void ToggleConfigUi()
    {
        this.pluginStateService.ShowWindows = !this.pluginStateService.ShowWindows;
        this.ConfigWindow.Toggle();
    }
}
