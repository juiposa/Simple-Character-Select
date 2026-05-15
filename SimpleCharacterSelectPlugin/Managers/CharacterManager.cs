using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Managers;

public static class CharacterManager
{
    private static readonly string NameValidationError =
        "You already have a character with this name. Please choose a different name.";
    
    private static readonly string NameEmptyError =
        "Name cannot be empty.";

    public static string? ValidateName(string name, List<Character> characters)
    {
        if (string.IsNullOrWhiteSpace(name))
            return NameEmptyError;

        if (characters.Any(c => c.Data.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return NameValidationError;
        }
        return null;
    }
    
    public static void SaveCharacter(int index, Character character, Configuration config, IPluginLog log)
    {
        if (character.Data.Designs.Count == 0)
        {
            character.Data.Designs.Add(DesignManager.CreateDefaultDesign(character, config));
        }
        
        character.Save();
        
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

    public static List<Character> LoadCharacters(Configuration config, IPluginLog log)
    {
        return new List<Character>();
    }

    public static List<PlayerCharacter> GetPlayerCharactersWithAssignments(List<PlayerCharacter> pcs)
    {
        return pcs;
    }
}