using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin;

// tracking the state of a player character
public class PlayerCharacter
{
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
    public string FullName => $"{Name}@{World}";
    public Character? AssignedCharacter { get; set; } = null;
    public Character? ActiveCharacter = null!;
    public int ActiveDesign { get; set; } = 0;
    internal byte lastSeenIdlePose = 255;
    internal byte lastSeenSitPose = 255;
    internal byte lastSeenGroundSitPose = 255;
    internal byte lastSeenDozePose = 255;

    private static readonly Regex ValidNameRegex = new Regex("/^[A-Z]{1}[a-z']{1,14} [A-Z]{1}[a-z']{1,14}$/");

    public PlayerCharacter(string name, string world)
    {
    }

    public static PlayerCharacter? NewCharacter(string fullname)
    {
        string[] parts = fullname.Split('@');
        if (parts.Length != 2)
        {
            return null;
        }

        return NewCharacter(parts[0], parts[1]);
    }

    public static PlayerCharacter? NewCharacter(string name, string world)
    {
        if (!validName(name) || !validWorld(world))
        {
            return null;
        }

        return new PlayerCharacter(name, world);
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