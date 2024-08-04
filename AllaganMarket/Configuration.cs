using AllaganLib.Interface.FormFields;

namespace AllaganMarket;

using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Models;

[Serializable]
public class Configuration : IPluginConfiguration, IConfigurable<int?>, IConfigurable<bool?>, IConfigurable<Enum?>
{
    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public bool IsConfigWindowMovable { get; set; } = true;

    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    
    public Dictionary<ulong, Character> Characters { get; set; } = new();

    //Move these to CSVs
    public Dictionary<ulong, SaleItem[]> SaleItems { get; set; } = new();
    public Dictionary<ulong, List<SoldItem>> Sales { get; set; } = new();
    public Dictionary<ulong, uint> Gil { get; set; } = new();

    public Dictionary<string, int> IntegerSettings { get; set; } = new();
    public Dictionary<string, bool> BooleanSettings { get; set; } = new();
    public Dictionary<string, Enum> EnumSettings { get; set; } = new();

    public int Version { get; set; } = 0;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }

    public int? Get(string key)
    {
        return this.IntegerSettings.TryGetValue(key, out var value) ? value : null;
    }

    public void Set(string key, Enum? newValue)
    {
        if (newValue == null)
        {
            this.EnumSettings.Remove(key);
        }
        else
        {
            this.EnumSettings[key] = newValue;
        }

        this.Save();
    }

    public void Set(string key, bool? newValue)
    {
        if (newValue == null)
        {
            this.BooleanSettings.Remove(key);
        }
        else
        {
            this.BooleanSettings[key] = newValue.Value;
        }

        this.Save();
    }

    public void Set(string key, int? newValue)
    {
        if (newValue == null)
        {
            this.IntegerSettings.Remove(key);
        }
        else
        {
            this.IntegerSettings[key] = newValue.Value;
        }

        this.Save();
    }

    bool? IConfigurable<bool?>.Get(string key)
    {
        return this.BooleanSettings.TryGetValue(key, out var value) ? value : null;
    }

    Enum? IConfigurable<Enum?>.Get(string key)
    {
        return this.EnumSettings.GetValueOrDefault(key);
    }
}
