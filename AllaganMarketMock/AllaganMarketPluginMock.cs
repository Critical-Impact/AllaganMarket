using System;
using System.Collections.Generic;

using AllaganLib.Universalis.Models;

using AllaganMarket;
using AllaganMarket.Services;
using AllaganMarket.Services.Interfaces;

using Autofac;

using DalaMock.Core.Mocks;
using DalaMock.Core.Windows;
using DalaMock.Shared.Interfaces;

using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AllaganMarketMock;

public class AllaganMarketPluginMock : AllaganMarketPlugin
{
    public AllaganMarketPluginMock(
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
        ICondition condition,
        IObjectTable objectTable,
        IPlayerState playerState)
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
        condition,
        objectTable,
        playerState)
    {
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        base.ConfigureContainer(containerBuilder);
        containerBuilder.RegisterType<MockWindowSystem>().AsSelf().As<IWindowSystem>().SingleInstance();
        containerBuilder.RegisterType<MockFileDialogManager>().AsSelf().As<IFileDialogManager>().SingleInstance();
        containerBuilder.RegisterType<MockFont>().AsSelf().As<IFont>().SingleInstance();
        containerBuilder.RegisterType<MockRetainerService>().AsSelf().As<IRetainerService>().SingleInstance();
        containerBuilder.RegisterType<MockWindow>().AsSelf().As<Window>().SingleInstance();
        containerBuilder.RegisterType<MockCharacterWindow>().AsSelf().As<Window>().SingleInstance();
        containerBuilder.RegisterType<MockBootService>().AsSelf().AsImplementedInterfaces().SingleInstance();
        containerBuilder.Register<UniversalisUserAgent>(c =>
        {
            return new UniversalisUserAgent("AllaganMarket", "DEV");
        });
    }

    public override void ReplaceHostedServices(Dictionary<Type, Type> replacements)
    {
        replacements.Add(typeof(InventoryService), typeof(MockInventoryService));
        replacements.Add(typeof(GameInterfaceService), typeof(MockGameInterfaceService));
        replacements.Add(typeof(RetainerMarketService), typeof(MockRetainerMarketService));
    }
}
