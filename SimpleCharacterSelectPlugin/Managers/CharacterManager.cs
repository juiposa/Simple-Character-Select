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

    public static SCSError? ValidateName(string name, List<Character> characters)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new SCSError(NameEmptyError);

        if (characters.Any(c => c.Data.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return new SCSError(NameValidationError);
        }
        return null;
    }
    
    public static void SaveCharacter(int index, Character character, Configuration config, IPluginLog log)
    {
        log.Info("Char {0}", character);
        log.Info("Data: {0}", character.Data);
        log.Info("Desings: {0}", character.Data.Designs);
        if (character.Data.Designs.Count == 0)
        {
            character.Data.Designs.Add(CreateDefaultDesign(character, config));
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

    private static CharacterDesign CreateDefaultDesign(Character character, Configuration config)
    {
        string defaultDesignName = $"{character.Data.Name} {character.Data.GlamourerDesign}";
        var defaultDesign = new CharacterDesign(
            defaultDesignName,
            "",  // macro will be filled below
            false,
            ""
        );

        // Sanitize to include Automation fallback
        defaultDesign.Macro = GameCommandManager.SanitizeDesignMacro(
            $"/glamour apply {character.Data.GlamourerDesign} | self\n/penumbra redraw self",
            defaultDesign,
            character,
            config.EnableAutomations
        );
        return defaultDesign;
    }
}