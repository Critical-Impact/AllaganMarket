using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ItemUpdatePeriodSetting(ImGuiService imGuiService)
    : IntegerFormField<Configuration>(imGuiService), ISetting
{
    public override string? Affix { get; } = "m";

    public override int DefaultValue { get; set; } = 300;

    public override string Key { get; set; } = "ItemUpdatePeriod";

    public override string Name { get; set; } = "Stale Pricing Period";

    public override string HelpText { get; set; } =
        "The plugin will mark items in yellow once their pricing is considered to be stale. How often should the plugin ask you to price check/update the item in minutes? Viewing the current offerings for an item or updating the price of an update will reset this counter.";

    public override string Version { get; } = "1.0.0";

    public SettingType Type { get; set; } = SettingType.General;

    public bool ShowInSettings => true;
}
