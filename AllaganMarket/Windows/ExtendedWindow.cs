using DalaMock.Host.Mediator;

namespace AllaganMarket.Windows;

using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

using Services;

public abstract class ExtendedWindow : Window, IMediatorSubscriber, IDisposable
{
    public ImGuiService ImGuiService { get; }

    public ExtendedWindow(MediatorService mediatorService, ImGuiService imGuiService, string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
        : base(name, flags, forceMainWindow)
    {
        this.ImGuiService = imGuiService;
        this.MediatorService = mediatorService;
    }

    public MediatorService MediatorService { get; }

    public void Dispose()
    {
        this.MediatorService.UnsubscribeAll(this);
    }
}
