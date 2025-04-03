using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class RoundToSetting : IntegerFormField<Configuration>, ISetting
{
    public RoundToSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override int DefaultValue { get; set; } = 1;

    public override string Key { get; set; } = "RoundTo";

    public override string Name { get; set; } = "Round the recommended price to the nearest multiple of";

    public override string HelpText { get; set; } =
        "When displaying a recommended undercut price, this indicates the nearest multiple to which the recommended price is rounded to. Set to 1 if you don't want to round off the recommended pricing.";

    public override string Version { get; } = "1.0.0.1";

    public SettingType Type { get; set; } = SettingType.Undercutting;
}
