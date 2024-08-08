using System.Threading;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Models;

public class PluginStateService : IHostedService
{
    private readonly IClientState clientState;

    public PluginStateService(IClientState clientState)
    {
        this.clientState = clientState;
    }

    private void ClientStateOnLogin()
    {
        this.ShowWindows = true;
    }

    public bool ShowWindows { get; set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login += this.ClientStateOnLogin;
        this.clientState.Logout += this.ClientStateOnLogout;
        return Task.CompletedTask;
    }

    private void ClientStateOnLogout()
    {
        this.ShowWindows = false;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login -= this.ClientStateOnLogin;
        return Task.CompletedTask;
    }
}
