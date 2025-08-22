using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class UndercutAllowFallbackSetting : BooleanFormField<Configuration>, ISetting
{
    public UndercutAllowFallbackSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override bool DefaultValue { get; set; } = true;

    public override string Key { get; set; } = "UndercutAllowFallback";

    public override string Name { get; set; } = "Undercut fallback";

    public override string HelpText { get; set; } =
        "When determining if you have been undercut and there are no listings for the quality selected, should AT fallback to using a price from the other quality. i.e. there are no HQ entries, it will use NQ entries instead. ";

    public override string Version { get; } = "1.1.0.9";

    public SettingType Type { get; set; } = SettingType.Undercutting;

    public bool ShowInSettings => true;
}
