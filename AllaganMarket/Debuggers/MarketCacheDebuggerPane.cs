using System;
using System.Collections.Generic;
using System.Globalization;

using AllaganLib.Shared.Interfaces;

using AllaganMarket.Models;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AllaganMarket.Debuggers;

public class MarketCacheDebuggerPane : IDebugPane
{
    private readonly Configuration configuration;

    private string searchItemId = string.Empty;
    private string searchWorldId = string.Empty;
    private string searchType = string.Empty;
    private string searchUnitCost = string.Empty;
    private string searchOwnPrice = string.Empty;

    public MarketCacheDebuggerPane(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public string Name => "Market Cache Debugger";

    public void Draw()
    {
        // --- Search bar row ---
        ImGui.Text("Filters:");
        ImGui.SameLine();
        ImGui.InputText("ItemId", ref this.searchItemId, 32);
        ImGui.SameLine();
        ImGui.InputText("WorldId", ref this.searchWorldId, 32);
        ImGui.SameLine();
        ImGui.InputText("Type", ref this.searchType, 32);
        ImGui.SameLine();
        ImGui.InputText("UnitCost", ref this.searchUnitCost, 32);
        ImGui.SameLine();
        ImGui.InputText("OwnPrice", ref this.searchOwnPrice, 32);

        var data = this.CollectFiltered();

        // --- Table ---
        using var table = ImRaii.Table("##marketcachetable",7,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp);

        if (table.Success)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("ItemId");
            ImGui.TableSetupColumn("IsHq");
            ImGui.TableSetupColumn("WorldId");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("LastUpdated");
            ImGui.TableSetupColumn("UnitCost");
            ImGui.TableSetupColumn("OwnPrice");
            ImGui.TableHeadersRow();

            var clipper = new ImGuiListClipper();
            clipper.Begin(data.Count);

            while (clipper.Step())
            {
                for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    var row = data[i];
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(row.ItemId.ToString(CultureInfo.InvariantCulture));

                    ImGui.TableNextColumn();
                    ImGui.Text(row.IsHq ? "HQ" : "NQ");

                    ImGui.TableNextColumn();
                    ImGui.Text(row.WorldId.ToString(CultureInfo.InvariantCulture));

                    ImGui.TableNextColumn();
                    ImGui.Text(row.GetFormattedType());

                    ImGui.TableNextColumn();
                    ImGui.Text(row.LastUpdated.ToString("u", CultureInfo.InvariantCulture));

                    ImGui.TableNextColumn();
                    ImGui.Text(row.UnitCost.ToString(CultureInfo.InvariantCulture));

                    ImGui.TableNextColumn();
                    ImGui.Text(row.OwnPrice ? "Y" : "N");
                }
            }

            clipper.End();
        }
    }

    private List<MarketPriceCache> CollectFiltered()
    {
        var list = new List<MarketPriceCache>();

        foreach (var worldPair in this.configuration.MarketPriceCache)
        {
            foreach (var entry in worldPair.Value.Values)
            {
                if (!string.IsNullOrEmpty(this.searchItemId) &&
                    !entry.ItemId.ToString(CultureInfo.InvariantCulture).Contains(this.searchItemId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(this.searchWorldId) &&
                    !entry.WorldId.ToString(CultureInfo.InvariantCulture).Contains(this.searchWorldId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(this.searchType) &&
                    !entry.GetFormattedType().Contains(this.searchType, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(this.searchUnitCost) &&
                    !entry.UnitCost.ToString(CultureInfo.InvariantCulture).Contains(this.searchUnitCost, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(this.searchOwnPrice))
                {
                    var match = entry.OwnPrice ? "Y" : "N";
                    if (!match.Contains(this.searchOwnPrice, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                list.Add(entry);
            }
        }

        return list;
    }
}
