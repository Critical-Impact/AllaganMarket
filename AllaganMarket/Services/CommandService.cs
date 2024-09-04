using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Windows;

using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class CommandService(ICommandManager commandManager, MainWindow mainWindow) : IHostedService
{
    private readonly string[] commandName = { "/allaganmarket" , "/amarket"};

    public ICommandManager CommandManager { get; } = commandManager;

    public MainWindow MainWindow { get; } = mainWindow;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < commandName.Length; i++)
        {
            this.CommandManager.AddHandler(
            commandName[i],
            new CommandInfo(this.OnCommand)
            {
                HelpMessage = "Shows the Allagan Market main window.",
            });
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < commandName.Length; i++)
        {
            this.CommandManager.RemoveHandler(commandName[i]);
        }

        return Task.CompletedTask;
    }

    private void OnCommand(string command, string arguments)
    {
        this.MainWindow.Toggle();
    }
}
