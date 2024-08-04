using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace AllaganMarket.Services;

using System.Runtime.CompilerServices;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

public class ImGuiService : AllaganLib.Interface.Services.ImGuiService
{
    public ImGuiService(IDalamudPluginInterface pluginInterface, ITextureProvider textureProvider) : base(pluginInterface, textureProvider)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void VerticalCenter()
    {
        var offset = ImGui.GetWindowHeight() - ImGui.GetFrameHeightWithSpacing();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void HoverTooltip(string tooltip, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (tooltip.Length > 0 && ImGui.IsItemHovered(flags))
        {
            using var tt = ImRaii.Tooltip();
            ImGui.TextUnformatted(tooltip);
        }
    }
}
