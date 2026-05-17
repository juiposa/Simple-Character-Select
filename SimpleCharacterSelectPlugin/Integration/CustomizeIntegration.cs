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
            
            var result = CustomizeIpc.SetTempProfile?.InvokeFunc((ushort)local.ObjectIndex, profile.Value.Item2);
            Plugin.Log.Debug($"Customize+ Applied '{profileId.Item2}', error: {result?.Item1}, guid: {result?.Item2}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Customize+ Failed: {ex.Message}");
        }
    }
}