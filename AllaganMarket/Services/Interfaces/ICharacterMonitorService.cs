using System.Collections.Generic;

using AllaganMarket.Models;

using Microsoft.Extensions.Hosting;

namespace AllaganMarket.Services.Interfaces;

/// <summary>
/// An interface representing the character monitoring service.
/// </summary>
public interface ICharacterMonitorService : IHostedService
{
    Dictionary<ulong, Character> Characters { get; }

    public Character? ActiveCharacter { get; }

    public Character? ActiveRetainer { get; }

    ulong ActiveRetainerId { get; }

    ulong ActiveCharacterId { get; }

    public bool IsLoggedIn { get; }

    List<Character> GetRetainers(ulong character);

    void RemoveCharacter(ulong characterId);

    void OverrideActiveCharacter(ulong activeCharacter);

    void OverrideActiveRetainer(ulong activeRetainer);

    void LoadExistingData(Dictionary<ulong, Character> characters);

    public Character? GetCharacterById(ulong characterId);

    public List<Character> GetCharactersByType(CharacterType characterType, uint? worldId);

    /// <summary>
    /// Returns a list of characters owned by a character(retainers owned by a character for example).
    /// </summary>
    /// <param name="ownerId">the id of the owner.</param>
    /// <param name="characterType">the type of character. for future use.</param>
    /// <returns>A list of owned characters of the given type.</returns>
    public List<Character> GetOwnedCharacters(ulong ownerId, CharacterType characterType);

    /// <summary>
    /// Returns a list of the unique world IDs that occur across all characters.
    /// </summary>
    /// <param name="characterType">the type of character.</param>
    /// <returns>a list of world ids.</returns>
    public List<uint> GetWorldIds(CharacterType characterType);

    /// <summary>
    /// Is the character known to the character monitor service.
    /// </summary>
    /// <param name="characterId">the id of the retainer/character.</param>
    /// <returns>if the character is known.</returns>
    public bool IsCharacterKnown(ulong characterId);
}
