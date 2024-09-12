using System;
using System.Globalization;

using AllaganMarket.Mediator;
using AllaganMarket.Models;
using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;

using DalaMock.Host.Mediator;
using DalaMock.Shared.Interfaces;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Common.Math;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AllaganMarket.Windows;

public class RetainerSellOverlayWindow : OverlayWindow
{
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly SaleTrackerService saleTrackerService;
    private readonly IClientState clientState;
    private readonly Configuration configuration;
    private readonly IFont font;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly RetainerOverlayCollapsedSetting overlayCollapsedSetting;
    private readonly IInventoryService inventoryService;
    private readonly ShowRetainerOverlaySetting retainerOverlaySetting;
    private readonly RetainerMarketService retainerMarketService;
    private readonly UndercutService undercutService;

    public RetainerSellOverlayWindow(
        IAddonLifecycle addonLifecycle,
        IGameGui gameGui,
        IPluginLog logger,
        MediatorService mediator,
        ImGuiService imGuiService,
        ICharacterMonitorService characterMonitorService,
        SaleTrackerService saleTrackerService,
        IClientState clientState,
        Configuration configuration,
        IFont font,
        ExcelSheet<Item> itemSheet,
        RetainerOverlayCollapsedSetting overlayCollapsedSetting,
        IInventoryService inventoryService,
        ShowRetainerOverlaySetting retainerOverlaySetting,
        RetainerMarketService retainerMarketService,
        UndercutService undercutService)
        : base(addonLifecycle, gameGui, logger, mediator, imGuiService, "Retainer Sell Overlay")
    {
        this.characterMonitorService = characterMonitorService;
        this.saleTrackerService = saleTrackerService;
        this.clientState = clientState;
        this.configuration = configuration;
        this.font = font;
        this.itemSheet = itemSheet;
        this.overlayCollapsedSetting = overlayCollapsedSetting;
        this.inventoryService = inventoryService;
        this.retainerOverlaySetting = retainerOverlaySetting;
        this.retainerMarketService = retainerMarketService;
        this.undercutService = undercutService;
        this.AttachAddon("RetainerSell", AttachPosition.Right);
        this.Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;
        this.RespectCloseHotkey = false;
    }

    public bool IsCollapsed
    {
        get => this.overlayCollapsedSetting.CurrentValue(this.configuration);

        set => this.overlayCollapsedSetting.UpdateFilterConfiguration(this.configuration, value);
    }

    public unsafe uint CurrentItemId
    {
        get
        {
            var selectedItem = this.inventoryService.GetInventorySlot(InventoryType.DamagedGear, 0);
            return selectedItem == null ? 0 : selectedItem->ItemId;
        }
    }

    public unsafe InventoryItem.ItemFlags? CurrentItemFlags
    {
        get
        {
            var selectedItem = this.inventoryService.GetInventorySlot(InventoryType.DamagedGear, 0);
            return selectedItem == null ? null : selectedItem->Flags;
        }
    }


    public Item? CurrentItem => this.itemSheet.GetRow(this.CurrentItemId);

    public SaleItem? CurrentSaleItem => this.saleTrackerService.GetSaleItem(
        this.CurrentItemId,
        this.characterMonitorService.ActiveRetainer?.WorldId ?? null);

    public override bool DrawConditions()
    {
        return this.retainerOverlaySetting.CurrentValue(this.configuration) && base.DrawConditions();
    }

    public override void PreOpenCheck()
    {
        base.PreOpenCheck();
        if (this.characterMonitorService.ActiveRetainerId == 0 && this.CurrentItemId != 0 && this.IsOpen)
        {
            this.IsOpen = false;
        }
    }

    public override void Draw()
    {
        var collapsed = this.IsCollapsed;

        var currentCursorPosX = ImGui.GetCursorPosX();

        this.SizeConstraints = new WindowSizeConstraints()
        {
            MaximumSize = new Vector2(340, 800) * ImGui.GetIO().FontGlobalScale,
        };

        if (collapsed && ImGuiService.DrawIconButton(this.font, FontAwesomeIcon.ChevronRight, ref currentCursorPosX))
        {
            this.IsCollapsed = false;
        }

        if (!collapsed && ImGuiService.DrawIconButton(this.font, FontAwesomeIcon.ChevronLeft, ref currentCursorPosX))
        {
            this.IsCollapsed = true;
        }

        if (collapsed)
        {
            return;
        }

        ImGui.SameLine();
        ImGui.Text("Allagan Market");

        ImGui.SameLine();

        currentCursorPosX = ImGui.GetWindowSize().X;

        if (ImGuiService.DrawIconButton(
                this.font,
                FontAwesomeIcon.Bars,
                ref currentCursorPosX,
                "Open the Allagan Market main window.",
                true))
        {
            this.MediatorService.Publish(new ToggleWindowMessage(typeof(MainWindow)));
        }

        ImGui.SameLine();

        if (ImGuiService.DrawIconButton(
                this.font,
                FontAwesomeIcon.Cog,
                ref currentCursorPosX,
                "Open the Allagan Market configuration window.",
                true))
        {
            this.MediatorService.Publish(new ToggleWindowMessage(typeof(ConfigWindow)));
        }

        ImGui.Separator();

        if (this.retainerMarketService.InBadState)
        {
            ImGui.PushTextWrapPos();
            ImGui.Text(
                "The plugin has been reloaded since entering a retainer, please back out and load back into the retainer.");
            ImGui.PopTextWrapPos();
            return;
        }

        var activeRetainer = this.characterMonitorService.ActiveRetainer;
        var currentItem = this.CurrentItem;
        var currentSaleItem = this.CurrentSaleItem;
        if (this.clientState.IsLoggedIn && activeRetainer != null && currentItem != null)
        {
            bool? isHq = null;
            switch (this.CurrentItemFlags)
            {
                case InventoryItem.ItemFlags.None:
                    isHq = false;
                    break;
                case InventoryItem.ItemFlags.HighQuality:
                    isHq = true;
                    break;
                case null:
                    break;
            }

            var recommendedUnitPrice = this.undercutService.GetRecommendedUnitPrice(activeRetainer.WorldId, currentItem.RowId, isHq ?? false);
            var lastUpdated = this.undercutService.GetLastUpdateTime(activeRetainer.WorldId, currentItem.RowId);
            var recommendedPrice = recommendedUnitPrice == null ? "No Data" : recommendedUnitPrice.Value.ToString();

            using (ImRaii.Table("ItemList", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 120 * ImGui.GetIO().FontGlobalScale);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 200 * ImGui.GetIO().FontGlobalScale);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Name: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{currentItem.Name}");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Rec. Unit Price: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{recommendedPrice}");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Updated At: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{lastUpdated?.ToString(CultureInfo.CurrentCulture) ?? "No Data"}");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Listed At: ");
                ImGui.TableNextColumn();
                ImGui.Text($"{currentSaleItem?.ListedAt.ToString(CultureInfo.CurrentCulture) ?? "N/A"}");
            }
        }
        else
        {
            ImGui.Text("Please login.");
        }
    }
}
