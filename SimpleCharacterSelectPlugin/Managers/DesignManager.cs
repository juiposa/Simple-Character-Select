using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Managers;

public class DesignManager
{   
     private readonly Dictionary<string, string> ActiveProfilesByPlayerName = new();
     public string NewCharacterTag { get; set; } = "";
     public List<string> KnownTags => Configuration.KnownTags;
     public string NewCharacterAutomation { get; set; } = "";
     public int? NewCharacterGearset { get; set; } = null;
     public byte LastIdlePoseAppliedByPlugin { get; set; } = 255;
     
     public static CharacterDesign CreateDefaultDesign(Character character, Configuration config)
     {
         string defaultDesignName = $"{character.Data.Name} {character.Data.GlamourerDesign}";
         var defaultDesign = new CharacterDesign(
             defaultDesignName,
             ""  // macro will be filled below
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
     
     public bool ApplyDesign(PlayerCharacter pc, Character character, int designIndex)
     {
         if (!Plugin.ClientState.IsLoggedIn ||
             Plugin.ClientState.TerritoryType == 0 ||
             Plugin.ObjectTable.LocalPlayer == null ||
             string.IsNullOrEmpty(Plugin.ObjectTable.LocalPlayer.Name.TextValue) ||
             !Plugin.ObjectTable.LocalPlayer.HomeWorld.IsValid)
         {
             Plugin.Log.Debug("[ApplyProfile] Skipped: Player not fully loaded.");
             return false;
         }
         
         pc.ActiveCharacter = character;
         pc.ActiveDesign = designIndex;
         
         CharacterDesign characterDesign = character.Data.Designs[designIndex];
     }
     public void ApplyProfile(PlayerCharacter pc, Character character, int designIndex)
     {
         // Detect if this is a design switch on the SAME character (not a full character switch)
         // If so, we should only re-run known integration commands, not custom toggles like /minion
         bool isSameCharacterDesignSwitch = pc.ActiveCharacter != null &&
                                            pc.ActiveCharacter.Data.Name == character.Data.Name &&
                                             designIndex >= 0;
         if (isSameCharacterDesignSwitch)
         {
             Plugin.Log.Debug($"[ApplyProfile] Same-character design switch - filtering to integration commands only");
         }
    
         pc.ActiveCharacter = character;
         

    
         if (Plugin.ObjectTable.LocalPlayer is { } player && player.HomeWorld.IsValid)
         {
             string localName = player.Name.TextValue;
             string worldName = player.HomeWorld.Value.Name.ToString();
             string fullKey = $"{localName}@{worldName}";
             string newProfileKey = $"{character.Data.Name}@{worldName}";
    
             // Remove all old entries for this player
             var toRemove = ActiveProfilesByPlayerName
                 .Where(kvp => kvp.Key.StartsWith($"{localName}@{worldName}", StringComparison.OrdinalIgnoreCase))
                 .Select(kvp => kvp.Key)
                 .ToList();
    
             foreach (var oldKey in toRemove)
                 ActiveProfilesByPlayerName.Remove(oldKey);
    
             // Register key
             ActiveProfilesByPlayerName[fullKey] = character.Data.Name;
             string pluginCharacterKey = $"{character.Data.Name}@{worldName}"; // plugin character identity
             character.Data.LastInGameName = $"{localName}@{worldName}";        // who is currently logged in
    
             Plugin.Configuration.LastUsedCharacterByPlayer[fullKey] = pluginCharacterKey;
             Configuration.LastUsedCharacterKey = character.Data.Name;
             Configuration.Save();
             
             
             //TODO dupe SetActive
             Plugin.Log.Debug($"[ApplyProfile] Saved: {fullKey} → {pluginCharacterKey}");
             Plugin.Log.Debug($"[SetActiveCharacter] Updated LastUsedCharacterKey = {fullKey}");
             Plugin.Log.Debug($"[ApplyProfile] Set LastInGameName = {character.Data.LastInGameName} for profile {character.Data.Name}");
         }
         Plugin.SaveConfiguration();
         if (character == null) return;
         
         // Switch Penumbra UI collection to match the character's collection
         if (!string.IsNullOrEmpty(character.Data.PenumbraCollection))
         {
             var success = PenumbraManager.SwitchCollection(character.Data.PenumbraCollection);
             if (success)
             {
                 Plugin.Log.Information($"Successfully switched Penumbra UI collection to: {character.Data.PenumbraCollection}");
             }
             else
             {
                 Plugin.Log.Warning($"Failed to switch Penumbra UI collection to: {character.Data.PenumbraCollection}");
             }
         }
    
         // Apply the character's macro
         // If this is a same-character design switch, only run known integration commands
         string characterMacro = isSameCharacterDesignSwitch
             ? FilterToKnownIntegrationCommands(character.Data.Macros)
             : character.Data.Macros;
         GameCommandManager.ExecuteMacro(characterMacro, character, null);
    
         // Switch gearset AFTER Glamourer design is applied (via macros above)
         // This ensures Lightless sees the correct appearance when the gearset switch triggers a model refresh
         if (Configuration.EnableGearsetAssignments)
         {
             int? effectiveGearset = null;
             if (designIndex >= 0 && designIndex < character.Data.Designs.Count)
             {
                 var designForGearset = character.Data.Designs[designIndex];
                 effectiveGearset = designForGearset.AssignedGearset ?? character.Data.AssignedGearset;
             }
             else
             {
                 effectiveGearset = character.Data.AssignedGearset;
             }
    
             if (effectiveGearset.HasValue)
             {
                 // // Small delay to let Glamourer finish applying before gearset switch
                 // var gearsetToSwitch = effectiveGearset.Value;
                 // Framework.RunOnTick(() => SwitchToGearset(gearsetToSwitch), delayTicks: 5);
             }
         }
    
    
         // Apply poses immediately
         if (character.Data.IdlePoseIndex < 7)
         {
             PoseManager.ApplyPose(EmoteController.PoseType.Idle, character.Data.IdlePoseIndex);
             Configuration.LastIdlePoseAppliedByPlugin = character.Data.IdlePoseIndex;
             Configuration.Save();
         }
         else
         {
             Plugin.Log.Debug("[ApplyProfile] Skipping idle pose apply because it is set to None.");
         }
    
         Plugin.QuickSwitchWindow.UpdateSelectionFromCharacter(character);
    
         SaveConfiguration();
    }
}