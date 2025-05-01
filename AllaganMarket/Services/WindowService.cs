using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Mediator;

using DalaMock.Host.Factories;
using DalaMock.Host.Mediator;
using DalaMock.Shared.Interfaces;

using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

/// <summary>
/// Handles the drawing of plugin windows.
/// </summary>
public class WindowService(
    MediatorService mediatorService,
    IDalamudPluginInterface pluginInterface,
    IEnumerable<Window> pluginWindows,
    IWindowSystemFactory windowSystemFactory,
    IFileDialogManager fileDialogManager) : IHostedService, IMediatorSubscriber
{
    public MediatorService MediatorService { get; } = mediatorService;

    public IDalamudPluginInterface PluginInterface { get; } = pluginInterface;

    public IEnumerable<Window> PluginWindows { get; } = pluginWindows;

    public IFileDialogManager FileDialogManager { get; } = fileDialogManager;

    public IWindowSystem WindowSystem { get; } = windowSystemFactory.Create("AllaganMarket");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var pluginWindow in this.PluginWindows)
        {
            this.WindowSystem.AddWindow(pluginWindow);
        }

        this.PluginInterface.UiBuilder.Draw += this.UiBuilderOnDraw;

        this.MediatorService.Subscribe(this, new Action<ToggleWindowMessage>(this.ToggleWindow));
        this.MediatorService.Subscribe(this, new Action<OpenWindowMessage>(this.OpenWindow));
        this.MediatorService.Subscribe(this, new Action<CloseWindowMessage>(this.CloseWindow));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.PluginInterface.UiBuilder.Draw -= this.UiBuilderOnDraw;
        this.WindowSystem.RemoveAllWindows();
        this.MediatorService.UnsubscribeAll(this);
        return Task.CompletedTask;
    }

    private void CloseWindow(CloseWindowMessage obj)
    {
        var window = this.PluginWindows.FirstOrDefault(c => c.GetType() == obj.WindowType);
        if (window != null)
        {
            window.IsOpen = false;
        }
    }

    private void OpenWindow(OpenWindowMessage obj)
    {
        var window = this.PluginWindows.FirstOrDefault(c => c.GetType() == obj.WindowType);
        if (window != null)
        {
            window.IsOpen = true;
        }
    }

    private void ToggleWindow(ToggleWindowMessage obj)
    {
        var window = this.PluginWindows.FirstOrDefault(c => c.GetType() == obj.WindowType);
        window?.Toggle();
    }

    private void UiBuilderOnDraw()
    {
        this.WindowSystem.Draw();
        this.FileDialogManager.Draw();
    }
}
