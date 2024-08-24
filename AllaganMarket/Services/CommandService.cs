using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Windows;

using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class CommandService(ICommandManager commandManager, MainWindow mainWindow) : IHostedService
{
    private const string CommandName = "/allaganmarket";

    public ICommandManager CommandManager { get; } = commandManager;

    public MainWindow MainWindow { get; } = mainWindow;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.CommandManager.AddHandler(
            CommandName,
            new CommandInfo(this.OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp",
            });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.CommandManager.RemoveHandler(CommandName);
        return Task.CompletedTask;
    }

    private void OnCommand(string command, string arguments)
    {
        this.MainWindow.Toggle();
    }
}
