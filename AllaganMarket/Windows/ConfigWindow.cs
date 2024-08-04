using System.Collections.Generic;
using System.Linq;

using AllaganMarket.Settings;

namespace AllaganMarket.Windows;

using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Services;

public class ConfigWindow : ExtendedWindow, IDisposable
{
    private Configuration configuration;
    private readonly List<IGrouping<SettingType, ISetting>> settings;

    public ConfigWindow(MediatorService mediatorService, ImGuiService imGuiService, Configuration configuration, IEnumerable<ISetting> settings)
        : base(mediatorService, imGuiService, "Allagan Market - Configuration")
    {
        this.Size = new Vector2(232, 200);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.configuration = configuration;
        this.settings = settings.GroupBy(c => c.Type).ToList();
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
        foreach (var group in this.settings)
        {
            ImGui.Text(group.Key.ToString());
            foreach (var setting in group)
            {
                setting.Draw(this.configuration);
            }
            ImGui.NewLine();
        }
    }
}
