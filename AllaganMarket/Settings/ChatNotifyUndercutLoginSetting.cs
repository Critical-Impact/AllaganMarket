using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ChatNotifyUndercutLoginSetting : BooleanSetting<Configuration>, ISetting
{
    public ChatNotifyUndercutLoginSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override bool DefaultValue { get; set; } = true;

    public override string Key { get; set; } = "ChatNotifyUndercutLogin";

    public override string Name { get; set; } = "Show chat message for undercuts on login?";

    public override string HelpText { get; set; } =
        "When you first login should the plugin notify you of any undercuts that have occurred on your items?";

    public override string Version { get; } = "1.0.0";

    public SettingType Type { get; set; } = SettingType.Chat;
}
