using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ShowRetainerOverlaySetting(ImGuiService imGuiService)
    : BooleanFormField<Configuration>(imGuiService), ISetting
{
    public override bool DefaultValue { get; set; } = true;

    public override string Key { get; set; } = "ShowRetainerOverlay";

    public override string Name { get; set; } = "Show Retainer Overlay?";

    public override string HelpText { get; set; } =
        "When enabled and you are at a retainer bell, a overlay will display on the right hand side of the retainer list, sales list and when pricing items.";

    public override string Version { get; } = "1.0.0";

    public SettingType Type { get; set; } = SettingType.Overlays;
}
