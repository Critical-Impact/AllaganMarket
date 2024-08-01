namespace AllaganMarket.Services;

using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

/// <summary>
/// Wrapper for the item order module's retainer ID
/// </summary>
public class RetainerService : IRetainerService
{
    private readonly IClientState clientState;

    public RetainerService(IClientState clientState)
    {
        this.clientState = clientState;
    }

    public uint RetainerWorldId => this.clientState.LocalPlayer?.HomeWorld.Id ?? 0;

    public ulong RetainerId
    {
        get
        {
            unsafe
            {
                var clientInterfaceUiModule = Framework.Instance()->UIModule->GetItemOrderModule();
                var module = clientInterfaceUiModule;
                return module != null ? module->ActiveRetainerId : 0;
            }
        }
    }

    public unsafe uint RetainerGil => this.RetainerId == 0 ? 0 : InventoryManager.Instance()->GetRetainerGil();
}

public interface IRetainerService
{
    public uint RetainerWorldId { get; }

    public ulong RetainerId { get; }

    public uint RetainerGil { get; }
}