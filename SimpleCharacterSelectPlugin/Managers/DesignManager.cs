using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Serilog;
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
         
         PenumbraIntegration.SwitchCollection(designToApply.PenumbraCollection);
         GlamourerIntegration.ApplyGlamourerDesign(designToApply.GlamourerDesign);
         CustomizeIntegration.ApplyCustomizePlusProfile(designToApply.CustomizeProfileTuple);
         MoodlesIntegration.ApplyMoodlesProfile(designToApply.MoodlePresetTuple);
         HonorificIntegration.ApplyTitle(designToApply.Honorific);
         // apply C+
         // apply moodles
         // apply honorific

         //switch gearset if needed
     }
     
    private  static bool TryApplyGearsetAssignment(uint gearsetId)
    {
        // if (!Configuration.EnableJobAssignments || Configuration.JobAssignments.Count == 0)
        //     return false;
        //
        // string? assignmentValue = null;
        //
        // // Check job-specific assignment first
        // var jobKey = $"Job_{jobId}";
        // if (Configuration.JobAssignments.TryGetValue(jobKey, out var jobAssignment))
        // {
        //     assignmentValue = jobAssignment;
        //     Log.Debug($"[JobAssignment] Found job-specific assignment for {jobKey}: {assignmentValue}");
        // }
        // else
        // {
        //     // Check role assignment
        //     var role = GetRoleForJob(jobId);
        //     var roleKey = $"Role_{role}";
        //     if (Configuration.JobAssignments.TryGetValue(roleKey, out var roleAssignment))
        //     {
        //         assignmentValue = roleAssignment;
        //         Log.Debug($"[JobAssignment] Found role assignment for {roleKey}: {assignmentValue}");
        //     }
        // }
        //
        // if (string.IsNullOrEmpty(assignmentValue))
        //     return false;
        //
        // // Parse the assignment
        // var (characterName, designName) = ParseJobAssignment(assignmentValue);
        // if (string.IsNullOrEmpty(characterName))
        //     return false;
        //
        // // Find the character
        // var character = Characters.FirstOrDefault(c => c.Data.Name == characterName);
        // if (character == null)
        // {
        //     Log.Warning($"[JobAssignment] Character '{characterName}' not found for job assignment");
        //     return false;
        // }
        //
        // // Apply the character (and optionally design)
        // if (!string.IsNullOrEmpty(designName))
        // {
        //     var designIndex = character.Data.Designs.FindIndex(d => d.Name == designName);
        //     if (designIndex >= 0)
        //     {
        //         Log.Info(
        //             $"[JobAssignment] Applying design '{designName}' on character '{characterName}' for job {jobId}");
        //         ApplyProfile(character, designIndex);
        //     }
        //     else
        //     {
        //         Log.Warning(
        //             $"[JobAssignment] Design '{designName}' not found on character '{characterName}', applying character only");
        //         ApplyProfile(character, -1);
        //     }
        // }
        // else
        // {
        //     Log.Info($"[JobAssignment] Applying character '{characterName}' for job {jobId}");
        //     ApplyProfile(character, -1);
        // }

        return true;
    }
}