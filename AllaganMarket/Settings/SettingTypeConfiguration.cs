using System.Collections.Generic;

using static AllaganMarket.Settings.SettingType;

namespace AllaganMarket.Settings;

public class SettingTypeConfiguration
{
    private List<SettingType>? settingTypes;

    public string GetFormattedName(SettingType settingType)
    {
        switch (settingType)
        {
            case Chat:
                return "Chat";
            case General:
                return "General";
        }

        return settingType.ToString();
    }

    public List<SettingType> GetCategoryOrder()
    {
        return this.settingTypes ??= [General, Chat];
    }
}
