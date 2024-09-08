using System;
using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ChatNotifyUndercutGroupingSetting : EnumFormField<ChatNotifyUndercutGrouping, Configuration>, ISetting
{
    public ChatNotifyUndercutGroupingSetting(ImGuiService imGuiService)
        : base(imGuiService)
    {
    }

    public override Enum DefaultValue { get; set; } = ChatNotifyUndercutGrouping.GroupByItem;

    public override string Key { get; set; } = "UndercutGrouping";

    public override string Name { get; set; } = "Group undercut messages by?";

    public override string HelpText { get; set; } =
        "When multiple undercuts occur, how should these be grouped together?";

    public override string Version => "1.0.0.1";

    public override Dictionary<Enum, string> Choices => new()
    {
        { ChatNotifyUndercutGrouping.Individual, "Individually"},
        { ChatNotifyUndercutGrouping.Together, "All Together"},
        { ChatNotifyUndercutGrouping.GroupByItem, "By Item"},
        { ChatNotifyUndercutGrouping.GroupByRetainer, "By Retainer"},
    };

    public SettingType Type { get; set; } = SettingType.Chat;
}

public enum ChatNotifyUndercutGrouping
{
    Individual,
    Together,
    GroupByItem,
    GroupByRetainer,
}
