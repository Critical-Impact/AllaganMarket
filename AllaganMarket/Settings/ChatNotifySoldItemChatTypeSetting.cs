using System;

using AllaganLib.Interface.Services;

using AllaganMarket.Settings.Abstract;

using Dalamud.Game.Text;

namespace AllaganMarket.Settings;

public class ChatNotifySoldItemChatTypeSetting : XivChatTypeFormField, ISetting
{
    public ChatNotifySoldItemChatTypeSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override Enum DefaultValue { get; set; } = XivChatType.Echo;

    public override string Key { get; set; } = "SoldItemChatType";

    public override string Name { get; set; } = "Show chat messages on sale of item - chat type";

    public override string HelpText { get; set; } =
        "If `Show chat messages on sale of item` is enabled, which chat channel should these messages be directed to?";

    public override string Version { get; } = "1.0.0.1";

    public SettingType Type { get; set; } = SettingType.Chat;
}
