using System.Linq;
using System.Net.WebSockets;
using System.Threading;

using AllaganLib.Universalis.Services;

using AllaganMarket.Mediator;
using AllaganMarket.Services;
using AllaganMarket.Windows;

using DalaMock.Core.Mocks;
using DalaMock.Host.Mediator;

using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AllaganMarketMock;

public class MockWindow : ExtendedWindow
{
    private readonly MockClientState mockClientState;
    private readonly UniversalisWebsocketService universalisWebsocketService;
    private readonly ExcelSheet<World> worldSheet;
    private int selectedWorld;

    public MockWindow(MediatorService mediatorService, ImGuiService imGuiService, MockClientState mockClientState, UniversalisWebsocketService universalisWebsocketService, ExcelSheet<World> worldSheet)
        : base(mediatorService, imGuiService, "Mock Window")
    {
        this.mockClientState = mockClientState;
        this.universalisWebsocketService = universalisWebsocketService;
        this.worldSheet = worldSheet;
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

        using (var combo = ImRaii.Combo(
                   "World",
                   this.selectedWorld == 0
                       ? "N/A"
                       : this.worldSheet.GetRow((uint)this.selectedWorld).Name.ExtractText()))
        {
            if (combo)
            {
                foreach (var world in this.worldSheet.Where(c => c.IsPublic).OrderBy(c => c.Name.ExtractText()))
                {
                    if (ImGui.Selectable(world.Name.ExtractText()))
                    {
                        this.selectedWorld = (int)world.RowId;
                    }
                }
            }
        }

        if (ImGui.Button("Subscribe"))
        {
            this.universalisWebsocketService.SubscribeToChannel(UniversalisWebsocketService.EventType.ListingsAdd, (uint)this.selectedWorld);
        }

        if (ImGui.Button("Force Disconnect"))
        {
            this.universalisWebsocketService.Client.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
    }
}
