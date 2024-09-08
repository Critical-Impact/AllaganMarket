using Autofac;

namespace AllaganMarket.Settings.Layout;

public interface ISettingLayoutItem
{
    public void Draw(Configuration configuration, int? labelSize = null, int? inputSize = null);

    public void Build(IComponentContext context);
}
