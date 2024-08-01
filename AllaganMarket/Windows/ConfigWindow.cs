namespace AllaganMarket.Windows;

using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Services;

public class ConfigWindow : ExtendedWindow, IDisposable
{
    private Configuration configuration;

    public ConfigWindow(MediatorService mediatorService, ImGuiService imGuiService, Configuration configuration)
        : base(mediatorService, imGuiService, "A Wonderful Configuration Window###With a constant ID")
    {
        this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                     ImGuiWindowFlags.NoScrollWithMouse;

        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.Always;

        this.configuration = configuration;
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (this.configuration.IsConfigWindowMovable)
        {
            this.Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            this.Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var configValue = this.configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.Checkbox("Random Config Bool", ref configValue))
        {
            this.configuration.SomePropertyToBeSavedAndWithADefault = configValue;

            // can save immediately on change, if you don't want to provide a "Save and Close" button
            this.configuration.Save();
        }

        var movable = this.configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            this.configuration.IsConfigWindowMovable = movable;
            this.configuration.Save();
        }
    }
}