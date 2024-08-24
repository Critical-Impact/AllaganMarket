using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class RetainerOverlayCollapsedSetting(ImGuiService imGuiService) : BooleanFormField<Configuration>(imGuiService)
{
    public override bool DefaultValue { get; set; } = true;

    public override string Key { get; set; } = "RetainerOverlayCollapsed";

    public override string Name { get; set; } = "Retainer Overlay - Collapsed?";

    public override string HelpText { get; set; } = "Is the retainer overlay currently collapsed?";

    public override string Version { get; } = "1.0.0";
}
