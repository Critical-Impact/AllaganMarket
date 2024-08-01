namespace AllaganMarket.Extensions;

using System;
using System.Collections.Generic;

public static class StringExtensions
{
    private static Dictionary<string, Func<int, TimeSpan>> timeUnitMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "second", value => TimeSpan.FromSeconds(value) },
        { "seconds", value => TimeSpan.FromSeconds(value) },
        { "minute", value => TimeSpan.FromMinutes(value) },
        { "minutes", value => TimeSpan.FromMinutes(value) },
        { "hour", value => TimeSpan.FromHours(value) },
        { "hours", value => TimeSpan.FromHours(value) },
        { "day", value => TimeSpan.FromDays(value) },
        { "days", value => TimeSpan.FromDays(value) },
        { "week", value => TimeSpan.FromDays(value * 7) },
        { "weeks", value => TimeSpan.FromDays(value * 7) },
        { "month", value => TimeSpan.FromDays(value * 30) }, // Approximation
        { "months", value => TimeSpan.FromDays(value * 30) }, // Approximation
        { "year", value => TimeSpan.FromDays(value * 365) }, // Approximation
        { "years", value => TimeSpan.FromDays(value * 365) } // Approximation
    };

    public static bool IsHumanizedString(this string humanizedString)
    {
        var parts = humanizedString.Split(' ');
        if (parts.Length < 2)
        {
            return false;
        }

        // Extract the number and unit
        if (!int.TryParse(parts[0], out var value))
        {
            return false;
        }

        var unit = parts[1].ToLowerInvariant();

        // Check if the unit is present in the dictionary and convert to TimeSpan
        if (timeUnitMap.TryGetValue(unit, out _))
        {
            return true;
        }

        return false;
    }

    public static TimeSpan ParseHumanizedString(this string humanizedString)
    {
        var parts = humanizedString.Split(' ');
        if (parts.Length < 2)
        {
            throw new ArgumentException("Invalid humanized string format");
        }

        // Extract the number and unit
        if (!int.TryParse(parts[0], out var value))
        {
            throw new ArgumentException("Invalid number in humanized string");
        }

        var unit = parts[1].ToLowerInvariant();

        // Check if the unit is present in the dictionary and convert to TimeSpan
        if (timeUnitMap.TryGetValue(unit, out var timeSpanFactory))
        {
            return timeSpanFactory(value);
        }

        throw new ArgumentException($"Unsupported time unit: {unit}");
    }
}