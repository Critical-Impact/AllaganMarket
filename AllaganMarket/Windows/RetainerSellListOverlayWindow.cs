using System.Linq;

using AllaganMarket.Extensions;
using AllaganMarket.Mediator;
using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;

using DalaMock.Host.Mediator;
using DalaMock.Shared.Interfaces;

using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AllaganMarket.Windows;

public class RetainerSellListOverlayWindow : OverlayWindow
{
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly SaleTrackerService saleTrackerService;
    private readonly IClientState clientState;
    private readonly Configuration configuration;
    private readonly ItemUpdatePeriodSetting updatePeriodSetting;
    private readonly IFont font;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly RetainerOverlayCollapsedSetting overlayCollapsedSetting;
    private readonly ShowRetainerOverlaySetting retainerOverlaySetting;
    private readonly RetainerMarketService retainerMarketService;
    private bool showAllItems;

    public RetainerSellListOverlayWindow(
        IAddonLifecycle addonLifecycle,
        IGameGui gameGui,
        IPluginLog logger,
        MediatorService mediator,
        ImGuiService imGuiService,
        ICharacterMonitorService characterMonitorService,
        SaleTrackerService saleTrackerService,
        IClientState clientState,
        Configuration configuration,
        ItemUpdatePeriodSetting updatePeriodSetting,
        IFont font,
        ExcelSheet<Item> itemSheet,
        RetainerOverlayCollapsedSetting overlayCollapsedSetting,
        ShowRetainerOverlaySetting retainerOverlaySetting,
        RetainerMarketService retainerMarketService)
        : base(addonLifecycle, gameGui, logger, mediator, imGuiService, "Retainer Sell List Overlay")
    {
        this.characterMonitorService = characterMonitorService;
        this.saleTrackerService = saleTrackerService;
        this.clientState = clientState;
        this.configuration = configuration;
        this.updatePeriodSetting = updatePeriodSetting;
        this.font = font;
        this.itemSheet = itemSheet;
        this.overlayCollapsedSetting = overlayCollapsedSetting;
        this.retainerOverlaySetting = retainerOverlaySetting;
        this.retainerMarketService = retainerMarketService;
        this.AttachAddon("RetainerSellList", AttachPosition.Right);
        this.Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;
        this.RespectCloseHotkey = false;
    }

    public bool IsCollapsed
    {
        get => this.overlayCollapsedSetting.CurrentValue(this.configuration);

        set => this.overlayCollapsedSetting.UpdateFilterConfiguration(this.configuration, value);
    }

    public override bool DrawConditions()
    {
        return this.retainerOverlaySetting.CurrentValue(this.configuration) && base.DrawConditions();
    }

    public override void PreOpenCheck()
    {
        base.PreOpenCheck();
        if (this.characterMonitorService.ActiveRetainerId == 0 && this.IsOpen)
        {
            this.IsOpen = false;
        }
    }

    public override void Draw()
    {
        var collapsed = this.IsCollapsed;

        var currentCursorPosX = ImGui.GetCursorPosX();

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
            this.MediatorService.Publish(new ToggleWindowMessage(typeof(ConfigWindow)));
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

        ImGui.SameLine();

        if (ImGuiService.DrawIconButton(
                this.font,
                FontAwesomeIcon.Eye,
                ref currentCursorPosX,
                "Show/hide items that need to be updated.",
                true,
                this.showAllItems ? null : ImGuiColors.ParsedGrey))
        {
            this.showAllItems = !this.showAllItems;
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
        if (this.clientState.IsLoggedIn && activeRetainer != null)
        {
            var saleItems = this.saleTrackerService.GetRetainerSales(activeRetainer.CharacterId)
                                ?.Where(c => !c.IsEmpty()).SortByRetainerMarketOrder(this.itemSheet).ToList();
            var interval = this.updatePeriodSetting.CurrentValue(this.configuration);
            var itemsToCheck = false;

            using (ImRaii.Table("ItemList", 5, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 100);
                ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.None, 40);
                ImGui.TableSetupColumn("Rec. Price", ImGuiTableColumnFlags.None, 80);
                ImGui.TableSetupColumn("Undercut", ImGuiTableColumnFlags.None, 70);
                ImGui.TableSetupColumn("Needs Update?", ImGuiTableColumnFlags.None, 100);
                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                ImGui.TableNextColumn();
                ImGui.Text("Name");
                ImGui.TableNextColumn();
                ImGui.Text("Slot");
                ImGui.TableNextColumn();
                ImGui.Text("Rec. Price");
                ImGui.TableNextColumn();
                ImGui.Text("Undercut?");
                ImGui.TableNextColumn();
                ImGui.Text("Needs Update?");
                if (saleItems != null)
                {
                    for (var index = 0; index < saleItems.Count; index++)
                    {
                        var saleItem = saleItems[index];
                        var isUnderCut = saleItem.UndercutBy != null;
                        var needsUpdate = saleItem.NeedsUpdate(interval);
                        if (!isUnderCut && !needsUpdate && !this.showAllItems)
                        {
                            continue;
                        }

                        itemsToCheck = true;
                        ImGui.TableNextRow();
                        using var green = ImRaii.PushColor(
                            ImGuiCol.Text,
                            ImGuiColors.HealerGreen,
                            !isUnderCut && !needsUpdate);
                        using var yellow = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow, needsUpdate);
                        using var red = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed, isUnderCut);
                        ImGui.TableNextColumn();
                        ImGui.Text(
                            saleItem.IsEmpty()
                                ? "Empty"
                                : this.itemSheet.GetRow(saleItem.ItemId)?.Name ?? "Unknown Item");
                        green.Pop();
                        yellow.Pop();
                        red.Pop();

                        ImGui.TableNextColumn();
                        ImGui.PushTextWrapPos();
                        ImGui.Text((index + 1).ToString());
                        ImGui.PopTextWrapPos();

                        ImGui.TableNextColumn();
                        ImGui.Text(saleItem.RecommendedUnitPrice().ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(saleItem.IsEmpty() ? "N/A" : isUnderCut ? "Yes" : "No");

                        ImGui.TableNextColumn();
                        var needsUpdateText = needsUpdate ? "Yes" : "No";

                        ImGui.Text(saleItem.IsEmpty() ? "N/A" : needsUpdateText);
                    }
                }
            }

            if (!itemsToCheck)
            {
                ImGui.Text("No items left to update.");
            }
        }
        else
        {
            ImGui.Text("Please login.");
        }
    }
}