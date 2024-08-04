using AllaganLib.Universalis.Models;
using AllaganLib.Universalis.Models.Bson;
using AllaganLib.Universalis.Services;

using AllaganMarket.Extensions;
using AllaganMarket.Interfaces;
using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;

using DalaMock.Host.Mediator;

using Dalamud.Game.Network.Structures;

namespace AllaganMarket.Services;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Keeps track of which items have been undercut.
/// Connects to universalis's websocket for live updates
/// Connects to universalis's API to get an initial read on what the prices of items are
/// Listens for marketboard events to see if they've been undercut when viewing the marketboad listing
/// </summary>
public class UndercutService : IHostedService, IMediatorSubscriber
{
    private readonly UniversalisWebsocketService websocketService;
    private readonly MediatorService mediatorService;
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly IMarketBoard marketBoard;
    private readonly UniversalisApiService universalisApiService;
    private readonly SaleTrackerService saleTrackerService;
    private readonly IPluginLog pluginLog;
    private readonly IChatGui chatGui;
    private readonly IClientState clientState;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly NumberFormatInfo gilNumberFormat;
    private readonly Configuration configuration;
    private readonly ChatNotifyUndercutSetting chatNotifyUndercutSetting;
    private readonly ChatNotifyUndercutLoginCharacterSetting chatNotifyUndercutLoginSetting;
    private Dictionary<ulong, Dictionary<uint, uint>> queuedUndercuts = new();
    private uint activeHomeWorld;

    public UndercutService(
        UniversalisWebsocketService websocketService,
        MediatorService mediatorService,
        ICharacterMonitorService characterMonitorService,
        IMarketBoard marketBoard,
        UniversalisApiService universalisApiService,
        SaleTrackerService saleTrackerService,
        IPluginLog pluginLog,
        IChatGui chatGui,
        IClientState clientState,
        ExcelSheet<Item> itemSheet,
        NumberFormatInfo gilNumberFormat,
        Configuration configuration,
        ChatNotifyUndercutSetting chatNotifyUndercutSetting,
        ChatNotifyUndercutLoginCharacterSetting chatNotifyUndercutLoginSetting)
    {
        this.websocketService = websocketService;
        this.mediatorService = mediatorService;
        this.characterMonitorService = characterMonitorService;
        this.marketBoard = marketBoard;
        this.universalisApiService = universalisApiService;
        this.saleTrackerService = saleTrackerService;
        this.pluginLog = pluginLog;
        this.chatGui = chatGui;
        this.clientState = clientState;
        this.itemSheet = itemSheet;
        this.gilNumberFormat = gilNumberFormat;
        this.configuration = configuration;
        this.chatNotifyUndercutSetting = chatNotifyUndercutSetting;
        this.chatNotifyUndercutLoginSetting = chatNotifyUndercutLoginSetting;
        this.mediatorService.Subscribe<PluginLoaded>(this, this.PluginLoaded);
    }

