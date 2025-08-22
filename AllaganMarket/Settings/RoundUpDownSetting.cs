using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;


public class RoundUpDownSetting : BooleanFormField<Configuration>, ISetting
{
    public RoundUpDownSetting(ImGuiService imGuiService)
            : base(imGuiService)
    {
    }
    public override bool DefaultValue { get; set; } = false;

    public override string Key { get; set; } = "RoundUpDown";

    public override string Name { get; set; } = "Round up or down";

    public override string HelpText { get; set; } = "Checking this box will round the recommended price UP from the undercut calcuations. You might want to do this if you undercut by a larger number and want a visually pleasing result. Leaving the box unchecked will round DOWN.";

    public override string Version { get; } = "1.0.0.1";

    public SettingType Type { get; set; } = SettingType.Undercutting;

    public bool ShowInSettings => true;
}

