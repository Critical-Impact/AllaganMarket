// <copyright file="ChatNotifyUndercutSetting.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Services;

namespace AllaganMarket.Settings;

public class ChatNotifyUndercutSetting : BooleanSetting<Configuration>, ISetting
{
    public ChatNotifyUndercutSetting(ImGuiService imGuiService) : base(imGuiService)
    {
    }

    public override bool DefaultValue { get; set; } = true;

    public override string Key { get; set; } = "ChatNotifyUndercut";

    public override string Name { get; set; } = "Show chat messages on undercut?";

    public override string HelpText { get; set; } =
        "Should a chat message anytime a undercut occurs on an item you are selling?";

    public override string Version { get; } = "1.0.0";

    public SettingType Type { get; set; } = SettingType.Chat;
}
