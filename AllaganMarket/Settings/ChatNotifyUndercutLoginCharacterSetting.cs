using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ChatNotifyUndercutLoginCharacterSetting : EnumSetting<ChatNotifyCharacterEnum, Configuration>, ISetting
{
    public ChatNotifyUndercutLoginCharacterSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override Enum DefaultValue { get; set; } = ChatNotifyCharacterEnum.AllCharacters;

    public override string Key { get; set; } = "ChatNotifyUndercutLoginCharacter";

    public override string Name { get; set; } = "Show chat messages for undercuts for which characters?";

    public override string HelpText { get; set; } =
        "If chat messages are enabled for undercuts, should undercuts on active characters be shown or all retainers?";

    public override string Version { get; } = "1.0.0";

    public override Dictionary<Enum, string> Choices { get; } = new Dictionary<Enum, string>()
    {
        { ChatNotifyCharacterEnum.OnlyActiveCharacter, "Only undercuts on active character's retainers" },
        { ChatNotifyCharacterEnum.AllCharacters, "All retainers" },
    };

    public SettingType Type { get; set; } = SettingType.Chat;
}
