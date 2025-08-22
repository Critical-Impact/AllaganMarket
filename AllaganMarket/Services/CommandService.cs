using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AllaganLib.Shared.Windows;

using AllaganMarket.Mediator;
using AllaganMarket.Windows;

using DalaMock.Host.Mediator;

using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class CommandService : IHostedService
{
    private readonly ICommandManager commandManager;
    private readonly MediatorService mediatorService;

    private readonly List<CommandRegistration> commands;

    public CommandService(ICommandManager commandManager, MediatorService mediatorService)
    {
        this.commandManager = commandManager;
        this.mediatorService = mediatorService;
        this.commands = new List<CommandRegistration>
        {
            new("/allaganmarket",
                "Shows the Allagan Market main window.",
                (args) => this.mediatorService.Publish(new ToggleWindowMessage(typeof(MainWindow)))),

            new("/amarket",
                "Alias for /allaganmarket.",
                (args) => this.mediatorService.Publish(new ToggleWindowMessage(typeof(MainWindow)))),

            new("/amconfig",
                "Shows the Allagan Market configuration window.",
                (args) => this.mediatorService.Publish(new ToggleWindowMessage(typeof(ConfigWindow)))),

            new("/amdebug",
                "Shows the Allagan Market debug window.",
                (args) => this.mediatorService.Publish(new ToggleWindowMessage(typeof(AllaganDebugWindow)))),
        };
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var cmd in this.commands)
        {
            this.commandManager.AddHandler(cmd.Name, new CommandInfo((_, args) => cmd.Action(args))
            {
                HelpMessage = cmd.HelpMessage,
            });
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var cmd in this.commands)
        {
            this.commandManager.RemoveHandler(cmd.Name);
        }

        return Task.CompletedTask;
    }

    private record CommandRegistration(string Name, string HelpMessage, Action<string> Action);
}
