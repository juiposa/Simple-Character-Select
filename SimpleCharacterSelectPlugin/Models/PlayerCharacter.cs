using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin;

// tracking the state of a player character
public class PlayerCharacter
{
    public IPlayerCharacter? InGameCharacter { get; set; }
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
    public string FullName => $"{Name}@{World}";
    public Character? AssignedCharacter { get; set; } = null;
    public Character? ActiveCharacter = null!;
    public int ActiveDesign { get; set; } = 0;
    // internal byte lastSeenIdlePose = 255;
    // internal byte lastSeenSitPose = 255;
    // internal byte lastSeenGroundSitPose = 255;
    // internal byte lastSeenDozePose = 255;

    private static readonly Regex ValidNameRegex = new Regex("/^[A-Z]{1}[a-z']{1,14} [A-Z]{1}[a-z']{1,14}$/");

    internal PlayerCharacter()
    {
    }
    
    public PlayerCharacter(IPlayerCharacter? ingame, string name, string world)
    {
        InGameCharacter = ingame;
        Name = name;
        World = world; 
    }

    private static bool validName(string name)
    {
        var match = ValidNameRegex.Match(name);
        return match.Success && match.Length - 1 <= 20;
    }

    private static bool validWorld(string world)
    {
        return XivConstants.Worlds.Contains(world);
    }
}