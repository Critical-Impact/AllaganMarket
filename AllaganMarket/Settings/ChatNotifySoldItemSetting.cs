using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ChatNotifySoldItemSetting(ImGuiService imGuiService) : BooleanFormField<Configuration>(imGuiService), ISetting
{
    public override bool DefaultValue { get; set; } = true;

    public override string Key { get; set; } = "ChatNotifySoldItem";

    public override string Name { get; set; } = "Show chat messages on sale of item?";

    public override string HelpText { get; set; } =
        "This will show a message when a sale of an item occurs. At present, this only happens when you view a retainer and AM is able to calculate if the item is sold.";

    public override string Version { get; } = "1.0.0.1";

    public SettingType Type { get; set; } = SettingType.Chat;

    public bool ShowInSettings => true;
}
