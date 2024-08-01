namespace AllaganMarket.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;
using Models.Bson;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

public class UniversalisWebsocketService : BackgroundService
{
    private readonly ClientWebSocket client;
    private readonly IPluginLog pluginLog;
    private readonly Uri uri = new("wss://universalis.app/api/ws");
    private readonly Queue<(EventType, uint)> subscriptionChannelQueue = new();
    private readonly Queue<(EventType, uint)> unsubscriptionChannelQueue = new();
    private readonly HashSet<(EventType, uint)> subscriptions = [];


    public UniversalisWebsocketService(ClientWebSocket client, IPluginLog pluginLog)
    {
        this.client = client;
        this.pluginLog = pluginLog;
    }

    public delegate void UniversalisEventDelegate(SubscriptionReceivedMessage subscriptionReceivedMessage);

    public event UniversalisEventDelegate? OnUniversalisEvent;

    public enum EventType
    {
        ListingsAdd = 0,
        ListingsRemove = 1,
        SalesAdd = 2,
        SalesRemove = 3,
        Unknown = 4
    }

    public void SubscribeToChannel(EventType eventType, uint worldId)
    {
        var newSubscription = (subscriptionType: eventType, worldId);
        if (!this.subscriptions.Contains(newSubscription))
        {
            this.subscriptionChannelQueue.Enqueue(newSubscription);
        }
    }

    public void UnsubscribeFromChannel(EventType eventType, uint worldId)
    {
        var newSubscription = (subscriptionType: eventType, worldId);
        if (this.subscriptions.Contains(newSubscription))
        {
            this.unsubscriptionChannelQueue.Enqueue(newSubscription);
        }
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return this.ProcessingLoop(cancellationToken);
    }

    /// <summary>
    /// Either connects to universalis, subscribes, unsubscribes or receives data
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    protected async Task ProcessingLoop(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (this.client.State is WebSocketState.None or WebSocketState.Closed)
            {
                if (this.subscriptionChannelQueue.Count != 0)
                {
                    await this.ConnectWithRetryAsync(cancellationToken);
                }
                else
                {
                    await Task.Delay(500, cancellationToken);
                    return;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (this.subscriptionChannelQueue.Count != 0)
            {
                await this.SendMessages(this.subscriptionChannelQueue, "subscribe", cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (this.unsubscriptionChannelQueue.Count != 0)
            {
                await this.SendMessages(this.unsubscriptionChannelQueue, "unsubscribe", cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (this.subscriptions.Count == 0)
            {
                // No subscribed worlds, wait a second and check again
                await Task.Delay(1000, cancellationToken);
            }

            await this.ReceiveMessages(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task SendMessages(Queue<(EventType SubscriptionType, uint WorldId)> subscriptionQueue, string eventName, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (subscriptionQueue.Count == 0)
        {
            return;
        }

        var nextMessage = subscriptionQueue.Dequeue();
        var worldId = nextMessage.WorldId;
        string channelType;
        switch (nextMessage.SubscriptionType)
        {
            case EventType.ListingsAdd:
                channelType = "listings/add";
                break;
            case EventType.ListingsRemove:
                channelType = "listings/remove";
                break;
            case EventType.SalesAdd:
                channelType = "sales/add";
                break;
            case EventType.SalesRemove:
                channelType = "sales/remove";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var channel = channelType + "{world=" + worldId + "}";
        var subscriptionMessage = new SubscriptionMessage(eventName, channel).ToBsonDocument();
        var segment = new ArraySegment<byte>(subscriptionMessage.ToBson());

        try
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await this.client.SendAsync(segment, WebSocketMessageType.Binary, false, cancellationToken);
                if (eventName == "subscribe")
                {
                    this.subscriptions.Add(nextMessage);
                    this.pluginLog.Verbose($"Subscribed to world {worldId}'s {channelType}.");
                }
                else
                {
                    this.subscriptions.Remove(nextMessage);
                    this.pluginLog.Verbose($"Unsubscribed from world {worldId}'s {channelType}.");
                }
            }
        }
        catch (Exception ex)
        {
            this.pluginLog.Verbose($"Failed to subscribe to world {worldId}'s {channelType}.");
            subscriptionQueue.Enqueue(nextMessage);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
    }

    private async Task ReceiveMessages(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        if (!cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            var messageBytes = new List<byte>();

            do
            {
                try
                {
                    result = await this.client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (TimeoutException ex)
                {
                    return;
                }

                messageBytes.AddRange(buffer.Take(result.Count));
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                this.pluginLog.Info("WebSocket connection closed.");
                await this.client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
            }
            else
            {
                var message = BsonSerializer.Deserialize<SubscriptionReceivedMessage>(messageBytes.ToArray());
                this.OnUniversalisEvent?.Invoke(message);
            }

            await this.ReceiveMessages(cancellationToken);
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await this.client.ConnectAsync(this.uri, cancellationToken);
                this.subscriptions.Clear();
            }

            this.pluginLog.Info("Connected to universalis websocket.");
        }
        catch (Exception ex)
        {
            this.pluginLog.Error(ex, "Failed to connect to universalis. Retrying in 5 seconds...");
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    public class SubscriptionMessage(string @event, string channel)
    {
        [BsonElement("event")]
        public string Event { get; } = @event;

        [BsonElement("channel")]
        public string Channel { get; } = channel;
    }
}