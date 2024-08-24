using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Models;
using AllaganMarket.Windows;

using Dalamud.Plugin;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class InstallerWindowService(
    IDalamudPluginInterface pluginInterface,
    ConfigWindow configWindow,
    MainWindow mainWindow,
    PluginStateService pluginStateService) : IHostedService
{
    public IDalamudPluginInterface PluginInterface { get; } = pluginInterface;

    public ConfigWindow ConfigWindow { get; } = configWindow;

    public MainWindow MainWindow { get; } = mainWindow;

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
        pluginStateService.ShowWindows = !pluginStateService.ShowWindows;
        this.MainWindow.Toggle();
    }

    private void ToggleConfigUi()
    {
        pluginStateService.ShowWindows = !pluginStateService.ShowWindows;
        this.ConfigWindow.Toggle();
    }
}
