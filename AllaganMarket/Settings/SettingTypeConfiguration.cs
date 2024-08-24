using System.Collections.Generic;

using static AllaganMarket.Settings.SettingType;

namespace AllaganMarket.Settings;

public class SettingTypeConfiguration
{
    private List<SettingType>? settingTypes;

    public static string GetFormattedName(SettingType settingType)
    {
        return settingType switch
        {
            Chat => "Chat",
            General => "General",
            SettingType.Features => "Features",
            Overlays => "Overlays",
            _ => settingType.ToString(),
        };
    }

    public List<SettingType> GetCategoryOrder()
    {
        return this.settingTypes ??= [General, SettingType.Features, Overlays, Chat];
    }
}
