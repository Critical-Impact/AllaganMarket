using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public enum ViewMode
{
    Grid,
    List
}

public class ViewModeSetting : EnumFormField<ViewMode, Configuration>, ISetting
{
    public ViewModeSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override Enum DefaultValue { get; set; } = ViewMode.Grid;

    public override string Key { get; set; } = "ViewMode";

    public override string Name { get; set; } = "Sales/Sold View Mode";

    public override string HelpText { get; set; } =
        "How should the sales/sold tabs be laid out, in a grid or in a list?";

    public override string Version { get; } = "1.0.0";

    public override Dictionary<Enum, string> Choices { get; } = new Dictionary<Enum, string>()
    {
        [ViewMode.Grid] = "Grid",
        [ViewMode.List] = "List",
    };

    public SettingType Type { get; set; } = SettingType.General;
}
