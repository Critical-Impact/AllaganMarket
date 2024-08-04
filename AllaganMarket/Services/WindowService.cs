using DalaMock.Host.Mediator;

namespace AllaganMarket.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AllaganMarket.Interfaces;
using DalaMock.Host.Factories;
using DalaMock.Shared;
using DalaMock.Shared.Interfaces;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;
using Models;

/// <summary>
/// Handles the drawing of plugin windows
/// </summary>
public class WindowService : IHostedService, IMediatorSubscriber, IDisposable
{
    public WindowService(MediatorService mediatorService,
        IDalamudPluginInterface pluginInterface,
        IEnumerable<Window> pluginWindows,
        IWindowSystemFactory windowSystemFactory)
    {
        this.MediatorService = mediatorService;
        this.PluginInterface = pluginInterface;
        this.PluginWindows = pluginWindows;
        this.WindowSystem = windowSystemFactory.Create("AllaganMarket");
    }

    public MediatorService MediatorService { get; }

    public IDalamudPluginInterface PluginInterface { get; }

    public IEnumerable<Window> PluginWindows { get; }

    public IWindowSystem WindowSystem { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var pluginWindow in this.PluginWindows)
        {
            this.WindowSystem.AddWindow(pluginWindow);
        }

        this.PluginInterface.UiBuilder.Draw += this.UiBuilderOnDraw;

        this.MediatorService.Subscribe(this, new Action<ToggleWindow>(this.ToggleWindow));
        this.MediatorService.Subscribe(this, new Action<OpenWindow>(this.OpenWindow));
        this.MediatorService.Subscribe(this, new Action<CloseWindow>(this.CloseWindow));

        return Task.CompletedTask;
    }

    private void CloseWindow(CloseWindow obj)
    {
        var window = this.PluginWindows.FirstOrDefault(c => c.GetType() == obj.WindowType);
        if (window != null)
        {
            window.IsOpen = false;
        }
    }

    private void OpenWindow(OpenWindow obj)
    {
        var window = this.PluginWindows.FirstOrDefault(c => c.GetType() == obj.WindowType);
        if (window != null)
        {
            window.IsOpen = true;
        }
    }

    private void ToggleWindow(ToggleWindow obj)
    {
        var window = this.PluginWindows.FirstOrDefault(c => c.GetType() == obj.WindowType);
        window?.Toggle();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.PluginInterface.UiBuilder.Draw -= this.UiBuilderOnDraw;
        this.WindowSystem.RemoveAllWindows();
        return Task.CompletedTask;
    }

    private void UiBuilderOnDraw()
    {
        this.WindowSystem.Draw();
    }

    public void Dispose()
    {
        this.MediatorService.UnsubscribeAll(this);
    }
}