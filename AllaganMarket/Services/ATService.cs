using System.Threading;
using System.Threading.Tasks;

using DalaMock.Host.Mediator;

using Dalamud.Plugin.Services;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services;

public class ATService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly ICommandManager commandManager;

    public ATService(IPluginLog logger, MediatorService mediatorService, ICommandManager commandManager) : base(logger, mediatorService)
    {
        this.commandManager = commandManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.MediatorService.Subscribe<OpenMoreInformation>(this, this.OpenMoreInformationSub);
        return Task.CompletedTask;
    }

    public void OpenMoreInformationSub(OpenMoreInformation openMoreInformation)
    {
        this.commandManager.ProcessCommand("/moreinfo " + openMoreInformation.itemId);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
