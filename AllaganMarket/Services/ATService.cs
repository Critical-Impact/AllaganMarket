using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Mediator;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class ATService(IPluginLog logger, MediatorService mediatorService, ICommandManager commandManager)
    : DisposableMediatorSubscriberBase(logger, mediatorService), IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.MediatorService.Subscribe<OpenMoreInformation>(this, this.OpenMoreInformationSub);
        return Task.CompletedTask;
    }

    public void OpenMoreInformationSub(OpenMoreInformation openMoreInformation)
    {
        commandManager.ProcessCommand("/moreinfo " + openMoreInformation.ItemId);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
