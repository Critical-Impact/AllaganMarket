using System.Numerics;
using System.Runtime.CompilerServices;

using DalaMock.Shared.Interfaces;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Dalamud.Bindings.ImGui;

namespace AllaganMarket.Services;

public class ImGuiService(IDalamudPluginInterface pluginInterface, ITextureProvider textureProvider)
    : AllaganLib.Interface.Services.ImGuiService(pluginInterface, textureProvider)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void VerticalCenter()
    {
        var offset = ImGui.GetWindowHeight() - ImGui.GetFrameHeightWithSpacing();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void HoverTooltip(string tooltip, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (tooltip.Length > 0 && ImGui.IsItemHovered(flags))
        {
            using var tt = ImRaii.Tooltip();
            ImGui.TextUnformatted(tooltip);
        }
    }

    public static bool DrawIconButton(
        IFont font,
        FontAwesomeIcon icon,
        ref float currentCursorX,
        string? tooltip = null,
        bool reverseCursor = false,
        Vector4? textColor = null)
    {
        var success = false;
        var iconString = icon.ToIconString();

        using var pushFont = ImRaii.PushFont(font.IconFont);
        using var pushColor = ImRaii.PushColor(ImGuiCol.Text, textColor ?? new Vector4(1, 1, 1, 1), textColor != null);
        var globalScale = ImGui.GetIO().FontGlobalScale;
        var iconSize = ImGui.CalcTextSize(iconString);
        var framePadding = ImGui.GetStyle().FramePadding * globalScale;

        var buttonSize = iconSize + (framePadding * 2);

        if (reverseCursor)
        {
            currentCursorX -= buttonSize.X + ImGui.GetStyle().ItemSpacing.X;
        }

        ImGui.SetCursorPosX(currentCursorX);

        if (ImGui.Button(iconString, buttonSize))
        {
            success = true;
        }

        pushColor.Pop();
        pushFont.Pop();

        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(tooltip))
        {
            using var tooltipScope = ImRaii.Tooltip();
            if (tooltipScope)
            {
                ImGui.Text(tooltip);
            }
        }

        return success;
    }
}
