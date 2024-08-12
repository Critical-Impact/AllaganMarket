namespace AllaganMarket.Models;

using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json;
using Services;

public class Character : IEquatable<Character>
{
    public Character(CharacterType characterType, ulong characterId, string name, uint worldId, uint classJobId, byte level)
    {
        this.CharacterType = characterType;
        this.CharacterId = characterId;
        this.Name = name;
        this.WorldId = worldId;
        this.ClassJobId = classJobId;
        this.Level = level;
    }

    public Character()
    {
    }

    public ulong CharacterId { get; set; }

    public CharacterType CharacterType { get; set; }

    public ulong? OwnerId { get; set; }

    public string Name { get; set; }

    public uint WorldId { get; set; }

    public uint ClassJobId { get; set; }

    public byte Level { get; set; }


    public RetainerManager.RetainerTown? RetainerTown { get; set; }

    public bool Equals(Character? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.CharacterId == other.CharacterId && this.CharacterType == other.CharacterType &&
               this.OwnerId == other.OwnerId && this.Name == other.Name && this.WorldId == other.WorldId &&
               other.RetainerTown == this.RetainerTown &&
               other.ClassJobId == this.ClassJobId && other.Level == this.Level;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return this.Equals((Character)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            this.CharacterId,
            (int)this.CharacterType,
            this.OwnerId,
            this.WorldId,
            this.ClassJobId,
            this.Level,
            this.RetainerTown,
            this.Name);
    }
}
