using System;
using System.Collections.Generic;
using System.Globalization;

using AllaganLib.Shared.Interfaces;

using AllaganMarket.Models;
using AllaganMarket.Services;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AllaganMarket.Debuggers;

public sealed class RetainerMarketServiceDebuggerPane : IDebugPane, IDisposable
{
    private readonly IRetainerMarketService service;

    private readonly List<string> eventLog = new();
    private const int MaxEvents = 100;

    public RetainerMarketServiceDebuggerPane(IRetainerMarketService service)
    {
        this.service = service;

        service.OnItemAdded += this.OnItemEvent;
        service.OnItemRemoved += this.OnItemEvent;
        service.OnItemUpdated += this.OnItemEvent;
        service.OnOpened += this.OnOpened;
        service.OnClosed += this.OnClosed;
    }

    public string Name => "Retainer Market Service";

    public void Dispose()
    {
        this.service.OnItemAdded -= this.OnItemEvent;
        this.service.OnItemRemoved -= this.OnItemEvent;
        this.service.OnItemUpdated -= this.OnItemEvent;
        this.service.OnOpened -= this.OnOpened;
        this.service.OnClosed -= this.OnClosed;
    }

    public void Draw()
    {
        ImGui.Text($"Bad State: {(this.service.InBadState ? "YES" : "NO")}");
        ImGui.Text("Active Slots:");
        ImGui.SameLine();

        for (short slot = 0; slot < 20; slot++)
        {
            ImGui.SameLine();

            if (this.service.ActiveSlots.Contains(slot))
            {
                ImGui.Text(slot.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                ImGui.TextDisabled(slot.ToString(CultureInfo.InvariantCulture));
            }
        }

        ImGui.Separator();

        ImGui.Text("Sale Items");

        using (var table = ImRaii.Table(
                   "##retainersaleitems",
                   7,
                   ImGuiTableFlags.RowBg |
                   ImGuiTableFlags.Borders |
                   ImGuiTableFlags.Resizable |
                   ImGuiTableFlags.ScrollY |
                   ImGuiTableFlags.SizingStretchProp))
        {
            if (table.Success)
            {
                ImGui.TableSetupScrollFreeze(0, 1);

                ImGui.TableSetupColumn("Slot");
                ImGui.TableSetupColumn("ItemId");
                ImGui.TableSetupColumn("HQ");
                ImGui.TableSetupColumn("Qty");
                ImGui.TableSetupColumn("Unit Price");
                ImGui.TableSetupColumn("Total");
                ImGui.TableSetupColumn("Updated");

                ImGui.TableHeadersRow();

                var items = this.service.SaleItems;
                var clipper = new ImGuiListClipper();
                clipper.Begin(items.Length);

                while (clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var item = items[i];
                        if (item == null || item.IsEmpty())
                        {
                            continue;
                        }

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(item.MenuIndex.ToString(CultureInfo.InvariantCulture));

                        ImGui.TableNextColumn();
                        ImGui.Text(item.ItemId.ToString(CultureInfo.InvariantCulture));

                        ImGui.TableNextColumn();
                        ImGui.Text(item.IsHq ? "HQ" : "NQ");

                        ImGui.TableNextColumn();
                        ImGui.Text(item.Quantity.ToString(CultureInfo.InvariantCulture));

                        ImGui.TableNextColumn();
                        ImGui.Text(item.UnitPrice.ToString(CultureInfo.InvariantCulture));

                        ImGui.TableNextColumn();
                        ImGui.Text(item.Total.ToString(CultureInfo.InvariantCulture));

                        ImGui.TableNextColumn();
                        ImGui.Text(item.UpdatedAt.ToString("u", CultureInfo.InvariantCulture));
                    }
                }

                clipper.End();
            }
        }

        ImGui.Separator();

        ImGui.Text("Recent Events");

        using (var child = ImRaii.Child("##retainermarketevents", new System.Numerics.Vector2(0, 200), true))
        {
            if (child.Success)
            {
                for (int i = this.eventLog.Count - 1; i >= 0; i--)
                {
                    ImGui.TextWrapped(this.eventLog[i]);
                }
            }
        }
    }

    private void OnItemEvent(RetainerMarketListEvent listEvent)
    {
        this.PushEvent(listEvent.AsDebugString());
    }

    private void OnOpened()
    {
        this.PushEvent("Market window opened");
    }

    private void OnClosed()
    {
        this.PushEvent("Market window closed");
    }

    private void PushEvent(string message)
    {
        this.eventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

        if (this.eventLog.Count > MaxEvents)
        {
            this.eventLog.RemoveAt(0);
        }
    }
}
