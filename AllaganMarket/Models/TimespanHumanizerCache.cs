using System;
using System.Collections.Generic;

using Humanizer;

namespace AllaganMarket.Models;

public class TimeSpanHumanizerCache
{
    private readonly int cacheSize;
    private readonly Dictionary<TimeSpan, string> cache;
    private readonly Queue<TimeSpan> cacheOrder;

    public TimeSpanHumanizerCache(int cacheSize = 100)
    {
        this.cacheSize = cacheSize;
        this.cache = new Dictionary<TimeSpan, string>();
        this.cacheOrder = new Queue<TimeSpan>();
    }

    public string GetHumanizedTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.Days == 0 && timeSpan.Hours == 0 && timeSpan.Minutes == 0)
        {
            return "Just Now";
        }

        var roundedTimeSpan = new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, 0);

        if (this.cache.TryGetValue(roundedTimeSpan, out var humanized))
        {
            return humanized;
        }

        humanized = roundedTimeSpan.Humanize();

        this.AddToCache(roundedTimeSpan, humanized);

        return humanized;
    }

    private void AddToCache(TimeSpan timeSpan, string humanized)
    {
        if (this.cache.Count >= this.cacheSize)
        {
            var oldestTimeSpan = this.cacheOrder.Dequeue();
            this.cache.Remove(oldestTimeSpan);
        }

        this.cacheOrder.Enqueue(timeSpan);
        this.cache[timeSpan] = humanized;
    }
}
