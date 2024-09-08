using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class UndercutComparisonSetting : EnumFormField<UndercutComparison, Configuration>, ISetting
{
    public UndercutComparisonSetting(ImGuiService imGuiService)
        : base(imGuiService)
    {
    }

    public override Enum DefaultValue { get; set; } = UndercutComparison.Any;

    public override string Key { get; set; } = "UndercutComparison";

    public override string Name { get; set; } = "Undercut comparison";

    public override string HelpText { get; set; } = "When determining if an item is undercut, which results should be compared against? This can be changed individually per item. Items that cannot be HQ will ignore this setting.";

    public override string Version { get; } = "1.0.0.1";

    public override Dictionary<Enum, string> Choices { get; } = new()
    {
        { UndercutComparison.Any, "Any" },
        { UndercutComparison.MatchingQuality, "Matches Quality" },
        { UndercutComparison.NqOnly, "NQ Only" },
        { UndercutComparison.HqOnly, "HQ Only" },
    };

    public SettingType Type { get; set; } = SettingType.Undercutting;
}
