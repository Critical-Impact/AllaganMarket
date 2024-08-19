using System;
using System.Collections.Generic;
using System.ComponentModel;

using AllaganLib.Interface.FormFields;

namespace AllaganMarket.Grids;

public class SearchResultConfiguration : IConfigurable<string?>, IConfigurable<int?>, INotifyPropertyChanged, IConfigurable<DateTime?>
{
    private readonly Dictionary<string, string> stringFilters = [];
    private readonly Dictionary<string, int> integerFilters = [];
    private readonly Dictionary<string, DateTime> dateTimeFilters = [];

    public string? Get(string key)
    {
        return this.stringFilters.GetValueOrDefault(key);
    }

    public void Set(string key, DateTime? newValue)
    {
        if (newValue == null)
        {
            this.dateTimeFilters.Remove(key);
        }
        else
        {
            this.dateTimeFilters[key] = newValue.Value;
        }
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
    }

    public void Set(string key, int? newValue)
    {
        if (newValue == null)
        {
            this.integerFilters.Remove(key);
        }
        else
        {
            this.integerFilters[key] = newValue.Value;
        }
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
    }

    public void Set(string key, string? newValue)
    {
        if (newValue == null)
        {
            this.stringFilters.Remove(key);
        }
        else
        {
            this.stringFilters[key] = newValue;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
    }

    int? IConfigurable<int?>.Get(string key)
    {
        return this.integerFilters.GetValueOrDefault(key);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    DateTime? IConfigurable<DateTime?>.Get(string key)
    {
        return this.dateTimeFilters.GetValueOrDefault(key);
    }
}
