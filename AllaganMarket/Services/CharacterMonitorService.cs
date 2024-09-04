using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllaganMarket.Models;
using AllaganMarket.Services.Interfaces;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace AllaganMarket.Services;

/// <summary>
/// Stripped down version of CharacterMonitor from CCL.
/// </summary>
public class CharacterMonitorService(
    IFramework framework,
    IClientState clientState,
    IRetainerService retainerService,
    IAddonLifecycle addonLifecycle,
    IPluginLog pluginLog) : ICharacterMonitorService
{
    private ulong cachedRetainerId;

    public Dictionary<ulong, Character> Characters { get; private set; } = [];

    public Character? ActiveCharacter => this.Characters.GetValueOrDefault(this.ActiveCharacterId);

    public Character? ActiveRetainer => this.Characters.GetValueOrDefault(this.ActiveRetainerId);

    public ulong ActiveRetainerId => retainerService.RetainerId;

    public ulong ActiveCharacterId => clientState.LocalContentId;

    public bool IsLoggedIn => clientState.IsLoggedIn;

    public List<Character> GetRetainers(ulong character)
    {
        return this.Characters.Where(c => c.Value.CharacterType == CharacterType.Retainer && c.Value.OwnerId == character).Select(c => c.Value)
                   .ToList();
    }

    public void RemoveCharacter(ulong characterId)
    {
        var character = this.GetCharacterById(characterId);
        if (character != null)
        {
            var ownedRetainers = this.GetOwnedCharacters(characterId, CharacterType.Retainer);
            foreach (var retainer in ownedRetainers)
            {
                this.Characters.Remove(retainer.CharacterId);
            }

            this.Characters.Remove(characterId);
        }
    }

    public void OverrideActiveCharacter(ulong activeCharacter)
    {
    }

    public void OverrideActiveRetainer(ulong activeRetainer)
    {
    }

    public void LoadExistingData(Dictionary<ulong, Character> characters)
    {
        this.Characters = characters;
    }

    public Character? GetCharacterById(ulong characterId)
    {
        return this.Characters.GetValueOrDefault(characterId);
    }

    public List<Character> GetCharactersByType(CharacterType characterType, uint? worldId)
    {
        return this.Characters
                   .Where(
                       c => c.Value.CharacterType == characterType && (worldId == null || c.Value.WorldId == worldId))
                   .Select(c => c.Value).ToList();
    }

    public List<Character> GetOwnedCharacters(ulong ownerId, CharacterType characterType)
    {
        return this.Characters.Where(c => c.Value.OwnerId == ownerId && c.Value.CharacterType == characterType)
                   .Select(c => c.Value).ToList();
    }

    public List<uint> GetWorldIds(CharacterType characterType)
    {
        return this.Characters.Where(c => c.Value.CharacterType == characterType).Select(c => c.Value.WorldId)
                   .Distinct().ToList();
    }

    public bool IsCharacterKnown(ulong characterId)
    {
        return this.Characters.ContainsKey(characterId);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        clientState.Login += this.ClientStateOnLogin;
        addonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        framework.Update += this.FrameworkOnUpdate;
        framework.RunOnFrameworkThread(this.UpdatePlayerCharacter);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        clientState.Login -= this.ClientStateOnLogin;
        addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        framework.Update -= this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    private void ClientStateOnLogin()
    {
        this.UpdatePlayerCharacter();
    }

    private void UpdatePlayerCharacter()
    {
        if (clientState.LocalPlayer != null && clientState.LocalContentId != 0)
        {
            var newCharacter = new Character(
                CharacterType.Character,
                clientState.LocalContentId,
                clientState.LocalPlayer.Name.ToString(),
                clientState.LocalPlayer.HomeWorld.Id,
                clientState.LocalPlayer.ClassJob.Id,
                clientState.LocalPlayer.Level,
                0);
            this.Characters[clientState.LocalContentId] = newCharacter;
        }
    }

    private unsafe void UpdateRetainer()
    {
        var retainerId = this.cachedRetainerId;
        if (retainerId != 0 && clientState.LocalPlayer != null)
        {
            pluginLog.Verbose($"Updating retainer: {retainerId}");
            var span = RetainerManager.Instance()->Retainers;
            for (var index = 0; index < span.Length; index++)
            {
                var retainer = span[index];
                var displayOrder = RetainerManager.Instance()->DisplayOrder[index];
                if (retainer.RetainerId == retainerId)
                {
                    var retainerName = retainer.NameString.Trim();

                    var newRetainer = new Character(
                        CharacterType.Retainer,
                        retainerId,
                        retainerName,
                        clientState.LocalPlayer.HomeWorld.Id,
                        retainer.ClassJob,
                        retainer.Level,
                        displayOrder);
                    newRetainer.RetainerTown = retainer.Town;
                    newRetainer.OwnerId = clientState.LocalContentId;
                    this.Characters[retainerId] = newRetainer;
                }
            }
        }
    }

    private void FrameworkOnUpdate(IFramework framework1)
    {
        if (retainerService.RetainerId == 0 && this.cachedRetainerId != 0)
        {
            this.cachedRetainerId = 0;
        }
    }

    private void RetainerWindowOpened(AddonEvent type, AddonArgs args)
    {
        if (retainerService.RetainerId != 0)
        {
            this.cachedRetainerId = retainerService.RetainerId;
            this.UpdateRetainer();
        }
    }
}
