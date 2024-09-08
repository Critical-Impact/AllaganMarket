using AllaganMarket.Mediator;
using AllaganMarket.Services;
using AllaganMarket.Windows;

using DalaMock.Core.Mocks;
using DalaMock.Host.Mediator;

using ImGuiNET;

namespace AllaganMarketMock;

public class MockWindow : ExtendedWindow
{
    private readonly MockClientState mockClientState;

    public MockWindow(MediatorService mediatorService, ImGuiService imGuiService, MockClientState mockClientState)
        : base(mediatorService, imGuiService, "Mock Window")
    {
        this.mockClientState = mockClientState;
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
