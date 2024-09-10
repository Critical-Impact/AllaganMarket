using System;
using System.Linq;
using System.Numerics;

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

using Humanizer;

using ImGuiNET;

namespace AllaganMarket.Windows;

public class RetainerListOverlayWindow : OverlayWindow
{
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly SaleTrackerService saleTrackerService;
    private readonly IClientState clientState;
    private readonly Configuration configuration;
    private readonly ItemUpdatePeriodSetting updatePeriodSetting;
    private readonly IFont font;
    private readonly RetainerOverlayCollapsedSetting overlayCollapsedSetting;
    private readonly ShowRetainerOverlaySetting retainerOverlaySetting;
    private readonly UndercutService undercutService;
    private readonly HighlightingRetainerListSetting retainerListSetting;
    private bool showAllRetainers;

    public RetainerListOverlayWindow(
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
        RetainerOverlayCollapsedSetting overlayCollapsedSetting,
        ShowRetainerOverlaySetting retainerOverlaySetting,
        UndercutService undercutService,
        HighlightingRetainerListSetting retainerListSetting)
        : base(addonLifecycle, gameGui, logger, mediator, imGuiService, "Retainer List Overlay")
    {
        this.characterMonitorService = characterMonitorService;
        this.saleTrackerService = saleTrackerService;
        this.clientState = clientState;
        this.configuration = configuration;
        this.updatePeriodSetting = updatePeriodSetting;
        this.font = font;
        this.overlayCollapsedSetting = overlayCollapsedSetting;
        this.retainerOverlaySetting = retainerOverlaySetting;
        this.undercutService = undercutService;
        this.retainerListSetting = retainerListSetting;
        this.AttachAddon("RetainerList", AttachPosition.Right);
        this.Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize |
                     ImGuiWindowFlags.NoSavedSettings;
        this.Size = null;
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
        if (!this.clientState.IsLoggedIn && this.IsOpen)
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
            MaximumSize = new Vector2(360, 800) * ImGui.GetIO().FontGlobalScale,
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

        ImGui.SameLine();

        if (ImGuiService.DrawIconButton(
                this.font,
                FontAwesomeIcon.Eye,
                ref currentCursorPosX,
                "Show/hide retainers that need to be updated.",
                true,
                this.showAllRetainers ? null : ImGuiColors.ParsedGrey))
        {
            this.showAllRetainers = !this.showAllRetainers;
        }

        ImGui.SameLine();

        var retainerHighlighting = this.retainerListSetting.CurrentValue(this.configuration);
        if (ImGuiService.DrawIconButton(
                this.font,
                FontAwesomeIcon.Lightbulb,
                ref currentCursorPosX,
                "Toggle highlighting on the retainer list.",
                true,
                retainerHighlighting ? null : ImGuiColors.ParsedGrey))
        {
            this.retainerListSetting.UpdateFilterConfiguration(this.configuration, !retainerHighlighting);
        }

        ImGui.Separator();

        var activeCharacter = this.characterMonitorService.ActiveCharacter;
        if (this.clientState.IsLoggedIn && activeCharacter != null)
        {
            var retainers = this.characterMonitorService.GetRetainers(activeCharacter.CharacterId)
                                .OrderBy(c => c.DisplayOrder).ToList();
            var interval = this.updatePeriodSetting.CurrentValue(this.configuration);
            var retainersToCheck = false;

            using (ImRaii.Table("RetainerList", 4, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 100 * ImGui.GetIO().FontGlobalScale);
                ImGui.TableSetupColumn("Selling", ImGuiTableColumnFlags.WidthFixed, 50 * ImGui.GetIO().FontGlobalScale);
                ImGui.TableSetupColumn("Undercut", ImGuiTableColumnFlags.WidthFixed, 70 * ImGui.GetIO().FontGlobalScale);
                ImGui.TableSetupColumn("Last Update", ImGuiTableColumnFlags.WidthFixed, 130 * ImGui.GetIO().FontGlobalScale);
                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                ImGui.TableNextColumn();
                ImGui.Text("Name");
                ImGui.TableNextColumn();
                ImGui.Text("Selling");
                ImGui.TableNextColumn();
                ImGui.Text("Undercut?");
                ImGui.TableNextColumn();
                ImGui.Text("Stale Pricing?");
                foreach (var retainer in retainers)
                {
                    var isUnderCut = this.saleTrackerService.SaleItems[retainer.CharacterId]
                                         .Any(c => this.undercutService.IsItemUndercut(c) ?? false);
                    var needsUpdate = this.saleTrackerService.SaleItems[retainer.CharacterId]
                                          .Any(c => !c.IsEmpty() && this.undercutService.NeedsUpdate(c, interval));
                    var nextUpdate = this.saleTrackerService.SaleItems[retainer.CharacterId]
                                         .Where(c => !c.IsEmpty()).DefaultIfEmpty()
                                         .Min(c => c == null ? null : (DateTime?)this.undercutService.NextUpdateDate(c, interval));
                    var sellingCount = this.saleTrackerService.SaleItems[retainer.CharacterId].Count(c => !c.IsEmpty());
                    if (!isUnderCut && !needsUpdate && !this.showAllRetainers)
                    {
                        continue;
                    }

                    retainersToCheck = true;
                    ImGui.TableNextRow();
                    using var green = ImRaii.PushColor(
                        ImGuiCol.Text,
                        ImGuiColors.HealerGreen,
                        !isUnderCut && !needsUpdate);
                    using var yellow = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow, needsUpdate);
                    using var red = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed, isUnderCut);
                    ImGui.TableNextColumn();
                    ImGui.Text(retainer.Name);
                    green.Pop();
                    yellow.Pop();
                    red.Pop();

                    ImGui.TableNextColumn();
                    ImGui.Text(sellingCount.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(isUnderCut ? "Yes" : "No");

                    ImGui.TableNextColumn();
                    var needsUpdateText = needsUpdate ? "Yes" : "No";
                    if (nextUpdate != null)
                    {
                        var timeSpan = nextUpdate - DateTime.Now;
                        needsUpdateText += " (" + timeSpan.Value.Humanize() + ")";
                    }

                    ImGui.Text(needsUpdateText);
                }
            }

            if (!retainersToCheck)
            {
                ImGui.Text("No retainers left to update.");
            }
        }
        else
        {
            ImGui.Text("Please login.");
        }
    }
}
