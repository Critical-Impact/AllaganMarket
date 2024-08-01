namespace AllaganMarket.Services;

using System.Threading;
using System.Threading.Tasks;
using Windows;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;

public class CommandService : IHostedService
{
    private const string CommandName = "/pmycommand";

    public CommandService(ICommandManager commandManager, MainWindow mainWindow)
    {
        this.CommandManager = commandManager;
        this.MainWindow = mainWindow;
    }

    public ICommandManager CommandManager { get; }

    public MainWindow MainWindow { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.CommandManager.AddHandler(
            CommandName,
            new CommandInfo(this.OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
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