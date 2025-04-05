using System.Globalization;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Widgets;
using AllaganLib.Interface.Wizard;
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
using DalaMock.Host.Mediator;
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

        // Hosted Services
        containerBuilder.RegisterType<WindowService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<InstallerWindowService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<MarketPriceUpdaterService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<RetainerMarketService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<ATService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<MediatorService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<CommandService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<UniversalisWebsocketService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<UniversalisApiService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<InventoryService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<SaleTrackerService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<UndercutService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<AutoSaveService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<NotificationService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<CharacterMonitorService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<PluginBootService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<PluginStateService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<ConfigurationLoaderService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<LaunchButtonService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<DtrService>().AsImplementedInterfaces().AsSelf().SingleInstance();
        containerBuilder.RegisterType<HighlightingService>().AsImplementedInterfaces().AsSelf().SingleInstance();

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
