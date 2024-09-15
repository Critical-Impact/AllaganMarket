using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Extensions;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Colors;
using Dalamud.Memory;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text.ReadOnly;

using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic.CompilerServices;

namespace AllaganMarket.Services;

public class HighlightingService : IHostedService
{
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IRetainerService retainerService;
    private readonly UndercutService undercutService;
    private readonly SaleTrackerService saleTrackerService;
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly IClientState clientState;
    private readonly ItemUpdatePeriodSetting updatePeriodSetting;
    private readonly Configuration configuration;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly HighlightingRetainerListSetting retainerListSetting;
    private readonly HighlightingRetainerSellListSetting retainerSellListSetting;
    private readonly IGameGui gameGui;
    private bool retainerListModified;
    private bool retainerSellListModified;
    private ByteColor? originalRetainerListColor;
    private ByteColor? originalRetainerSellListColor;

    public HighlightingService(
        IAddonLifecycle addonLifecycle,
        IRetainerService retainerService,
        UndercutService undercutService,
        SaleTrackerService saleTrackerService,
        ICharacterMonitorService characterMonitorService,
        IClientState clientState,
        ItemUpdatePeriodSetting updatePeriodSetting,
        Configuration configuration,
        ExcelSheet<Item> itemSheet,
        HighlightingRetainerListSetting retainerListSetting,
        HighlightingRetainerSellListSetting retainerSellListSetting,
        IGameGui gameGui)
    {
        this.addonLifecycle = addonLifecycle;
        this.retainerService = retainerService;
        this.undercutService = undercutService;
        this.saleTrackerService = saleTrackerService;
        this.characterMonitorService = characterMonitorService;
        this.clientState = clientState;
        this.updatePeriodSetting = updatePeriodSetting;
        this.configuration = configuration;
        this.itemSheet = itemSheet;
        this.retainerListSetting = retainerListSetting;
        this.retainerSellListSetting = retainerSellListSetting;
        this.gameGui = gameGui;
    }

    public static ByteColor ColorFromVector4(Vector4 hexString)
    {
        return new ByteColor { R = (byte)(hexString.X * 0xFF), B = (byte)(hexString.Z * 0xFF), G = (byte)(hexString.Y * 0xFF), A = (byte)(hexString.W * 0xFF) };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.addonLifecycle.RegisterListener(AddonEvent.PostDraw, "RetainerSellList", this.RetainerSellListDraw);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSellList", this.RetainerSellListFinalize);
        this.addonLifecycle.RegisterListener(AddonEvent.PostDraw, "RetainerList", this.RetainerListDraw);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerList", this.RetainerListFinalize);
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ItemSearchResult", this.ItemSearchClosed);
        return Task.CompletedTask;
    }

    private void RetainerSellListFinalize(AddonEvent type, AddonArgs args)
    {
        this.retainerSellListModified = false;
    }

    private void RetainerListFinalize(AddonEvent type, AddonArgs args)
    {
        this.retainerListModified = false;
    }

    private unsafe void RetainerListDraw(AddonEvent type, AddonArgs args)
    {
        if (args.Addon != IntPtr.Zero)
        {
            var addon = (AtkUnitBase*)args.Addon;
            if (addon != null)
            {
                var isEnabled = this.retainerListSetting.CurrentValue(this.configuration);

                if (!isEnabled && !this.retainerListModified)
                {
                    return;
                }

                var interval = this.updatePeriodSetting.CurrentValue(this.configuration);
                var componentList = addon->GetComponentListById(27);
                if (componentList != null)
                {
                    var retainers = this.characterMonitorService.GetRetainers(this.clientState.LocalContentId)
                                        .OrderBy(c => c.DisplayOrder).ToArray();

                    foreach (var index in Enumerable.Range(0, componentList->ListLength))
                    {
                        var listItemRenderer = componentList->ItemRendererList[index].AtkComponentListItemRenderer;
                        if (listItemRenderer is null)
                        {
                            continue;
                        }

                        var sellingTextNode = (AtkTextNode*)listItemRenderer->GetTextNodeById(11);
                        if (sellingTextNode is null)
                        {
                            continue;
                        }

                        if (listItemRenderer->ListItemIndex < 0 || listItemRenderer->ListItemIndex >= retainers.Length)
                        {
                            continue;
                        }

                        var retainer = retainers[listItemRenderer->ListItemIndex];

                        var undercutItems = this.saleTrackerService.SaleItems[retainer.CharacterId]
                                                .Where(c => this.undercutService.IsItemUndercut(c) ?? false).ToList();

                        var isUndercut = undercutItems.Any();

                        var needsUpdate = this.saleTrackerService.SaleItems[retainer.CharacterId]
                                              .Any(c => !c.IsEmpty() && this.undercutService.NeedsUpdate(c, interval));

                        this.originalRetainerListColor ??= sellingTextNode->TextColor;

                        if (!isEnabled)
                        {
                            this.retainerListModified = false;
                            sellingTextNode->SetText(sellingTextNode->OriginalTextPointer);
                            sellingTextNode->TextColor = this.originalRetainerListColor.Value;
                        }
                        else if (isUndercut)
                        {
                            this.retainerListModified = true;
                            sellingTextNode->TextColor = ColorFromVector4(ImGuiColors.DalamudRed);
                            if (!sellingTextNode->NodeText.ToString().Contains("("))
                            {
                                sellingTextNode->NodeText.Append(
                                    Utf8String.FromString(" (" + undercutItems.Count + ")"));
                            }
                        }
                        else if (needsUpdate)
                        {
                            this.retainerListModified = true;
                            sellingTextNode->TextColor = ColorFromVector4(ImGuiColors.DalamudYellow);
                        }
                    }
                }
            }
        }
    }

