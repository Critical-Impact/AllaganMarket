using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Wizard;

using AllaganMarket.Settings;

namespace AllaganMarket.Features;

public class ChatNotificationsFeature(IEnumerable<IFormField<Configuration>> settings) : Feature<Configuration>(
    [
        typeof(ChatNotifyUndercutSetting),
        typeof(ChatNotifyUndercutLoginSetting),
        typeof(ChatNotifySoldItemSetting),
        typeof(ChatNotifyUndercutCharacterSetting),
        typeof(ChatNotifyUndercutGroupingSetting),
    ],
    settings)
{
    public override string Name { get; } = "Chat Notifications";

    public override string Description { get; } = "What messages should be displayed in chat?";
}
