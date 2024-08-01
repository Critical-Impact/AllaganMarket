using AllaganMarket.Services;

namespace AllaganMarketMock;

using AllaganMarket;
using Autofac;
using DalaMock.Core.Mocks;
using DalaMock.Core.Windows;
using DalaMock.Shared.Interfaces;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

public class AllaganMarketPluginMock : AllaganMarketPlugin
{
    public AllaganMarketPluginMock(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, ICommandManager commandManager, ITextureProvider textureProvider, IGameInteropProvider gameInteropProvider, IAddonLifecycle addonLifecycle, IClientState clientState, IGameInventory gameInventory, IFramework framework, IDataManager dataManager, IChatGui chatGui, IMarketBoard marketBoard)
        : base(pluginInterface, pluginLog, commandManager, textureProvider, gameInteropProvider, addonLifecycle, clientState, gameInventory, framework, dataManager, chatGui, marketBoard)
    {
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        base.ConfigureContainer(containerBuilder);
        containerBuilder.RegisterType<MockWindowSystem>().As<IWindowSystem>();
        containerBuilder.RegisterType<MockFileDialogManager>().As<IFileDialogManager>();
        containerBuilder.RegisterType<MockFont>().As<IFont>();
        containerBuilder.RegisterType<MockRetainerService>().As<IRetainerService>();
    }
}
