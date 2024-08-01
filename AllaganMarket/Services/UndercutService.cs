using AllaganMarket.Services.Interfaces;

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
using Models.Bson;

public class UndercutService : IHostedService
{
    private readonly UniversalisWebsocketService websocketService;
    private readonly ICharacterMonitorService characterMonitorService;
    private readonly IMarketBoard marketBoard;
    private readonly SaleTrackerService saleTrackerService;
    private readonly IPluginLog pluginLog;
    private readonly IChatGui chatGui;
    private readonly IClientState clientState;
    private readonly ExcelSheet<Item> itemSheet;
    private readonly NumberFormatInfo gilNumberFormat;
    private uint activeHomeWorld;

    public UndercutService(UniversalisWebsocketService websocketService, ICharacterMonitorService characterMonitorService, IMarketBoard marketBoard, SaleTrackerService saleTrackerService, IPluginLog pluginLog, IChatGui chatGui, IClientState clientState, ExcelSheet<Item> itemSheet, NumberFormatInfo gilNumberFormat)
    {
        this.websocketService = websocketService;
        this.characterMonitorService = characterMonitorService;
        this.marketBoard = marketBoard;
        this.saleTrackerService = saleTrackerService;
        this.pluginLog = pluginLog;
        this.chatGui = chatGui;
        this.clientState = clientState;
        this.itemSheet = itemSheet;
        this.gilNumberFormat = gilNumberFormat;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login += this.OnLogin;
        this.clientState.Logout += this.OnLogout;
        this.websocketService.OnUniversalisEvent += this.OnUniversalisMessage;
        this.marketBoard.OfferingsReceived += this.OfferingsReceived;

        // Check to see if they are logged in already
        this.OnLogin();
        return Task.CompletedTask;
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
    }

    private void OnUniversalisMessage(SubscriptionReceivedMessage message)
    {
        this.pluginLog.Verbose(message.AsDebugString());
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
        return Task.CompletedTask;
    }
}
