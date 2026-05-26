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
             ApplyProfile(pc, pc.ActiveCharacter, pc.ActiveDesign);
         }
     }
     public static void ApplyProfile(PlayerCharacter pc, Character character, int designIndex)
     {
         Plugin.Log.Debug($"Applying profile {character.Data.Name} {designIndex}");
         pc.ActiveCharacter = character;
         if (designIndex >= 0 && designIndex < character.Data.Designs.Count && designIndex != pc.ActiveDesign)
         {
             pc.ActiveDesign = designIndex;
         }
         else
         {
             pc.ActiveDesign = character.Data.DefaultDesignIndex;
         }
         
         var designToApply = character.GetDesign(designIndex);
         
         if(designToApply.AssignedGearset != null && Plugin.Configuration.EnableDesignGearsetSwitching) 
             GearsetManager.TryApplyGearset(designToApply.AssignedGearset);
         
         PenumbraIntegration.SwitchCollection(designToApply.PenumbraCollection);
         CustomizeIntegration.ApplyCustomizePlusProfile(designToApply.CustomizeProfileTuple);
         MoodlesIntegration.ApplyMoodlesProfile(designToApply.MoodlePresetTuple);
         HonorificIntegration.ApplyTitle(designToApply.Honorific);
         if (!designToApply.DeferToGlamourer)
         { 
             GlamourerIntegration.ApplyGlamourerDesign(designToApply.GlamourerDesign);
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