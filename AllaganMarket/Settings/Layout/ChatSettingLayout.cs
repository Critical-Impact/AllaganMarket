using System.Collections.Generic;

using Autofac;

namespace AllaganMarket.Settings.Layout;

public class ChatSettingLayout : SettingPage
{
    public ChatSettingLayout(IComponentContext componentContext) : base(componentContext)
    {
    }

    public override SettingType SettingType => SettingType.Chat;

    public override List<ISettingLayoutItem> GenerateLayoutItems()
    {
        return
        [
            new TextLayoutItem("Undercuts - General"),
            new SeparatorLayoutItem(),
            new SettingLayoutItem(typeof(ChatNotifyUndercutSetting)),
            new SettingLayoutItem(typeof(ChatNotifyUndercutCharacterSetting)),
            new SettingLayoutItem(typeof(ChatNotifyUndercutGroupingSetting)),
            new SettingLayoutItem(typeof(ChatNotifyUndercutChatTypeSetting)),
            new SpacerLayoutItem(),
            new TextLayoutItem("Undercuts - On Login"),
            new SeparatorLayoutItem(),
            new SettingLayoutItem(typeof(ChatNotifyUndercutLoginSetting)),
            new SettingLayoutItem(typeof(ChatNotifyUndercutLoginChatTypeSetting)),
            new SpacerLayoutItem(),
            new TextLayoutItem("Item Sold"),
            new SeparatorLayoutItem(),
            new SettingLayoutItem(typeof(ChatNotifySoldItemSetting)),
            new SettingLayoutItem(typeof(ChatNotifySoldItemChatTypeSetting))
        ];
    }
}
