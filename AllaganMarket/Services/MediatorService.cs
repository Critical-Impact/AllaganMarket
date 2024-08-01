namespace AllaganMarket.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AllaganMarket.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;

public class MediatorService(ILogger<MediatorService> logger) : IHostedService
{
    public ILogger<MediatorService> Logger { get; } = logger;

    private readonly object addRemoveLock = new();
    private readonly Dictionary<object, DateTime> _lastErrorTime = new();
    private readonly CancellationTokenSource _loopCts = new();
    private readonly ConcurrentQueue<MessageBase> _messageQueue = new();
    private readonly Dictionary<Type, HashSet<SubscriberAction>> _subscriberDict = new();

    public void Publish<T>(T message)
        where T : MessageBase
    {
        if (message.KeepThreadContext)
        {
            this.ExecuteMessage(message);
        }
        else
        {
            this._messageQueue.Enqueue(message);
        }
    }

    public void Publish(List<MessageBase>? messages)
    {
        if (messages != null)
        {
            foreach (var message in messages)
            {
                if (message.KeepThreadContext)
                {
                    this.ExecuteMessage(message);
                }
                else
                {
                    this._messageQueue.Enqueue(message);
                }
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.Logger.LogTrace("Starting service {type} ({this})", this.GetType().Name, this);

        _ = Task.Run(
            async () =>
            {
                while (!this._loopCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, this._loopCts.Token).ConfigureAwait(false);

                    HashSet<MessageBase> processedMessages = new();
                    while (this._messageQueue.TryDequeue(out var message))
                    {
                        if (processedMessages.Contains(message))
                        {
                            continue;
                        }

                        processedMessages.Add(message);

                        this.ExecuteMessage(message);
                    }
                }
            });
        this.Logger.LogTrace("Started service {type} ({this})", this.GetType().Name, this);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Logger.LogTrace("Stopping service {type} ({this})", this.GetType().Name, this);

        this._messageQueue.Clear();
        this._loopCts.Cancel();
        return Task.CompletedTask;
    }

    public void Subscribe<T>(IMediatorSubscriber subscriber, Action<T> action) where T : MessageBase
    {
        lock (this.addRemoveLock)
        {
            this._subscriberDict.TryAdd(typeof(T), new HashSet<SubscriberAction>());

            if (!this._subscriberDict[typeof(T)].Add(new SubscriberAction(subscriber, action)))
            {
                throw new InvalidOperationException("Already subscribed");
            }

            this.Logger.LogDebug(
                "Subscriber added for message {message}: {sub}",
                typeof(T).Name,
                subscriber.GetType().Name);
        }
    }

    public void Unsubscribe<T>(IMediatorSubscriber subscriber) where T : MessageBase
    {
        lock (this.addRemoveLock)
        {
            if (this._subscriberDict.ContainsKey(typeof(T)))
            {
                this._subscriberDict[typeof(T)].RemoveWhere(p => p.Subscriber == subscriber);
            }
        }
    }

    public void UnsubscribeAll(IMediatorSubscriber subscriber)
    {
        lock (this.addRemoveLock)
        {
            foreach (var kvp in this._subscriberDict.Select(k => k.Key))
            {
                var unSubbed = this._subscriberDict[kvp]?.RemoveWhere(p => p.Subscriber == subscriber) ?? 0;
                if (unSubbed > 0)
                {
                    this.Logger.LogDebug("{sub} unsubscribed from {msg}", subscriber.GetType().Name, kvp.Name);
                }
            }
        }
    }

    private void ExecuteMessage(MessageBase message)
    {
        if (!this._subscriberDict.TryGetValue(message.GetType(), out var subscribers) ||
            subscribers == null || !subscribers.Any())
        {
            return;
        }

        HashSet<SubscriberAction> subscribersCopy;
        lock (this.addRemoveLock)
        {
            subscribersCopy = subscribers?.Where(s => s.Subscriber != null).ToHashSet() ??
                              new HashSet<SubscriberAction>();
        }

        foreach (var subscriber in subscribersCopy)
        {
            try
            {
                typeof(MediatorService)
                    .GetMethod(
                        nameof(this.ExecuteSubscriber),
                        BindingFlags.NonPublic | BindingFlags.Instance)?
                    .MakeGenericMethod(message.GetType())
                    .Invoke(this, new object[] { subscriber, message });
            }
            catch (Exception ex)
            {
                if (this._lastErrorTime.TryGetValue(subscriber, out var lastErrorTime) &&
                    lastErrorTime.Add(TimeSpan.FromSeconds(10)) > DateTime.UtcNow)
                {
                    continue;
                }

                this.Logger.LogError(
                    ex,
                    "Error executing {type} for subscriber {subscriber}",
                    message.GetType().Name,
                    subscriber.Subscriber.GetType().Name);
                this._lastErrorTime[subscriber] = DateTime.UtcNow;
            }
        }
    }

    private void ExecuteSubscriber<T>(SubscriberAction subscriber, T message) where T : MessageBase
    {
        var isSameThread = message.KeepThreadContext ? "$" : string.Empty;
        ((Action<T>)subscriber.Action).Invoke(message);
    }

    private sealed class SubscriberAction
    {
        public SubscriberAction(IMediatorSubscriber subscriber, object action)
        {
            this.Subscriber = subscriber;
            this.Action = action;
        }

        public object Action { get; }

        public IMediatorSubscriber Subscriber { get; }
    }
}