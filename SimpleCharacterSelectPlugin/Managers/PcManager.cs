using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Managers;

public static class PcManager
{
    private static readonly string NameValidationError =
        "You already have a character with this name. Please choose a different name.";

    private static readonly string NameEmptyError =
        "Name cannot be empty.";

    public static string? ValidateName(string name, string currentName, List<Character> characters)
    {
        if (string.IsNullOrWhiteSpace(name))
            return NameEmptyError;

        if (name != currentName && characters.Any(c => c.Data.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return NameValidationError;
        }

        return null;
    }

    public static PlayerCharacter? NewPlayerCharacter(string fullname)
    {
        string[] parts = fullname.Split('@');
        if (parts.Length != 2)
        {
            return null;
        }

        return new PlayerCharacter(null, parts[0], parts[1]);
    }

    public static PlayerCharacter MustNewPlayerCharacter(IPlayerCharacter ingame, string fullname)
    {
        string[] parts = fullname.Split('@');
        return new PlayerCharacter(ingame, parts[0], parts[1]);
    }

    public static void SaveCharacter(int index, Character character, CharacterData? newData, Configuration config)
    {
        character.Save(newData);

        if (index >= 0)
        {
            config.Characters[index] = character;
        }
        else
        {
            config.Characters.Add(character);
        }

        config.Save();
    }

    public static List<PlayerCharacter> GetPlayerCharactersWithAssignments(Dictionary<string, PlayerCharacter> pcs)
    {
        var returnList = new List<PlayerCharacter>();
        foreach (var pc in pcs)
        {
            if (pc.Value.AssignedCharacter != null)
            {
                returnList.Add(pc.Value);
            }
        }

        return returnList;
    }

    public static void ApplyLastUsedOrAssignedCharacter(PlayerCharacter pc)
    {
        if (pc.AssignedCharacter != null) //assignments take precedence
        {
            DesignManager.ApplyProfile(pc, pc.AssignedCharacter, null);
            return;
        }

        if (pc.ActiveCharacter != null) //else use last known
        {
            DesignManager.ApplyActiveProfile(pc);
        }
    }
}