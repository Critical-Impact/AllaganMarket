namespace AllaganMarket.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Interfaces;
using Models;

/// <summary>
/// Stripped down version of CharacterMonitor from CCL
/// </summary>
public class CharacterMonitorService : ICharacterMonitorService
{
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IClientState clientState;
    private readonly IFramework framework;
    private readonly IPluginLog pluginLog;
    private readonly IRetainerService retainerService;

    private ulong cachedRetainerId;

    public CharacterMonitorService(
        IFramework framework,
        IClientState clientState,
        IRetainerService retainerService,
        IAddonLifecycle addonLifecycle,
        IPluginLog pluginLog)
    {
        this.framework = framework;
        this.clientState = clientState;
        this.retainerService = retainerService;
        this.addonLifecycle = addonLifecycle;
        this.pluginLog = pluginLog;
        this.Characters = new Dictionary<ulong, Character>();
    }

    public Dictionary<ulong, Character> Characters { get; private set; }

    public List<Character> GetRetainers(ulong character)
    {
        return this.Characters.Where(c => c.Value.CharacterType == CharacterType.Retainer).Select(c => c.Value)
            .ToList();
    }

    public Character? ActiveCharacter => this.Characters.GetValueOrDefault(this.ActiveCharacterId);

    public Character? ActiveRetainer => this.Characters.GetValueOrDefault(this.ActiveRetainerId);

    public ulong ActiveRetainerId => this.retainerService.RetainerId;

    public ulong ActiveCharacterId => this.clientState.LocalContentId;

    public bool IsLoggedIn => this.clientState.IsLoggedIn;

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
            .Where(c => c.Value.CharacterType == characterType && (worldId == null || c.Value.WorldId == worldId))
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
        this.clientState.Login += this.ClientStateOnLogin;
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        this.framework.Update += this.FrameworkOnUpdate;
        this.framework.RunOnFrameworkThread(this.UpdatePlayerCharacter);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.clientState.Login -= this.ClientStateOnLogin;
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", this.RetainerWindowOpened);
        this.framework.Update -= this.FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    private void ClientStateOnLogin()
    {
        this.UpdatePlayerCharacter();
    }

    private void UpdatePlayerCharacter()
    {
        if (this.clientState.LocalPlayer != null && this.clientState.LocalContentId != 0)
        {
            var newCharacter = new Character(
                CharacterType.Character,
                this.clientState.LocalContentId,
                this.clientState.LocalPlayer.Name.ToString(),
                this.clientState.LocalPlayer.HomeWorld.Id,
                this.clientState.LocalPlayer.ClassJob.Id,
                this.clientState.LocalPlayer.Level);
            this.Characters[this.clientState.LocalContentId] = newCharacter;
        }
    }

    private unsafe void UpdateRetainer()
    {
        var retainerId = this.cachedRetainerId;
        if (retainerId != 0 && this.clientState.LocalPlayer != null)
        {
            this.pluginLog.Verbose($"Updating retainer: {retainerId}");
            foreach (var retainer in RetainerManager.Instance()->Retainers)
            {
                if (retainer.RetainerId == retainerId)
                {
                    var retainerName = retainer.NameString.Trim();

                    var newRetainer = new Character(
                        CharacterType.Retainer,
                        retainerId,
                        retainerName,
                        this.clientState.LocalPlayer.HomeWorld.Id,
                        retainer.ClassJob,
                        retainer.Level);
                    newRetainer.RetainerTown = retainer.Town;
                    newRetainer.OwnerId = this.clientState.LocalContentId;
                    this.Characters[retainerId] = newRetainer;
                }
            }
        }
    }

    private void FrameworkOnUpdate(IFramework framework1)
    {
        if (this.retainerService.RetainerId == 0 && this.cachedRetainerId != 0)
        {
            this.cachedRetainerId = 0;
        }
    }

    private void RetainerWindowOpened(AddonEvent type, AddonArgs args)
    {
        if (this.retainerService.RetainerId != 0)
        {
            this.cachedRetainerId = this.retainerService.RetainerId;
            this.UpdateRetainer();
        }
    }
}
