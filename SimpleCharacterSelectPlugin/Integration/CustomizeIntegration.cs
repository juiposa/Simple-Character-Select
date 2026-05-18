using System;

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
            
            //Clear any existing temp profiles
            var result = CustomizeIpc.DeleteTemporaryProfileOnCharacter?.InvokeFunc((ushort)local.ObjectIndex);
            Plugin.Log.Debug($"Customize+ Cleared '{profileId.Item2}', error: {result}");
            
            var result2 = CustomizeIpc.SetTempProfile?.InvokeFunc((ushort)local.ObjectIndex, profile.Value.Item2);
            Plugin.Log.Debug($"Customize+ Applied '{profileId.Item2}', error: {result2?.Item1}, guid: {result2?.Item2}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Customize+ Apply Failed: {ex.Message}");
        }
    }

    public static void RevertCustomizePlusProfile(ushort localObjectIndex)
    {
        try
        {
            var result = CustomizeIpc.DeleteTemporaryProfileOnCharacter?.InvokeFunc((ushort)localObjectIndex);
            Plugin.Log.Debug($"Customize+ Cleared");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Customize+ Revert Failed: {ex.Message}");
        }
    }
    
}