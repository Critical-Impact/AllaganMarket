using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

using AllaganLib.Shared.Interfaces;

using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AllaganMarket.Debugging;

public unsafe class RetainerDebuggerPane : IDebugPane
{
    private readonly ICharacterMonitorService characterMonitor;

    private string searchStoredName = string.Empty;
    private string searchStoredWorld = string.Empty;
    private string searchActiveName = string.Empty;

    public RetainerDebuggerPane(ICharacterMonitorService characterMonitor)
    {
        this.characterMonitor = characterMonitor;
    }

    public string Name => "Retainer Debugger";

    public void Draw()
    {
        using var child = ImRaii.Child("RetainerDebuggerPaneChild", new System.Numerics.Vector2(0, 0), true);

        if (!child.Success)
        {
            return;
        }

        DrawStoredRetainers();
        ImGui.Separator();
        DrawActiveRetainers();
    }

    private void DrawStoredRetainers()
    {
        ImGui.Text("Stored Retainers (from CharacterMonitorService)");
        ImGui.InputText("Filter Name (Stored)", ref this.searchStoredName, 32);
        ImGui.SameLine();
        ImGui.InputText("Filter WorldId (Stored)", ref this.searchStoredWorld, 32);

        var stored = this.characterMonitor.Characters.Values
                         .Where(c => c.CharacterType == CharacterType.Retainer)
                         .Where(c =>
                                    (string.IsNullOrEmpty(this.searchStoredName) ||
                                     c.Name.Contains(this.searchStoredName, StringComparison.OrdinalIgnoreCase)) &&
                                    (string.IsNullOrEmpty(this.searchStoredWorld) ||
                                     c.WorldId.ToString(CultureInfo.InvariantCulture)
                                      .Contains(this.searchStoredWorld, StringComparison.OrdinalIgnoreCase)))
                         .ToList();

        using var table = ImRaii.Table(
            "StoredRetainersTable",
            8,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp,
            new System.Numerics.Vector2(0, 200));

        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Character Id");
        ImGui.TableSetupColumn("Owner Id");
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("World Id");
        ImGui.TableSetupColumn("ClassJob Id");
        ImGui.TableSetupColumn("Level");
        ImGui.TableSetupColumn("Display Order");
        ImGui.TableSetupColumn("Town");
        ImGui.TableHeadersRow();

        var clipper = new ImGuiListClipper();
        clipper.Begin(stored.Count);

        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var ch = stored[i];

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(ch.CharacterId.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(ch.OwnerId?.ToString(CultureInfo.InvariantCulture) ?? "-");

                ImGui.TableNextColumn();
                ImGui.Text(ch.Name);

                ImGui.TableNextColumn();
                ImGui.Text(ch.WorldId.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(ch.ClassJobId.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(ch.Level.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(ch.DisplayOrder.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(ch.RetainerTown?.ToString() ?? "-");
            }
        }

        clipper.End();
    }

    private void DrawActiveRetainers()
    {
        ImGui.Text("Active Retainers (from RetainerManager)");
        ImGui.InputText("Filter Name (Active)", ref this.searchActiveName, 32);

        var manager = RetainerManager.Instance();
        if (manager == null)
        {
            ImGui.Text("RetainerManager not available.");
            return;
        }

        var span = manager->Retainers;
        var active = new List<RetainerManager.Retainer>();
        for (var i = 0; i < span.Length; i++)
        {
            var r = span[i];
            if (r.RetainerId != 0)
            {
                active.Add(r);
            }
        }

        using var table = ImRaii.Table("ActiveRetainersTable",
                                       11,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp,
            new System.Numerics.Vector2(0, 200));

        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Retainer Id");
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Available");
        ImGui.TableSetupColumn("Class Job");
        ImGui.TableSetupColumn("Level");
        ImGui.TableSetupColumn("Item Count");
        ImGui.TableSetupColumn("Gil");
        ImGui.TableSetupColumn("Town");
        ImGui.TableSetupColumn("Market Item Count");
        ImGui.TableSetupColumn("Venture Id");
        ImGui.TableSetupColumn("Retainer Order");
        ImGui.TableHeadersRow();

        var clipper = new ImGuiListClipper();
        clipper.Begin(active.Count);

        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var r = active[i];
                var name = r.NameString.Trim();

                if (!string.IsNullOrEmpty(this.searchActiveName) &&
                    !name.Contains(this.searchActiveName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(r.RetainerId.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(name);

                ImGui.TableNextColumn();
                ImGui.Text(r.Available ? "Yes" : "No");

                ImGui.TableNextColumn();
                ImGui.Text(r.ClassJob.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(r.Level.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(r.ItemCount.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(r.Gil.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(r.Town.ToString());

                ImGui.TableNextColumn();
                ImGui.Text(r.MarketItemCount.ToString(CultureInfo.InvariantCulture));

                ImGui.TableNextColumn();
                ImGui.Text(r.VentureId.ToString(CultureInfo.InvariantCulture));

                var displayOrder = RetainerManager.Instance()->DisplayOrder.IndexOf((byte)i);
                displayOrder = displayOrder == -1 ? 0 : displayOrder;

                ImGui.TableNextColumn();
                ImGui.Text(displayOrder.ToString());
            }
        }

        clipper.End();
    }
}
