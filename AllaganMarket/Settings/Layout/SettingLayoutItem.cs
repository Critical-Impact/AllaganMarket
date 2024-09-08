using System;

using Autofac;

namespace AllaganMarket.Settings.Layout;

public class SettingLayoutItem : ISettingLayoutItem
{
    private readonly Type settingType;
    private ISetting? setting;

    public SettingLayoutItem(Type settingType)
    {
        this.settingType = settingType;
    }

    public void Draw(Configuration configuration, int? labelSize = null, int? inputSize = null)
    {
        this.setting?.Draw(configuration, labelSize, inputSize);
    }

    public void Build(IComponentContext context)
    {
        this.setting = (ISetting)context.Resolve(this.settingType);
    }
}
