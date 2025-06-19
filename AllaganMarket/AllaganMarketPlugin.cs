using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Widgets;
using AllaganLib.Interface.Wizard;
using AllaganLib.Universalis.Models;
using AllaganLib.Universalis.Services;

using AllaganMarket.Filtering;
using AllaganMarket.Models;
using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;
using AllaganMarket.Settings;
using AllaganMarket.Settings.Layout;
using AllaganMarket.Tables;
using AllaganMarket.Tables.Fields;
using AllaganMarket.Windows;

using Autofac;

using DalaMock.Host.Hosting;
using DalaMock.Shared.Classes;
using DalaMock.Shared.Interfaces;

using Dalamud.Game.Text;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Lumina;
using Lumina.Excel;
using Lumina.Excel.Sheets;

using Microsoft.Extensions.DependencyInjection;

namespace AllaganMarket;

public class AllaganMarketPlugin : HostedPlugin
{
    private readonly IGameGui gameGui;

    public AllaganMarketPlugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        ICommandManager commandManager,
        ITextureProvider textureProvider,
        IGameInteropProvider gameInteropProvider,
        IAddonLifecycle addonLifecycle,
        IClientState clientState,
        IGameInventory gameInventory,
        IFramework framework,
        IDataManager dataManager,
        IChatGui chatGui,
        IMarketBoard marketBoard,
        ITitleScreenMenu titleScreenMenu,
        IDtrBar dtrBar,
        IGameGui gameGui,
        ICondition condition)
        : base(
            pluginInterface,
            pluginLog,
            commandManager,
            textureProvider,
            gameInteropProvider,
            addonLifecycle,
            clientState,
            gameInventory,
            framework,
            dataManager,
            chatGui,
            marketBoard,
            titleScreenMenu,
            dtrBar,
            gameGui,
            condition)
    {
        this.gameGui = gameGui;
        this.CreateHost();
        this.Start();
    }

    private List<Type> HostedServices { get; } = new()
    {
        typeof(WindowService),
        typeof(InstallerWindowService),
        typeof(MarketPriceUpdaterService),
        typeof(RetainerMarketService),
        typeof(ATService),
        typeof(CommandService),
        typeof(UniversalisWebsocketService),
        typeof(UniversalisApiService),
        typeof(InventoryService),
        typeof(SaleTrackerService),
        typeof(UndercutService),
        typeof(AutoSaveService),
        typeof(NotificationService),
        typeof(CharacterMonitorService),
        typeof(PluginBootService),
        typeof(PluginStateService),
        typeof(ConfigurationLoaderService),
        typeof(LaunchButtonService),
        typeof(DtrService),
        typeof(HighlightingService),
    };

    public List<Type> GetHostedServices()
    {
        var hostedServices = this.HostedServices.ToList();
        Dictionary<Type, Type> replacements = new();
        this.ReplaceHostedServices(replacements);
        foreach (var replacement in replacements)
        {
            hostedServices.Remove(replacement.Key);
            hostedServices.Add(replacement.Value);
        }

        return hostedServices;
    }

    public virtual void ReplaceHostedServices(Dictionary<Type, Type> replacements)
    {

    }

    public override HostedPluginOptions ConfigureOptions()
    {
        return new HostedPluginOptions()
        {
            UseMediatorService = true,
        };
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        var dataAccess = Assembly.GetExecutingAssembly();

        containerBuilder.Register(
            s =>
            {
                // Assume we only ever have one number format but we could just make this keyed later
                var gilNumberFormat =
                    (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
                gilNumberFormat.CurrencySymbol = SeIconChar.Gil.ToIconString();
                gilNumberFormat.CurrencyDecimalDigits = 0;
                return gilNumberFormat;
            });

        containerBuilder.RegisterAssemblyTypes(dataAccess)
                        .Where(t => t.Name.EndsWith("Setting"))
                        .AsSelf()
                        .AsImplementedInterfaces();

        containerBuilder.RegisterAssemblyTypes(dataAccess)
                        .Where(t => t.Name.EndsWith("Feature"))
                        .AsSelf()
                        .AsImplementedInterfaces();

        containerBuilder.RegisterAssemblyTypes(dataAccess)
                        .Where(t => t.Name.EndsWith("Column"))
                        .AsSelf()
                        .AsImplementedInterfaces();

        containerBuilder.RegisterAssemblyTypes(dataAccess)
                        .Where(t => t.Name.EndsWith("SettingLayout"))
                        .AsSelf()
                        .As<SettingPage>();

        var interfacesAssembly = typeof(StringColumnFilter).Assembly;

        containerBuilder.RegisterAssemblyTypes(interfacesAssembly)
                        .Where(t => t.Name.EndsWith("ColumnFilter"))
                        .AsSelf()
                        .AsImplementedInterfaces();

        foreach (var hostedService in this.GetHostedServices())
        {
            containerBuilder.RegisterType(hostedService).AsSelf().AsImplementedInterfaces().SingleInstance();
        }


        containerBuilder.RegisterType<SettingTypeConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ImGuiMenus>().SingleInstance();
        containerBuilder.RegisterType<RetainerService>().As<IRetainerService>().SingleInstance();
        containerBuilder.RegisterType<CsvLoaderService>().SingleInstance();
        containerBuilder.RegisterType<ImGuiService>().AsSelf().As<AllaganLib.Interface.Services.ImGuiService>()
                        .SingleInstance();
        containerBuilder.RegisterType<SaleItemTable>().SingleInstance();
        containerBuilder.RegisterType<SoldItemTable>().SingleInstance();
        containerBuilder.RegisterType<SaleSummaryTable>().SingleInstance();
        containerBuilder.RegisterType<SearchResultConfiguration>();
        containerBuilder.RegisterType<FileDialogManager>().SingleInstance();
        containerBuilder.RegisterType<DalamudFileDialogManager>().As<IFileDialogManager>().SingleInstance();
        containerBuilder.RegisterType<AtkOrderService>().As<IAtkOrderService>().SingleInstance();
        containerBuilder.RegisterType<GameInterfaceService>().As<IGameInterfaceService>().SingleInstance();

        containerBuilder.RegisterType<ClientWebSocket>();
        containerBuilder.Register(c => new HttpClient()).As<HttpClient>();
        containerBuilder.Register<UniversalisUserAgent>(c =>
        {
            var pluginInterface = c.Resolve<IDalamudPluginInterface>();
            #if DEBUG
            return new UniversalisUserAgent(pluginInterface.InternalName, "DEV");
            #else
            return new UniversalisUserAgent(pluginInterface.InternalName, pluginInterface.Manifest.AssemblyVersion.ToString());
            #endif
        });

        // Windows
        containerBuilder.RegisterType<MainWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<ConfigWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<WizardWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<RetainerListOverlayWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<RetainerSellListOverlayWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<RetainerSellOverlayWindow>().As<Window>().AsSelf().SingleInstance();

        containerBuilder.Register(c => c.Resolve<IDataManager>().GameData).SingleInstance();

        // Custom Widgets
        containerBuilder.RegisterType<SaleSummaryGroupFormField>();
        containerBuilder.RegisterType<SaleSummaryDateRangeFormField>();
        containerBuilder.RegisterType<SaleSummaryTimeSpanFormField>();

        containerBuilder.RegisterType<SaleFilter>().SingleInstance();
        containerBuilder.RegisterType<SaleSummary>();
        containerBuilder.Register(
            c => new WizardWidgetSettings() { PluginName = "Allagan Market", LogoPath = "logo_small" });
        containerBuilder.RegisterType<WizardWidget<Configuration>>().AsSelf().AsImplementedInterfaces()
                        .SingleInstance();
        containerBuilder.RegisterType<ConfigurationWizardService<Configuration>>().AsSelf().AsImplementedInterfaces()
                        .SingleInstance();
        containerBuilder.RegisterType<Font>().As<IFont>().SingleInstance();
        containerBuilder.RegisterType<TimeSpanHumanizerCache>();

        // Sheets
        containerBuilder.RegisterGeneric((context, parameters) =>
                        {
                            var gameData = context.Resolve<GameData>();
                            var method = typeof(GameData).GetMethod(nameof(GameData.GetExcelSheet))
                                                         ?.MakeGenericMethod(parameters);
                            var sheet = method!.Invoke(gameData, [null, null])!;
                            return sheet;
                        })
                        .As(typeof(ExcelSheet<>));

        containerBuilder.Register(
            s =>
            {
                var configurationLoaderService = s.Resolve<ConfigurationLoaderService>();
                return configurationLoaderService.GetConfiguration();
            }).SingleInstance();
    }

    public override void ConfigureServices(IServiceCollection serviceCollection)
    {
    }
}
