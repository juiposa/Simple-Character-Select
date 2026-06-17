using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Microsoft.VisualBasic.Logging;
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

        return new PlayerCharacter(parts[0], parts[1]);
    }

    public static PlayerCharacter MustNewPlayerCharacter(string fullname)
    {
        string[] parts = fullname.Split('@');
        return new PlayerCharacter(parts[0], parts[1]);
    }

    public static void SaveCharacter(int index, Character character, CharacterData? newData)
    {
        Plugin.Log.Debug($"Saving Character {character.Data.Name}");
        character.Save(newData);

        if (index >= 0)
        {
            Plugin.Configuration.Characters[index] = character;
        }
        else
        {
            Plugin.Configuration.Characters.Add(character);
        }

        Plugin.Configuration.Save();
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