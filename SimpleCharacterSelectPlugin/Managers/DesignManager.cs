using System;
using SimpleCharacterSelectPlugin.Integration;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Managers;

public static class DesignManager
{
    public static void ApplyUpdate(ActivePlayerCharacter activePlayer)
    {
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
         
         Plugin.Log.Debug($"Applying profile {character.Data.Name} {design.Name}");
         pc.ActiveCharacter = character;
         pc.ActiveDesignId = design.Id;
         
         if(design.AssignedGearset != null && Plugin.Configuration.EnableDesignGearsetSwitching) 
             GearsetManager.TryApplyGearset(design.AssignedGearset);
         
         PenumbraIntegration.SwitchCollection(design.PenumbraCollection);
         CustomizeIntegration.ApplyCustomizePlusProfile(design.CustomizeProfileTuple);
         MoodlesIntegration.ApplyMoodlesProfile(design.MoodlePresetTuple);
         HonorificIntegration.ApplyTitle(design.Honorific);
         if (!design.DeferToGlamourer)
         { 
             GlamourerIntegration.ApplyGlamourerDesign(design.GlamourerDesign);
         }
         else
         {
             Plugin.Log.Debug("Deferring design to Glamourer");
         }
     }
     
     public static void RevertAllChanges()
     {
        var local = Plugin.ObjectTable.LocalPlayer;
        if (local == null) return;

        PenumbraIntegration.ResetCollectionToDefault(local.ObjectIndex);
        GlamourerIntegration.RevertGlamourerState(local.ObjectIndex);
        CustomizeIntegration.RevertCustomizePlusProfile(local.ObjectIndex);
        MoodlesIntegration.RevertMoodles();
        HonorificIntegration.RevertHonorific();
     }
    
}