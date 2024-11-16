using System.Threading;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class PluginStateService(IClientState clientState) : IHostedService
{
    public bool ShowWindows { get; set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        clientState.Login += this.ClientStateOnLogin;
        clientState.Logout += this.ClientStateOnLogout;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        clientState.Login -= this.ClientStateOnLogin;
        return Task.CompletedTask;
    }

    private void ClientStateOnLogout(int type, int code)
    {
        this.ShowWindows = false;
    }

    private void ClientStateOnLogin()
    {
        this.ShowWindows = true;
    }
}
