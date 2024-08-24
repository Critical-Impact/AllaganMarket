using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class AddDtrBarEntrySetting(ImGuiService imGuiService) : BooleanFormField<Configuration>(imGuiService), ISetting
{
    public override bool DefaultValue { get; set; } = false;

    public override string Key { get; set; } = "AddDtrBarEntry";

    public override string Name { get; set; } = "Add undercut notification to server info bar?";

    public override string HelpText { get; set; } =
        "Adds a entry to the server info bar that informs you of the number of items you have for sale that have been undercut.";

    public override string Version { get; } = "1.0.0";

    public SettingType Type { get; set; } = SettingType.Features;
}
