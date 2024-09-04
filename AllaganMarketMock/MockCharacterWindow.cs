using AllaganMarket.Models;
using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Windows;

using DalaMock.Core.Mocks;
using DalaMock.Host.Mediator;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

namespace AllaganMarketMock;

public class MockCharacterWindow(
    ICharacterMonitorService characterMonitorService,
    MockClientState mockClientState,
    MockRetainerService mockRetainerService,
    MediatorService mediatorService,
    ImGuiService imGuiService) : ExtendedWindow(mediatorService, imGuiService, "Mock Character Window")
{
    public override void Draw()
    {
        using (var combo = ImRaii.Combo(
                   "Selected Character",
                   characterMonitorService.ActiveCharacter?.Name ?? "None"))
        {
            if (combo)
            {
                if (ImGui.Selectable("None", characterMonitorService.ActiveCharacterId == 0))
                {
                    mockClientState.LocalContentId = 0;
                    mockClientState.IsLoggedIn = false;
                }

                foreach (var character in characterMonitorService.GetCharactersByType(
                             CharacterType.Character,
                             null))
                {
                    if (ImGui.Selectable(
                            character.Name + "##" + character.CharacterId,
                            characterMonitorService.ActiveCharacterId == character.CharacterId))
                    {
                        mockClientState.LocalContentId = character.CharacterId;
                        mockClientState.IsLoggedIn = true;
                    }
                }
            }
        }

        using (var combo = ImRaii.Combo(
                   "Selected Retainer",
                   characterMonitorService.ActiveRetainer?.Name ?? "None"))
        {
            if (combo)
            {
                if (ImGui.Selectable("None", mockRetainerService.RetainerId == 0))
                {
                    mockRetainerService.RetainerId = 0;
                }

                foreach (var character in
                         characterMonitorService.GetCharactersByType(CharacterType.Retainer, null))
                {
                    if (ImGui.Selectable(
                            character.Name,
                            characterMonitorService.ActiveRetainerId == character.CharacterId))
                    {
                        mockRetainerService.RetainerId = character.CharacterId;
                    }
                }
            }
        }
    }
}
