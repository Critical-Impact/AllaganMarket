using System.ComponentModel;

using AllaganLib.Interface.Converters;
using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Wizard;

using Newtonsoft.Json;

namespace AllaganMarket;

using System;
using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Plugin;

using Models;

[Serializable]
public class Configuration : IPluginConfiguration, IConfigurable<int?>, IConfigurable<bool?>, IConfigurable<Enum?>,
                             IWizardConfiguration
{
    private HashSet<string>? wizardVersionsSeen { get; set; } = null;

    public bool IsConfigWindowMovable { get; set; } = true;

    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public Dictionary<ulong, Character> Characters { get; set; } = new();

    [JsonIgnore]
    public Dictionary<ulong, SaleItem[]> SaleItems { get; set; } = new();

    [JsonIgnore]
    public Dictionary<ulong, List<SoldItem>> Sales { get; set; } = new();

    public Dictionary<ulong, uint> Gil { get; set; } = new();

    public Dictionary<string, int> IntegerSettings { get; set; } = new();

    public Dictionary<string, bool> BooleanSettings { get; set; } = new();

    [JsonConverter(typeof(EnumDictionaryConverter))]
    public Dictionary<string, Enum> EnumSettings { get; set; } = new();

    public bool IsDirty { get; set; } = false;

    public int Version { get; set; } = 0;

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

        this.IsDirty = true;
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

        this.IsDirty = true;
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

        this.IsDirty = true;
    }

    bool? IConfigurable<bool?>.Get(string key)
    {
        return this.BooleanSettings.TryGetValue(key, out var value) ? value : null;
    }

    Enum? IConfigurable<Enum?>.Get(string key)
    {
        return this.EnumSettings.GetValueOrDefault(key);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Ignore)]
    [DefaultValue(true)]
    public bool ShowWizardNewFeatures { get; set; }

    public HashSet<string> WizardVersionsSeen
    {
        get => this.wizardVersionsSeen ??= new HashSet<string>();
        set
        {
            this.wizardVersionsSeen = value;
            this.IsDirty = true;
        }
    }

    public void MarkWizardVersionSeen(string versionNumber)
    {
        this.WizardVersionsSeen.Add(versionNumber);
        this.IsDirty = true;
    }
}
