using AllaganMarket.Services.Interfaces;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace AllaganMarket.Services;

/// <summary>
/// Wrapper for the item order module's retainer ID.
/// </summary>
public class RetainerService(IClientState clientState) : IRetainerService
{
    public uint RetainerWorldId => clientState.LocalPlayer?.HomeWorld.Id ?? 0;

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
