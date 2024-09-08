using Autofac;

using ImGuiNET;

namespace AllaganMarket.Settings.Layout;

public class SpacerLayoutItem : ISettingLayoutItem
{
    public void Draw(Configuration configuration, int? labelSize = null, int? inputSize = null)
    {
        ImGui.NewLine();
    }

    public void Build(IComponentContext context)
    {
    }
}
