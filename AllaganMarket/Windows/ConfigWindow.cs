using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using AllaganLib.Interface.Widgets;
using AllaganLib.Interface.Wizard;
using AllaganLib.Shared.Extensions;

using AllaganMarket.Mediator;
using AllaganMarket.Services;
using AllaganMarket.Settings;

using DalaMock.Host.Mediator;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

// ReSharper disable DisposeOnUsingVariable
namespace AllaganMarket.Windows;

public class ConfigWindow : ExtendedWindow, IDisposable
{
    private readonly Configuration configuration;
    private readonly IConfigurationWizardService<Configuration> configurationWizardService;
    private readonly SettingTypeConfiguration settingTypeConfiguration;
    private readonly VerticalSplitter verticalSplitter;
    private readonly List<IGrouping<SettingType, ISetting>> settings;
    private SettingType currentSettingType;

    public ConfigWindow(
        MediatorService mediatorService,
        ImGuiService imGuiService,
        Configuration configuration,
        IEnumerable<ISetting> settings,
        IConfigurationWizardService<Configuration> configurationWizardService,
        SettingTypeConfiguration settingTypeConfiguration)
        : base(mediatorService, imGuiService, "Allagan Market - Configuration")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        this.Size = new Vector2(600, 600);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.configuration = configuration;
        this.configurationWizardService = configurationWizardService;
        this.settingTypeConfiguration = settingTypeConfiguration;
        this.settings =
            [.. settings.GroupBy(c => c.Type).OrderBy(c => settingTypeConfiguration.GetCategoryOrder().IndexOf(c.Key))];
        this.Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.MenuBar;
        this.verticalSplitter = new VerticalSplitter(150, new Vector2(100, 200));
        this.currentSettingType = this.settings.First().Key;
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
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Main Window"))
                {
                    this.MediatorService.Publish(new OpenWindowMessage(typeof(MainWindow)));
                }

                if (ImGui.MenuItem("Report a Issue"))
                {
                    "https://github.com/Critical-Impact/AllaganMarket".OpenBrowser();
                }

                if (ImGui.MenuItem("Ko-Fi"))
                {
                    "https://ko-fi.com/critical_impact".OpenBrowser();
                }

                if (ImGui.MenuItem("Close"))
                {
                    this.IsOpen = false;
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Wizard"))
            {
                var hasNewFeatures = this.configurationWizardService.HasNewFeatures;
                using var disabled = ImRaii.Disabled(!hasNewFeatures);
                if (ImGui.MenuItem("Configure New Features"))
                {
                    this.MediatorService.Publish(new OpenWindowMessage(typeof(WizardWindow)));
                }

                disabled.Dispose();

                if (ImGui.MenuItem("Reconfigure All Features"))
                {
                    this.configurationWizardService.ClearFeaturesSeen();
                    this.MediatorService.Publish(new OpenWindowMessage(typeof(WizardWindow)));
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        this.verticalSplitter.Draw(
            () =>
            {
                ImGui.Text("Configuration");
                ImGui.Separator();
                foreach (var group in this.settings)
                {
                    if (ImGui.Selectable(group.Key.ToString(), group.Key == this.currentSettingType))
                    {
                        this.currentSettingType = group.Key;
                    }
                }
            },
            () =>
            {
                foreach (var group in this.settings)
                {
                    if (group.Key == this.currentSettingType)
                    {
                        foreach (var setting in group)
                        {
                            setting.Draw(this.configuration);
                        }
                    }
                }
            });
    }
}
