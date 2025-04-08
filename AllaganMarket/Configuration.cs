using System;
using System.Collections.Generic;
using System.ComponentModel;

using AllaganLib.Interface.Converters;
using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Wizard;

using AllaganMarket.Models;
using AllaganMarket.Settings;

using Dalamud.Configuration;
using Dalamud.Game.Text;

using Newtonsoft.Json;

namespace AllaganMarket;

[Serializable]
public class Configuration : IPluginConfiguration, IConfigurable<int?>, IConfigurable<bool?>, IConfigurable<Enum?>,
                             IWizardConfiguration
{
    private HashSet<string>? wizardVersionsSeen1;
    private bool isConfigWindowMovable = true;
    private Dictionary<ulong, Character> characters = [];
    private Dictionary<ulong, SaleItem[]> saleItems = [];
    private Dictionary<ulong, List<SoldItem>> sales = [];
    private Dictionary<uint, Dictionary<(uint, bool), MarketPriceCache>> marketPriceCache = [];
    private Dictionary<uint, UndercutComparison> undercutComparisonSettings = [];
    private Dictionary<ulong, uint> gil = [];
    private Dictionary<string, int> integerSettings = [];
    private Dictionary<string, bool> booleanSettings = [];
    private Dictionary<string, Enum> enumSettings = [];
    private bool isDirty;
    private int version;
    private bool showWizardNewFeatures;

    public bool IsConfigWindowMovable
    {
        get => this.isConfigWindowMovable;
        set => this.isConfigWindowMovable = value;
    }

    public Dictionary<ulong, Character> Characters
    {
        get => this.characters;
        set => this.characters = value;
    }

    [JsonIgnore]
    public Dictionary<ulong, SaleItem[]> SaleItems
    {
        get => this.saleItems;
        set => this.saleItems = value;
    }

    [JsonIgnore]
    public Dictionary<ulong, List<SoldItem>> Sales
    {
        get => this.sales;
        set => this.sales = value;
    }

    [JsonIgnore]
    public Dictionary<uint, Dictionary<(uint ItemId, bool IsHq), MarketPriceCache>> MarketPriceCache
    {
        get => this.marketPriceCache;
        set => this.marketPriceCache = value;
    }

    public Dictionary<ulong, uint> Gil
    {
        get => this.gil;
        set => this.gil = value;
    }

    public Dictionary<string, int> IntegerSettings
    {
        get => this.integerSettings;
        set => this.integerSettings = value;
    }

    public Dictionary<string, bool> BooleanSettings
    {
        get => this.booleanSettings;
        set => this.booleanSettings = value;
    }

    [JsonConverter(typeof(EnumDictionaryConverter))]
    public Dictionary<string, Enum> EnumSettings
    {
        get => this.enumSettings;
        set => this.enumSettings = value;
    }

    public bool IsDirty
    {
        get => this.isDirty;
        set => this.isDirty = value;
    }

    public int Version
    {
        get => this.version;
        set => this.version = value;
    }

    public Dictionary<uint, UndercutComparison> UndercutComparisonSettings
    {
        get => this.undercutComparisonSettings;
        set => this.undercutComparisonSettings = value;
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Ignore)]
    [DefaultValue(true)]
    public bool ShowWizardNewFeatures
    {
        get => this.showWizardNewFeatures;
        set
        {
            this.showWizardNewFeatures = value;
            this.isDirty = true;
        }
    }

    public HashSet<string> WizardVersionsSeen
    {
        get => this.WizardVersionsSeen1 ??= [];
        set
        {
            this.WizardVersionsSeen1 = value;
            this.IsDirty = true;
        }
    }

    private HashSet<string>? WizardVersionsSeen1
    {
        get => this.wizardVersionsSeen1;
        set => this.wizardVersionsSeen1 = value;
    }

    public void MarkWizardVersionSeen(string versionNumber)
    {
        this.WizardVersionsSeen.Add(versionNumber);
        this.IsDirty = true;
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

    public void SetUndercutComparison(uint itemId, UndercutComparison newSetting)
    {
        this.undercutComparisonSettings[itemId] = newSetting;
        this.IsDirty = true;
    }

    public void RemoveUndercutComparison(uint itemId)
    {
        if (this.undercutComparisonSettings.Remove(itemId))
        {
            this.IsDirty = true;
        }
    }

    public UndercutComparison? GetUndercutComparison(uint itemId)
    {
        return this.undercutComparisonSettings.TryGetValue(itemId, out var value) ? value : null;
    }
}
