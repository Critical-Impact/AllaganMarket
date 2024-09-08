using Autofac;

using ImGuiNET;

namespace AllaganMarket.Settings.Layout;

public class TextLayoutItem : ISettingLayoutItem
{
    private readonly string text;

    public TextLayoutItem(string text)
    {
        this.text = text;
    }

    public void Draw(Configuration configuration, int? labelSize = null, int? inputSize = null)
    {
        ImGui.Text(this.text);
    }

    public void Build(IComponentContext context)
    {
    }
}
