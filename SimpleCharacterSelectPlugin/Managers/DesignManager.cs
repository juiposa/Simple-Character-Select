using System;
using SimpleCharacterSelectPlugin.Integration;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Managers;

public static class DesignManager
{
    public static void ApplyUpdate(ActivePlayerCharacter activePlayer)
    {
        if (activePlayer.RequiresRevertBeforeUpdate())
        {
            

            RevertAllChanges(activePlayer.Pc);
            activePlayer.ApplicationBuffer = 10; //defer update until 10 frames from now
            return;
        }

        if (activePlayer.ApplicationBuffer > 0)
        {
            activePlayer.ApplicationBuffer--;
            return;
        }
        
        Plugin.Log.Debug("Updating player character");
        activePlayer.ApplyUpdate();
        ApplyActiveProfile(activePlayer.Pc);
    }
    
     public static void ApplyActiveProfile(PlayerCharacter pc)
     {
         if (pc.ActiveCharacter != null)
         {
             ApplyProfile(pc, pc.ActiveCharacter, pc.ActiveDesignId);
         }
     }
     public static void ApplyProfile(PlayerCharacter pc, Character character, Guid? designId)
     {
         CharacterDesign design = character.GetDesignByIdOrDefault(designId);
         CharacterDesign defaultDesign = character.GetDefaultDesign();
         Plugin.Log.Debug($"Applying profile {character.Data.Name} {design.Name}");
         pc.ActiveCharacter = character;
         pc.ActiveDesignId = design.Id;
         
         if(design.AssignedGearset != null && Plugin.Configuration.EnableDesignGearsetSwitching) 
             GearsetManager.TryApplyGearset(design.AssignedGearset);
         
         PenumbraIntegration.SwitchCollection(design.PenumbraCollection);
         CustomizeIntegration.ApplyCustomizePlusProfile(design.CustomizeProfileTuple);
         MoodlesIntegration.ApplyMoodlesProfile(defaultDesign.MoodlePresetTuple);
         HonorificIntegration.ApplyTitle(defaultDesign.Honorific);
         if (!design.DeferToGlamourer)
         { 
             GlamourerIntegration.ApplyGlamourerDesign(design.GlamourerDesign);
         }
         else
         {
             Plugin.Log.Debug("Deferring design to Glamourer");
         }
     }
     
     public static void RevertAllChanges(PlayerCharacter pc)
     {
        var local = Plugin.ObjectTable.LocalPlayer;
        if (local == null) return;

        var profile = pc.GetActiveDesign();
        
        Plugin.Log.Debug("Reverting all changes");

        PenumbraIntegration.ResetCollectionToDefault(local.ObjectIndex);
        GlamourerIntegration.RevertGlamourerState(local.ObjectIndex);
        CustomizeIntegration.RevertCustomizePlusProfile(profile?.CustomizeProfileTuple.Item1);
        MoodlesIntegration.RevertMoodles();
        HonorificIntegration.RevertHonorific();
        pc.ActiveCharacter = null;
     }
    
}