    private void PluginLoaded(PluginLoaded obj)
    {
        this.pluginLog.Verbose("Plugin has loaded, performing an initial scan of undercuts.");
        var allSales = this.saleTrackerService.GetSales(null, null);
        var toSearch = allSales.Select(c => (c.ItemId, c.WorldId)).Distinct();
        foreach (var item in toSearch)
        {
            this.universalisApiService.QueuePriceCheck(item.ItemId, item.WorldId);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login += this.OnLogin;
        this.clientState.Logout += this.OnLogout;
        this.websocketService.OnUniversalisEvent += this.OnUniversalisMessage;
        this.marketBoard.OfferingsReceived += this.OfferingsReceived;
        this.universalisApiService.PriceRetrieved += this.UniversalisApiPriceRetrieved;

        // Check to see if they are logged in already
        this.OnLogin();
        return Task.CompletedTask;
    }

    private void UniversalisApiPriceRetrieved(uint itemId, uint worldId, UniversalisPricing response)
    {
        var salesByItem = this.saleTrackerService.GetSales(null, worldId).GroupBy(c => c.ItemId).ToDictionary(c => c.Key, c => c.ToList());
        if (salesByItem.TryGetValue(itemId, out var currentSales))
        {
            foreach (var saleItem in currentSales)
            {
                uint lowestPrice;
                if (saleItem.IsHq)
                {
                    lowestPrice = (uint)response.MinPriceHq;
                }
                else
                {
                    lowestPrice = (uint)response.MinPriceNq;
                }

                if (lowestPrice == 0)
                {
                    this.pluginLog.Verbose("Received a price that was 0, skipping.");
                    continue;
                }


                if (lowestPrice < saleItem.UnitPrice)
                {
                    var undercutAmount = (uint?)(saleItem.UnitPrice - lowestPrice);
                    saleItem.UndercutBy = undercutAmount;
                    this.configuration.IsDirty = true;

                    if (this.clientState.IsLoggedIn)
                    {
                        this.PrintUndercutMessage(itemId, undercutAmount.Value);
                    }
                    else
                    {
                        this.QueueUndercutMessage(saleItem.RetainerId, itemId, undercutAmount.Value);
                    }
                }
            }
        }
    }

    private void SendQueuedUndercutMessages(ulong characterId)
    {
        var loginSetting = this.chatNotifyUndercutLoginSetting.CurrentValue(this.configuration);
        var ownedRetainers = this.characterMonitorService.GetOwnedCharacters(characterId, CharacterType.Retainer).ToDictionary(c => c.CharacterId);

        foreach (var retainer in this.queuedUndercuts)
        {
            if (loginSetting == ChatNotifyCharacterEnum.AllCharacters || ownedRetainers.ContainsKey(retainer.Key))
            {
                var messages = retainer.Value;
                foreach (var message in messages)
                {
                    this.PrintUndercutMessage(message.Key, message.Value);
                }

                retainer.Value.Clear();
            }
        }
    }

    private void QueueUndercutMessage(ulong retainerId, uint itemId, uint undercutAmount)
    {
        this.queuedUndercuts.TryAdd(retainerId, new Dictionary<uint, uint>());
        this.queuedUndercuts[retainerId].TryAdd(itemId, undercutAmount);
    }

    private void PrintUndercutMessage(uint itemId, uint undercutAmount)
    {
        if (!this.chatNotifyUndercutSetting.CurrentValue(this.configuration))
        {
            return;
        }

        var item = this.itemSheet.GetRow(itemId);
        if (item != null)
        {
            this.chatGui.Print(
                $"You have been undercut by {undercutAmount.ToString("C", this.gilNumberFormat)} for {item.Singular.AsReadOnly().ExtractText()}");
        }
    }

    /// <summary>
    /// If we are logged in, retrieve our active sales for the world we are in, find the lowest offering from a retainer we don't own
    /// </summary>
    /// <param name="offerings"></param>
    private void OfferingsReceived(IMarketBoardCurrentOfferings offerings)
    {
        var currentPlayer = this.clientState.LocalPlayer;
        if (currentPlayer != null)
        {
            if (offerings.ItemListings.Count == 0)
            {
                return;
            }

            var lowestOffering = offerings.ItemListings.Where(c => !this.characterMonitorService.IsCharacterKnown(c.RetainerId))
                     .Min(c => c.PricePerUnit);
            var itemId = offerings.ItemListings.First().ItemId;
            var item = this.itemSheet.GetRow(itemId);
            var salesByItem = this.saleTrackerService.GetSales(null, currentPlayer.HomeWorld.Id).GroupBy(c => c.ItemId).ToDictionary(c => c.Key, c => c.ToList());
            if (salesByItem.TryGetValue(itemId, out var currentSales))
            {
                foreach (var saleItem in currentSales)
                {
                    if (lowestOffering < saleItem.UnitPrice)
                    {
                        var undercutAmount = (uint?)(saleItem.UnitPrice - lowestOffering);
                        saleItem.UndercutBy = undercutAmount;
                        this.configuration.IsDirty = true;

                        if (item != null)
                        {
                            this.chatGui.Print(
                                $"You have been undercut by {undercutAmount.Value.ToString("C", this.gilNumberFormat)} for {item.Singular.AsReadOnly().ExtractText()}");
                        }
                    }
                }
            }
        }
    }

    private void OnLogout()
    {
        if (this.activeHomeWorld != 0)
        {
            this.websocketService.UnsubscribeFromChannel(
                UniversalisWebsocketService.EventType.ListingsAdd,
                this.activeHomeWorld);
            this.activeHomeWorld = 0;
        }
    }

    private void OnLogin()
    {
        if (this.clientState.LocalPlayer != null)
        {
            this.websocketService.SubscribeToChannel(
                UniversalisWebsocketService.EventType.ListingsAdd,
                this.clientState.LocalPlayer.HomeWorld.Id);
            this.activeHomeWorld = this.clientState.LocalPlayer.HomeWorld.Id;
        }

        this.SendQueuedUndercutMessages(this.clientState.LocalContentId);
    }

    private void OnUniversalisMessage(SubscriptionReceivedMessage message)
    {
        this.pluginLog.Verbose(message.ToDebugString());
        if (message.EventType == UniversalisWebsocketService.EventType.ListingsAdd)
        {
            var itemId = message.Item;
            if (this.saleTrackerService.SaleItemsByItemId.TryGetValue(itemId, out var value))
            {
                // TODO: Compare against known retainer names per world so you don't get a message saying you've undercut yourself
                HashSet<string> retainerNames = new();
                var ourCheapestPrice = value.Where(saleItem => message.World == saleItem.WorldId).DefaultIfEmpty(null).Min(c => c?.UnitPrice ?? 0);
                var theirCheapestPrice = message.Listings.Where(c => !retainerNames.Contains(c.RetainerName))
                    .DefaultIfEmpty(null).Min(c => c?.PricePerUnit ?? 0);
                if (theirCheapestPrice < ourCheapestPrice)
                {
                    var undercutAmount = (uint?)(theirCheapestPrice - ourCheapestPrice);
                    var item = this.itemSheet.GetRow(itemId);

                    foreach (var saleItem in value)
                    {
                        saleItem.UndercutBy = undercutAmount;
                        this.configuration.IsDirty = true;
                    }

                    if (item != null)
                    {
                        this.chatGui.Print(
                            $"You have been undercut by {undercutAmount.Value.ToString("C", this.gilNumberFormat)} for {item.Singular.AsReadOnly().ExtractText()}");
                    }
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login -= this.OnLogin;
        this.clientState.Logout -= this.OnLogout;
        this.websocketService.OnUniversalisEvent -= this.OnUniversalisMessage;
        this.marketBoard.OfferingsReceived -= this.OfferingsReceived;
        this.universalisApiService.PriceRetrieved -= this.UniversalisApiPriceRetrieved;
        return Task.CompletedTask;
    }

    public MediatorService MediatorService => this.mediatorService;
}
