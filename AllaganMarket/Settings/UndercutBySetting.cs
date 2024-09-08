using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class UndercutBySetting : IntegerFormField<Configuration>, ISetting
{
    public UndercutBySetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override int DefaultValue { get; set; } = 5;

    public override string Key { get; set; } = "UndercutBy";

    public override string Name { get; set; } = "Undercut amount";

    public override string HelpText { get; set; } =
        "When displaying a recommended undercut price, how many gil should we minus from the lowest price to come to come to the recommended price?";

    public override string Version { get; } = "1.0.0.1";

    public SettingType Type { get; set; } = SettingType.Undercutting;
}
