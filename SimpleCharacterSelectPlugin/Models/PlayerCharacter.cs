using System;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SimpleCharacterSelectPlugin.Models;

// tracking the state of a player character
public class PlayerCharacter
{
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
    public string FullName => $"{Name}@{World}";
    public Character? AssignedCharacter { get; set; } = null;
    public Character? ActiveCharacter = null!;
    public Guid ActiveDesignId { get; set; }
    public int ActiveDesign = -1;

    public CharacterDesign? GetActiveDesign()
    {
        return ActiveCharacter?.GetDesignById(ActiveDesignId);
    }
    
    public PlayerCharacter(string name, string world)
    {
        Name = name;
        World = world; 
    }
}