using System.Collections.Generic;

using Autofac;

namespace AllaganMarket.Settings.Layout;

public abstract class SettingPage
{
    private readonly IComponentContext componentContext;
    private List<ISettingLayoutItem>? layoutItems = null;

    public abstract SettingType SettingType { get;}

    public SettingPage(IComponentContext componentContext)
    {
        this.componentContext = componentContext;
    }

    public List<ISettingLayoutItem> LayoutItems
    {
        get
        {
            if (this.layoutItems != null)
            {
                return this.layoutItems;
            }

            var builtLayoutItems = this.GenerateLayoutItems();
            foreach (var layoutItem in builtLayoutItems)
            {
                layoutItem.Build(this.componentContext);
            }

            this.layoutItems = builtLayoutItems;
            return this.layoutItems;
        }
    }

    public abstract List<ISettingLayoutItem> GenerateLayoutItems();

    public void Draw(Configuration configuration, int? labelSize = null, int? inputSize = null)
    {
        foreach (var layoutItem in this.LayoutItems)
        {
            layoutItem.Draw(configuration, labelSize, inputSize);
        }
    }
}
