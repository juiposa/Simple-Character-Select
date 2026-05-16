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
    
    public static void SetActiveCharacter(Character character, Configuration config)
    {
        Plugin.Log.Debug("[SetActiveCharacter] CALLED");

        if (Plugin.ObjectTable.LocalPlayer is { } player && player.HomeWorld.IsValid)
        {
            string localName = player.Name.TextValue;
            string worldName = player.HomeWorld.Value.Name.ToString();
            string fullKey = $"{localName}@{worldName}"; // Who is logged in
            string pluginCharacterKey = $"{character.Data.Name}@{worldName}"; // SCS character identity

            // This is the key logic: player -> selected plugin character
            config.PlayerCharacters
            Configuration.LastUsedCharacterKey = character.Data.Name;

            try
            {
                Configuration.Save();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[SetActiveCharacter] Failed to save configuration: {ex.Message}");
            }
                
                
            // TODO dupe SetActive
            Plugin.Log.Debug($"[SetActiveCharacter] Saved: {fullKey} → {pluginCharacterKey}");
            Plugin.Log.Debug($"[SetActiveCharacter] Set LastInGameName = {fullKey} for profile {character.Data.Name}");
        }
    }

    public static void ApplyLastUsedOrAssignedCharacter(PlayerCharacter pc)
    {
        if (pc.AssignedCharacter != null) //assignments take precedence
        {
            DesignManager.
        }
    }

}