using System.Net.Http;
using System.Reflection;

using AllaganLib.Universalis.Services;

using AllaganMarket.Settings;

using DalaMock.Shared.Classes;
using DalaMock.Shared.Interfaces;

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
        IMarketBoard marketBoard)
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
            marketBoard)
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

        containerBuilder.RegisterType<WindowService>().SingleInstance();
        containerBuilder.RegisterType<FileDialogManager>().SingleInstance();
        containerBuilder.RegisterType<InstallerWindowService>().SingleInstance();
        containerBuilder.RegisterType<MarketPriceUpdaterService>().SingleInstance();
        containerBuilder.RegisterType<RetainerMarketService>().SingleInstance();
        containerBuilder.RegisterType<ImGuiService>().AsSelf().As<AllaganLib.Interface.Services.ImGuiService>().SingleInstance();
        containerBuilder.RegisterType<MediatorService>().SingleInstance();
        containerBuilder.RegisterType<ClientWebSocket>();
        containerBuilder.RegisterType<CommandService>().SingleInstance();
        containerBuilder.Register(c => new HttpClient()).As<HttpClient>();
        containerBuilder.RegisterType<UniversalisWebsocketService>().SingleInstance();
        containerBuilder.RegisterType<UniversalisApiService>().SingleInstance();
        containerBuilder.RegisterType<MainWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<ConfigWindow>().As<Window>().AsSelf().SingleInstance();
        containerBuilder.RegisterType<InventoryService>().As<IInventoryService>().SingleInstance();
        containerBuilder.RegisterType<SaleTrackerService>().SingleInstance();
        containerBuilder.RegisterType<UndercutService>().SingleInstance();
        containerBuilder.RegisterType<CharacterMonitorService>().As<ICharacterMonitorService>()
            .SingleInstance();
        containerBuilder.RegisterType<PluginBootService>().SingleInstance();
        containerBuilder.RegisterType<RetainerService>().As<IRetainerService>().SingleInstance();
        containerBuilder.RegisterType<SaleFilter>();
        containerBuilder.RegisterType<Font>().As<IFont>().SingleInstance();
        containerBuilder.Register<ExcelSheet<Item>>(
            s =>
            {
                var dataManger = s.Resolve<IDataManager>();
                return dataManger.GetExcelSheet<Item>()!;
            });

        //Add configuration
        containerBuilder.Register(
            s =>
            {
                var dalamudPluginInterface = s.Resolve<IDalamudPluginInterface>();
                var pluginLog = s.Resolve<IPluginLog>();
                Configuration configuration;
                try
                {
                    configuration = dalamudPluginInterface.GetPluginConfig() as Configuration ??
                                                  new Configuration();
                }
                catch (Exception e)
                {
                    pluginLog.Error(e, "Failed to load configuration");
                    configuration = new Configuration();
                }

                configuration.Initialize(dalamudPluginInterface);
                return configuration;
            }).SingleInstance();
    }

    public override void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService(p => p.GetRequiredService<WindowService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<CommandService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<InstallerWindowService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<MarketPriceUpdaterService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<IInventoryService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<SaleTrackerService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<RetainerMarketService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<ICharacterMonitorService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<PluginBootService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<UniversalisWebsocketService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<UniversalisApiService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<UndercutService>());
        serviceCollection.AddHostedService(p => p.GetRequiredService<MediatorService>()); 
    }
}
