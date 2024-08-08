using System.Collections.Generic;

using AllaganLib.Interface.FormFields;
using AllaganLib.Interface.Wizard;

using AllaganMarket.Settings;

namespace AllaganMarket.Features;

public class ChatNotificationsFeature : Feature<Configuration>
{
    public ChatNotificationsFeature(IEnumerable<IFormField<Configuration>> settings)
        : base(
            [
                typeof(ChatNotifyUndercutSetting),
                typeof(ChatNotifyUndercutLoginSetting),
                typeof(ChatNotifyUndercutLoginCharacterSetting),
            ],
            settings)
    {
    }

    public override string Name { get; } = "Chat Notifications";

    public override string Description { get; } = "What messages should be displayed in chat?";
}