    private void ItemSearchClosed(AddonEvent type, AddonArgs args)
    {
        var retainerSellList = this.gameGui.GetAddonByName("RetainerSellList");
        this.DrawRetainerSellList(retainerSellList);
    }

    private unsafe void RetainerSellListDraw(AddonEvent type, AddonArgs args)
    {
        var addonPtr = args.Addon;
        this.DrawRetainerSellList(addonPtr);
    }

    private unsafe void DrawRetainerSellList(IntPtr addonPtr)
    {
        if (this.retainerService.RetainerId != 0)
        {
            if (addonPtr != IntPtr.Zero)
            {
                var addon = (AtkUnitBase*)addonPtr;
                if (addon != null)
                {
                    var isEnabled = this.retainerSellListSetting.CurrentValue(this.configuration);

                    if (!isEnabled && !this.retainerSellListModified)
                    {
                        return;
                    }

                    var interval = this.updatePeriodSetting.CurrentValue(this.configuration);
                    var componentList = addon->GetComponentListById(11);
                    if (componentList != null)
                    {
                        var retainerSales = this.saleTrackerService.GetRetainerSales(this.retainerService.RetainerId);
                        if (retainerSales == null)
                        {
                            return;
                        }

                        var saleItems = retainerSales.Where(c => !c.IsEmpty()).SortByRetainerMarketOrder(this.itemSheet).ToArray();

                        foreach (var index in Enumerable.Range(0, componentList->ListLength))
                        {
                            var listItemRenderer = componentList->ItemRendererList[index].AtkComponentListItemRenderer;

                            if (listItemRenderer is null)
                            {
                                continue;
                            }

                            var sellingTextNode = (AtkTextNode*)listItemRenderer->GetTextNodeById(3);
                            if (sellingTextNode is null)
                            {
                                continue;
                            }

                            if (listItemRenderer->ListItemIndex < 0 || listItemRenderer->ListItemIndex >= saleItems.Length)
                            {
                                continue;
                            }

                            var saleItem = saleItems[listItemRenderer->ListItemIndex];

                            var isUndercut = this.undercutService.IsItemUndercut(saleItem) ?? false;

                            var needsUpdate = !saleItem.IsEmpty() && this.undercutService.NeedsUpdate(saleItem, interval);

                            this.originalRetainerSellListColor ??= sellingTextNode->TextColor;

                            if (!isEnabled)
                            {
                                this.retainerSellListModified = false;
                                sellingTextNode->SetText(sellingTextNode->OriginalTextPointer);
                                sellingTextNode->TextColor = this.originalRetainerSellListColor.Value;
                            }
                            else
                            {
                                if (isUndercut || needsUpdate)
                                {
                                    this.retainerSellListModified = true;
                                    var seString = MemoryHelper.ReadSeStringNullTerminated(
                                        (IntPtr)sellingTextNode->NodeText.StringPtr);
                                    var newText = string.Join(
                                        " ",
                                        seString.Payloads.OfType<TextPayload>().Select(c => c.Text ?? string.Empty));
                                    sellingTextNode->SetText(newText);
                                }

                                if (isUndercut)
                                {
                                    sellingTextNode->TextColor = ColorFromVector4(ImGuiColors.DalamudRed);
                                }
                                else if (needsUpdate)
                                {
                                    sellingTextNode->TextColor = ColorFromVector4(ImGuiColors.DalamudYellow);
                                }
                                else
                                {
                                    sellingTextNode->SetText(sellingTextNode->OriginalTextPointer);
                                    sellingTextNode->TextColor = this.originalRetainerSellListColor.Value;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostDraw, "RetainerSellList", this.RetainerSellListDraw);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerSellList", this.RetainerSellListFinalize);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostDraw, "RetainerList", this.RetainerListDraw);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerList", this.RetainerListFinalize);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "ItemSearchResult", this.ItemSearchClosed);
        return Task.CompletedTask;
    }
}
