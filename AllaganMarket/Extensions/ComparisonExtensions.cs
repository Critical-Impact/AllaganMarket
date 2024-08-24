using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

using Dalamud.Utility;

namespace AllaganMarket.Extensions;

public static class ComparisonExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool PassesFilter(this string text, string filterString)
    {
        filterString = filterString.Trim();
        if (filterString.Contains("||", StringComparison.Ordinal))
        {
            var ors = filterString.Split("||");
            return ors.Select(c => PassesFilter(text, c)).Any(c => c);
        }

        if (filterString.Contains("&&", StringComparison.Ordinal))
        {
            var ands = filterString.Split("&&");
            return ands.Select(c => PassesFilter(text, c)).All(c => c);
        }

        if (filterString.StartsWith('=') && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (text == filter)
            {
                return true;
            }
        }
        else if (filterString.StartsWith('!'))
        {
            if (filterString.Length >= 2)
            {
                var filter = filterString[1..];
                return !text.Contains(filter);
            }

            if (filterString.Length == 1)
            {
                return !text.IsNullOrEmpty();
            }
        }
        else if (filterString.StartsWith('~') && filterString.Length >= 2)
        {
            var filter = filterString[1..].Split(" ");
            var splitText = text.Split(" ");
            return filter.All(c => splitText.Any(d => d.Contains(c)));
        }

        return text.Contains(filterString, StringComparison.Ordinal);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool PassesFilter(this DateTime date, string filterString)
    {
        filterString = filterString.Trim();
        if (filterString.Contains("||", StringComparison.Ordinal))
        {
            var ors = filterString.Split("||");
            return ors.Select(c => PassesFilter(date, c)).Any(c => c);
        }

        if (filterString.Contains("&&", StringComparison.Ordinal))
        {
            var ands = filterString.Split("&&");
            return ands.Select(c => PassesFilter(date, c)).All(c => c);
        }

        if (filterString.StartsWith('=') && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (date.ToString(CultureInfo.CurrentCulture) == filter)
            {
                return true;
            }

            if (date.Date.ToShortDateString() == filter)
            {
                return true;
            }
        }
        else if (filterString.StartsWith("<=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (DateTime.TryParse(filter, CultureInfo.CurrentCulture, out var filterDate))
            {
                if (filterDate >= date)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (DateTime.TryParse(filter, CultureInfo.CurrentCulture, out var filterDate))
            {
                if (filterDate <= date)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("<") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (DateTime.TryParse(filter, CultureInfo.CurrentCulture, out var filterDate))
            {
                if (filterDate > date)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (DateTime.TryParse(filter, CultureInfo.CurrentCulture, out var filterDate))
            {
                if (filterDate < date)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith('!'))
        {
            if (filterString.Length >= 2)
            {
                var filter = filterString[1..];
                return !date.ToString(CultureInfo.CurrentCulture).Contains(filter);
            }

            if (filterString.Length == 1)
            {
                return !date.ToString(CultureInfo.CurrentCulture).IsNullOrEmpty();
            }
        }
        else if (filterString.StartsWith('~') && filterString.Length >= 2)
        {
            var filter = filterString[1..].Split(" ");
            var splitText = date.ToString(CultureInfo.CurrentCulture).Split(" ");
            return filter.All(c => splitText.Any(d => d.Contains(c)));
        }

        if (filterString.IsHumanizedString())
        {
            var timeSpan = filterString.ParseHumanizedString();
            return DateTime.Now - timeSpan >= date;
        }

        return date.ToString(CultureInfo.CurrentCulture).Contains(filterString, StringComparison.Ordinal);
    }

    public static bool PassesFilter(this uint number, string filterString)
    {
        return PassesFilter((int)number, filterString);
    }

    public static bool PassesFilter(this ushort number, string filterString)
    {
        return PassesFilter((int)number, filterString);
    }

    public static bool PassesFilter(this double number, string filterString)
    {
        filterString = filterString.Trim();
        if (filterString.Contains("||"))
        {
            var ors = filterString.Split("||");
            return ors.Select(c => PassesFilter(number, c)).Any(c => c);
        }

        if (filterString.Contains("&&"))
        {
            var ands = filterString.Split("&&");
            return ands.Select(c => PassesFilter(number, c)).All(c => c);
        }

        if (filterString.StartsWith("=") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (number.ToString() == filter)
            {
                return true;
            }
        }
        else if (filterString.StartsWith("<=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult >= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult <= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("<") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult > number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult < number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("!") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            return !number.ToString(CultureInfo.InvariantCulture).Contains(filter);
        }

        return number.ToString(CultureInfo.InvariantCulture).Contains(filterString);
    }

    public static bool PassesFilter(this decimal number, string filterString)
    {
        filterString = filterString.Trim();
        if (filterString.Contains("||"))
        {
            var ors = filterString.Split("||");
            return ors.Select(c => PassesFilter(number, c)).Any(c => c);
        }

        if (filterString.Contains("&&"))
        {
            var ands = filterString.Split("&&");
            return ands.Select(c => PassesFilter(number, c)).All(c => c);
        }

        if (filterString.StartsWith("=") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (number.ToString() == filter)
            {
                return true;
            }
        }
        else if (filterString.StartsWith("<=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (decimal.TryParse(filter, out var numberResult))
            {
                if (numberResult >= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (decimal.TryParse(filter, out var numberResult))
            {
                if (numberResult <= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("<") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (decimal.TryParse(filter, out var numberResult))
            {
                if (numberResult > number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (decimal.TryParse(filter, out var numberResult))
            {
                if (numberResult < number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("!") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            return !number.ToString(CultureInfo.InvariantCulture).Contains(filter);
        }

        return number.ToString(CultureInfo.InvariantCulture).Contains(filterString);
    }

    public static bool PassesFilter(this float number, string filterString)
    {
        filterString = filterString.Trim();
        if (filterString.Contains("||"))
        {
            var ors = filterString.Split("||");
            return ors.Select(c => PassesFilter(number, c)).Any(c => c);
        }

        if (filterString.Contains("&&"))
        {
            var ands = filterString.Split("&&");
            return ands.Select(c => PassesFilter(number, c)).All(c => c);
        }

        if (filterString.StartsWith("=") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (number.ToString() == filter)
            {
                return true;
            }
        }
        else if (filterString.StartsWith("<=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult >= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult <= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("<") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult > number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (double.TryParse(filter, out var numberResult))
            {
                if (numberResult < number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("!") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            return !number.ToString(CultureInfo.InvariantCulture).Contains(filter);
        }

        return number.ToString(CultureInfo.InvariantCulture).Contains(filterString);
    }

    public static bool PassesFilter(this int number, string filterString)
    {
        filterString = filterString.Trim();
        if (filterString.Contains("||"))
        {
            var ors = filterString.Split("||");
            return ors.Select(c => PassesFilter(number, c)).Any(c => c);
        }

        if (filterString.Contains("&&"))
        {
            var ands = filterString.Split("&&");
            return ands.Select(c => PassesFilter(number, c)).All(c => c);
        }

        if (filterString.StartsWith("=") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (number.ToString() == filter)
            {
                return true;
            }
        }
        else if (filterString.StartsWith("<=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (int.TryParse(filter, out var numberResult))
            {
                if (numberResult >= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">=") && filterString.Length >= 3)
        {
            var filter = filterString[2..];
            if (int.TryParse(filter, out var numberResult))
            {
                if (numberResult <= number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("<") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (int.TryParse(filter, out var numberResult))
            {
                if (numberResult > number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith(">") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            if (int.TryParse(filter, out var numberResult))
            {
                if (numberResult < number)
                {
                    return true;
                }
            }
        }
        else if (filterString.StartsWith("!") && filterString.Length >= 2)
        {
            var filter = filterString[1..];
            return !number.ToString().Contains(filter);
        }

        return number.ToString().Contains(filterString);
    }
}
