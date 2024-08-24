// Copyright (c) PlaceholderCompany. All rights reserved.

using AllaganMarket.Mediator;
using AllaganMarket.Services;
using AllaganMarket.Windows;

using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarketMock;

public class MockWindow : ExtendedWindow
{
    public MockWindow(MediatorService mediatorService, ImGuiService imGuiService)
        : base(mediatorService, imGuiService, "Mock Window")
    {
        this.IsOpen = true;
    }

    public override void Draw()
    {
        if (ImGui.Button("Open Retainer List Overlay"))
        {
            this.MediatorService.Publish(new OpenWindowMessage(typeof(RetainerListOverlayWindow)));
        }

        if (ImGui.Button("Open Retainer Sell List Overlay"))
        {
            this.MediatorService.Publish(new OpenWindowMessage(typeof(RetainerSellListOverlayWindow)));
        }

        if (ImGui.Button("Open Retainer Sell Overlay"))
        {
            this.MediatorService.Publish(new OpenWindowMessage(typeof(RetainerSellOverlayWindow)));
        }
    }
}
