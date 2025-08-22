using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class HighlightingRetainerSellListSetting(ImGuiService imGuiService)
    : BooleanFormField<Configuration>(imGuiService), ISetting
{
    public override bool DefaultValue { get; set; } = true;

    public override string Key { get; set; } = "HighlightRetainerSellList";

    public override string Name { get; set; } = "Highlight retainer sell list?";

    public override string HelpText { get; set; } =
        "Should the retainer sell list be highlighted for undercuts and updates?";

    public override string Version { get; } = "1.0.0.3";

    public SettingType Type { get; set; } = SettingType.Highlighting;

    public bool ShowInSettings => true;
}
