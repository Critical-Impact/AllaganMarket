using System.Linq;
using System.Net.Http;
using System.Reflection;

using AllaganLib.Data.Service;
using AllaganLib.Interface.Grid;
using AllaganLib.Interface.Grid.ColumnFilters;
using AllaganLib.Interface.Widgets;
using AllaganLib.Interface.Wizard;
using AllaganLib.Universalis.Services;

using AllaganMarket.Grids;
using AllaganMarket.Models;
using AllaganMarket.Settings;

using DalaMock.Host.Mediator;
using DalaMock.Shared.Classes;
using DalaMock.Shared.Interfaces;

using Lumina;

namespace AllaganMarket;

using System;
using System.Globalization;
using System.Net.WebSockets;
using Windows;
using Autofac;
using DalaMock.Host;
using DalaMock.Host.Hosting;
using DalaMock.Shared;
using Dalamud.Game.Text;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Filtering;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.DependencyInjection;
using Services;
using Services.Interfaces;

public class AllaganMarketPlugin : HostedPlugin
{
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
        IDtrBar dtrBar)
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
            dtrBar)
    {
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
                NumberFormatInfo gilNumberFormat =
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

        var interfacesAssembly = typeof(StringColumnFilter).Assembly;

        containerBuilder.RegisterAssemblyTypes(interfacesAssembly)
                        .Where(t => t.Name.EndsWith("ColumnFilter"))
                        .AsSelf()
                        .AsImplementedInterfaces();

        //Services
        containerBuilder.RegisterType<WindowService>().SingleInstance();
        containerBuilder.RegisterType<InstallerWindowService>().SingleInstance();
        containerBuilder.RegisterType<MarketPriceUpdaterService>().SingleInstance();
        containerBuilder.RegisterType<RetainerMarketService>().SingleInstance();
        containerBuilder.RegisterType<ATService>().SingleInstance();
        containerBuilder.RegisterType<ImGuiService>().AsSelf().As<AllaganLib.Interface.Services.ImGuiService>().SingleInstance();
        containerBuilder.RegisterType<MediatorService>().SingleInstance();
        containerBuilder.RegisterType<CommandService>().SingleInstance();
        containerBuilder.RegisterType<UniversalisWebsocketService>().SingleInstance();
        containerBuilder.RegisterType<UniversalisApiService>().SingleInstance();
        containerBuilder.RegisterType<InventoryService>().As<IInventoryService>().SingleInstance();
        containerBuilder.RegisterType<SaleTrackerService>().SingleInstance();
        containerBuilder.RegisterType<UndercutService>().SingleInstance();
        containerBuilder.RegisterType<CsvLoaderService>().SingleInstance();
        containerBuilder.RegisterType<AutoSaveService>().SingleInstance();
        containerBuilder.RegisterType<CharacterMonitorService>().As<ICharacterMonitorService>()
                        .SingleInstance();
        containerBuilder.RegisterType<PluginBootService>().SingleInstance();
        containerBuilder.RegisterType<PluginStateService>().SingleInstance();
        containerBuilder.RegisterType<ConfigurationLoaderService>().SingleInstance();
        containerBuilder.RegisterType<RetainerService>().As<IRetainerService>().SingleInstance();
        containerBuilder.RegisterType<SettingTypeConfiguration>().SingleInstance();
        containerBuilder.RegisterType<LaunchButtonService>().SingleInstance();
        containerBuilder.RegisterType<DtrService>().SingleInstance();

        containerBuilder.RegisterType<SaleItemTable>().SingleInstance();
        containerBuilder.RegisterType<SoldItemTable>().SingleInstance();
        containerBuilder.RegisterType<SearchResultConfiguration>();
        containerBuilder.RegisterType<FileDialogManager>().SingleInstance();
        containerBuilder.RegisterType<DalamudFileDialogManager>().As<IFileDialogManager>().SingleInstance();

        containerBuilder.RegisterType<ClientWebSocket>();
        containerBuilder.Register(c => new HttpClient()).As<HttpClient>();

        // Windows
        containerBuilder.RegisterType<MainWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<ConfigWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<WizardWindow>().As<Window>().AsSelf().SingleInstance();

        containerBuilder.Register(c => c.Resolve<IDataManager>().GameData).SingleInstance();


        containerBuilder.RegisterType<SaleFilter>().SingleInstance();
        containerBuilder.Register(c => new WizardWidgetSettings() { PluginName = "Allagan Market", LogoPath = "logo_small" });
        containerBuilder.RegisterType<WizardWidget<Configuration>>().AsSelf().AsImplementedInterfaces().SingleInstance();
        containerBuilder.RegisterType<ConfigurationWizardService<Configuration>>().AsSelf().AsImplementedInterfaces().SingleInstance();
        containerBuilder.RegisterType<Font>().As<IFont>().SingleInstance();

        // Sheets
        containerBuilder.Register<ExcelSheet<Item>>(
            s =>
            {
                var dataManger = s.Resolve<IDataManager>();
                return dataManger.GetExcelSheet<Item>()!;
            }).SingleInstance();

        containerBuilder.Register<ExcelSheet<ClassJob>>(
            s =>
            {
                var dataManger = s.Resolve<IDataManager>();
                return dataManger.GetExcelSheet<ClassJob>()!;
            }).SingleInstance();

        containerBuilder.Register<ExcelSheet<World>>(
            s =>
            {
                var dataManger = s.Resolve<IDataManager>();
                return dataManger.GetExcelSheet<World>()!;
            }).SingleInstance();

        containerBuilder.Register(
            s =>
            {
                var configurationLoaderService = s.Resolve<ConfigurationLoaderService>();
                return configurationLoaderService.GetConfiguration();
            }).SingleInstance();
    }

    public override void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService(p => p.GetRequiredService<PluginBootService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<PluginStateService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<WindowService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<CommandService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<InstallerWindowService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<MarketPriceUpdaterService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<IInventoryService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<SaleTrackerService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<RetainerMarketService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<ICharacterMonitorService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<UniversalisWebsocketService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<UniversalisApiService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<UndercutService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<MediatorService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<LaunchButtonService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<DtrService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<ConfigurationLoaderService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<AutoSaveService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<ATService>());
    }
}
