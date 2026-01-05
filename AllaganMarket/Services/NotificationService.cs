using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class NotificationService : IHostedService
{
    private readonly UndercutService undercutService;
    private readonly SaleTrackerService saleTrackerService;
    private readonly IChatGui chatGui;
    private readonly Configuration configuration;
    private readonly ChatNotifySoldItemSetting notifySoldItemSetting;
    private readonly ChatNotifySoldItemChatTypeSetting notifySoldItemChatTypeSetting;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly NumberFormatInfo gilNumberFormat;
    private readonly IClientState clientState;
    private readonly ChatNotifyUndercutGroupingSetting notifyUndercutGroupingSetting;
    private readonly ChatNotifyUndercutCharacterSetting notifyUndercutLoginCharacterSetting;
    private readonly ChatNotifyUndercutSetting notifyUndercutSetting;
    private readonly ChatNotifyUndercutLoginSetting notifyUndercutLoginSetting;
    private readonly ChatNotifyUndercutLoginChatTypeSetting notifyUndercutLoginChatTypeSetting;
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly IRetainerService retainerService;
    private readonly IPlayerState playerState;
    private readonly Subject<(ulong RetainerId, uint ItemId)> undercutQueue = new();

    public NotificationService(
        UndercutService undercutService,
        SaleTrackerService saleTrackerService,
        IChatGui chatGui,
        Configuration configuration,
        ExcelSheet<Item> itemSheet,
        NumberFormatInfo gilNumberFormat,
        IClientState clientState,
        ChatNotifySoldItemSetting notifySoldItemSetting,
        ChatNotifySoldItemChatTypeSetting notifySoldItemChatTypeSetting,
        ChatNotifyUndercutGroupingSetting notifyUndercutGroupingSetting,
        ChatNotifyUndercutCharacterSetting notifyUndercutLoginCharacterSetting,
        ChatNotifyUndercutSetting notifyUndercutSetting,
        ChatNotifyUndercutLoginSetting notifyUndercutLoginSetting,
        ChatNotifyUndercutLoginChatTypeSetting notifyUndercutLoginChatTypeSetting,
        ICharacterMonitorService characterMonitorService,
        IRetainerService retainerService,
        IPlayerState playerState)
    {
        this.undercutService = undercutService;
        this.saleTrackerService = saleTrackerService;
        this.chatGui = chatGui;
        this.configuration = configuration;
        this.notifySoldItemSetting = notifySoldItemSetting;
        this.notifySoldItemChatTypeSetting = notifySoldItemChatTypeSetting;
        this.itemSheet = itemSheet;
        this.gilNumberFormat = gilNumberFormat;
        this.clientState = clientState;
        this.notifyUndercutGroupingSetting = notifyUndercutGroupingSetting;
        this.notifyUndercutLoginCharacterSetting = notifyUndercutLoginCharacterSetting;
        this.notifyUndercutSetting = notifyUndercutSetting;
        this.notifyUndercutLoginSetting = notifyUndercutLoginSetting;
        this.notifyUndercutLoginChatTypeSetting = notifyUndercutLoginChatTypeSetting;
        this.characterMonitorService = characterMonitorService;
        this.retainerService = retainerService;
        this.playerState = playerState;
        this.undercutQueue
            .Buffer(() => this.undercutQueue.Throttle(TimeSpan.FromSeconds(2)))
            .Subscribe(
                undercutMessages =>
                {
                    if (undercutMessages.Any())
                    {
                        this.ProcessUndercuts(undercutMessages);
                    }
                });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.saleTrackerService.ItemSold += this.ItemSold;
        this.clientState.Login += this.ClientStateOnLogin;
        this.undercutService.ItemUndercut += this.QueueUndercut;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.saleTrackerService.ItemSold -= this.ItemSold;
        this.clientState.Login -= this.ClientStateOnLogin;
        this.undercutService.ItemUndercut -= this.QueueUndercut;
        return Task.CompletedTask;
    }

    public void QueueUndercut(ulong retainerId, uint itemId)
    {
        var message = (retainerId, itemId);
        this.undercutQueue.OnNext(message);
    }

    private void ProcessUndercuts(IList<(ulong RetainerId, uint ItemId)> undercuts)
    {
        if (this.notifyUndercutSetting.CurrentValue(this.configuration) && this.retainerService.RetainerId == 0)
        {
            var characterSetting = this.notifyUndercutLoginCharacterSetting.CurrentValue(this.configuration);
            ulong? characterId = null;
            if (characterSetting == ChatNotifyCharacterEnum.OnlyActiveCharacter)
            {
                characterId = this.playerState.ContentId;
            }

            var undercutHashSet = undercuts.Distinct().ToHashSet();
            var undercutSales = this.saleTrackerService.GetSales(characterId, null).Where(c => undercutHashSet.Contains((c.RetainerId, c.ItemId))).ToList();
            this.PrintUndercuts(undercutSales);
        }
    }

    private void ClientStateOnLogin()
    {
        if (this.notifyUndercutLoginSetting.CurrentValue(this.configuration))
        {
            var currentCharacterId = this.playerState.ContentId;
            var currentSales = this.saleTrackerService.GetSales(currentCharacterId, null);
            List<SaleItem> undercutItems = [];
            foreach (var currentSale in currentSales)
            {
                if (this.undercutService.IsItemUndercut(currentSale) == true)
                {
                    undercutItems.Add(currentSale);
                }
            }

            this.PrintUndercuts(undercutItems);
        }
    }

    private void PrintUndercuts(List<SaleItem> undercutItems)
    {
        undercutItems = undercutItems.Where(c => this.undercutService.IsItemUndercut(c) ?? false).ToList();

        if (undercutItems.Count != 0)
        {
            var groupingSetting = this.notifyUndercutGroupingSetting.CurrentValue(this.configuration);
            var chatType = this.notifyUndercutLoginChatTypeSetting.CurrentValue(this.configuration);
            if (groupingSetting == ChatNotifyUndercutGrouping.Individual)
            {
                foreach (var undercutItem in undercutItems)
                {
                    var item = this.itemSheet.GetRowOrDefault(undercutItem.ItemId);
                    var undercutAmount = this.undercutService.GetUndercutBy(undercutItem);
                    if (undercutAmount != null && undercutAmount != 0 && item != null)
                    {
                        this.SendMessage(
                            chatType,
                            $"You have been undercut by {undercutAmount.Value.ToString("C", this.gilNumberFormat)} for {item.Value.Singular.ExtractText()}");
                    }
                }
            }
            else if (groupingSetting == ChatNotifyUndercutGrouping.Together)
            {
                this.SendMessage(
                    chatType,
                    $"You have been undercut on {undercutItems.Count} items.");
            }
            else if (groupingSetting == ChatNotifyUndercutGrouping.GroupByItem)
            {
                foreach (var itemGroup in undercutItems.GroupBy(c => c.ItemId))
                {
                    uint totalUndercutAmount = 0;
                    foreach (var undercutItem in itemGroup)
                    {
                        var undercutAmount = this.undercutService.GetUndercutBy(undercutItem);
                        if (undercutAmount != null && undercutAmount != 0)
                        {
                            totalUndercutAmount = undercutAmount.Value;
                            break;
                        }
                    }

                    var item = this.itemSheet.GetRowOrDefault(itemGroup.Key);
                    if (item != null && totalUndercutAmount != 0)
                    {
                        this.SendMessage(
                            chatType,
                            $"You have been undercut by {totalUndercutAmount.ToString("C", this.gilNumberFormat)} on {itemGroup.Count()} {item.Value.Singular.ExtractText()} you are selling.");
                    }
                }
            }
            else if (groupingSetting == ChatNotifyUndercutGrouping.GroupByRetainer)
            {
                foreach (var itemGroup in undercutItems.GroupBy(c => c.RetainerId))
                {
                    var retainer = this.characterMonitorService.GetCharacterById(itemGroup.Key);
                    if (retainer != null)
                    {
                        this.SendMessage(
                            chatType,
                            $"You have been undercut on {itemGroup.Count()} items that {retainer.Name} is selling.");
                    }
                }
            }
        }
    }

    private void SendMessage(XivChatType chatType, string message)
    {
        var chatEntry = new XivChatEntry();
        chatEntry.Message = new SeStringBuilder().AddText(message)
                                                 .BuiltString;
        chatEntry.Type = chatType;
        this.chatGui.Print(chatEntry);
    }

    private void ItemSold(SaleItem saleItem, SoldItem soldItem)
    {
        if (this.notifySoldItemSetting.CurrentValue(this.configuration))
        {
            var item = this.itemSheet.GetRowOrDefault(soldItem.ItemId);
            if (item != null)
            {
                var chatEntry = new XivChatEntry();
                chatEntry.Message = new SeStringBuilder().AddText(
                                                             $"You sold {soldItem.Quantity} {item.Value.Name.ExtractText()} for {soldItem.TotalIncTax.ToString("C", this.gilNumberFormat)}")
                                                         .BuiltString;
                chatEntry.Type = this.notifySoldItemChatTypeSetting.CurrentValue(this.configuration);
                this.chatGui.Print(chatEntry);
            }
        }
    }
}
