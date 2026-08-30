using System;
using System.Collections.Generic;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Integration;

public static class CustomizeIntegration
{
    public static void ApplyCustomizePlusProfile((Guid, string) profileId)
    {
        try
        {
            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null) return;
            
            var profile = CustomizeIpc.GetByUniqueId?.InvokeFunc(profileId.Item1);
            
            if (!profile.HasValue || profile.Value.Item2 == null)
            {
                Plugin.Log.Warning($"Customize+ Failed: profile not found: {profileId.Item1}, {profileId.Item2}");
                return;
            }
            
            DisableCurrentProfiles();
            
            CustomizeIpc.EnableProfile?.InvokeFunc(profileId.Item1);
            Plugin.Log.Debug($"Customize+ Applied '{profileId.Item2}', guid: {profileId.Item1}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Customize+ Apply Failed: {ex.Message}");
        }
    }

    private static void DisableCurrentProfiles()
    {
        var currProfiles = GetCustomizePlusProfiles();
        if (Plugin.Configuration.LastKnownCustomizePlus == null)
        {
            Plugin.Configuration.LastKnownCustomizePlus = currProfiles;
            Plugin.Configuration.Save();
        }

        foreach (var profile in currProfiles)
        {
            CustomizeIpc.DisableProfile?.InvokeFunc(profile.Id);
        }
    }

    private static void EnableLastKnownProfiles()
    {
        if (Plugin.Configuration.LastKnownCustomizePlus == null)
            return;
        var lastKnown = Plugin.Configuration.LastKnownCustomizePlus;
        Plugin.Log.Debug("Customize+ Reenabling Last Known Profiles");
        foreach (var profile in lastKnown)
        {
            if (profile.Enabled)
            {
                Plugin.Log.Debug($"Customize+ Reenable Last Known Profile {profile.Name}");
                CustomizeIpc.EnableProfile?.InvokeFunc(profile.Id);
                CustomizeIpc.SetPriority?.InvokeFunc(profile.Id, profile.Priority);
            }
        }

        Plugin.Configuration.LastKnownCustomizePlus = null;
        Plugin.Configuration.Save();
    }

    public static List<CustomizePlusProfile> GetCustomizePlusProfiles()
    {
        var profiles = CustomizeIpc.GetProfileList!.InvokeFunc();
        var cplusProfiles = new List<CustomizePlusProfile>();
        foreach (var valueTuple in profiles)
        {
            cplusProfiles.Add(new CustomizePlusProfile()
            {
                Id = valueTuple.Item1,
                Name = valueTuple.Item2,
                Priority = valueTuple.Item5,
                Enabled = valueTuple.Item6
            });
        }

        return cplusProfiles;
    }

    public static void RevertCustomizePlusProfile(Guid? profile)
    {
        if (profile == null)
            return;
        try
        {
            CustomizeIpc.DisableProfile?.InvokeFunc(profile.Value);
            EnableLastKnownProfiles();
            Plugin.Log.Debug($"Customize+ Reset To Last Known");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Customize+ Revert Failed: {ex.Message}");
        }
    }
    
}