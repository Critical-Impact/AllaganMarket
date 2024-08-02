namespace AllaganMarket;

using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Models;

[Serializable]
public class Configuration : IPluginConfiguration
{
    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public bool IsConfigWindowMovable { get; set; } = true;

    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    
    public TimeSpan ItemCheckPeriod { get; set; } = TimeSpan.FromHours(6);
    
    public Dictionary<ulong, Character> Characters { get; set; } = new();

    //Move these to CSVs
    public Dictionary<ulong, SaleItem[]> SaleItems { get; set; } = new();
    public Dictionary<ulong, List<SoldItem>> Sales { get; set; } = new();
    public Dictionary<ulong, uint> Gil { get; set; } = new();

    public int Version { get; set; } = 0;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
