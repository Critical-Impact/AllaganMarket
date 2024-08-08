using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class AddTitleMenuButtonSetting : BooleanSetting<Configuration>, ISetting
{
    public AddTitleMenuButtonSetting(ImGuiService imGuiService)
        : base(imGuiService)
    {
    }

    public override bool DefaultValue { get; set; } = false;

    public override string Key { get; set; } = "AddTitleMenuButton";

    public override string Name { get; set; } = "Add title menu button?";

    public override string HelpText { get; set; } =
        "Should a button to open Allagan Market be added to Dalamud's title menu?";

    public override string Version { get; } = "1.0.0";

    public SettingType Type { get; set; } = SettingType.Features;
}
