using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ItemUpdatePeriodSetting : IntegerSetting<Configuration>, ISetting
{
    public ItemUpdatePeriodSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override string? Affix { get; } = "m";

    public override int DefaultValue { get; set; } = 300;

    public override string Key { get; set; } = "ItemUpdatePeriod";

    public override string Name { get; set; } = "Item Update Period";

    public override string HelpText { get; set; } =
        "How often should the plugin ask you to price check/update the item in minutes? Updating the price of an update will reset this counter.";

    public override string Version { get; } = "1.0.0";

    public SettingType Type { get; set; } = SettingType.General;
}
