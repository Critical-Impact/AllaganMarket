using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ChatNotifyUndercutCharacterSetting(ImGuiService imGuiService)
    : EnumFormField<ChatNotifyCharacterEnum, Configuration>(imGuiService), ISetting
{
    public override Enum DefaultValue { get; set; } = ChatNotifyCharacterEnum.AllCharacters;

    public override string Key { get; set; } = "ChatNotifyUndercutLoginCharacter";

    public override string Name { get; set; } = "Show chat messages for undercuts for which characters?";

    public override string HelpText { get; set; } =
        "If chat messages are enabled for undercuts, should undercuts on active characters be shown or all retainers?";

    public override string Version { get; } = "1.0.0";

    public override bool Equal(Enum item1, Enum item2)
    {
        return Equals(item1, item2);
    }

    public override Dictionary<Enum, string> Choices { get; } = new()
    {
        { ChatNotifyCharacterEnum.OnlyActiveCharacter, "Only undercuts on active character's retainers" },
        { ChatNotifyCharacterEnum.AllCharacters, "All retainers" },
    };

    public SettingType Type { get; set; } = SettingType.Chat;

    public bool ShowInSettings => true;
}
