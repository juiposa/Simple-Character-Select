using System;
using SimpleCharacterSelectPlugin.IPC;

namespace SimpleCharacterSelectPlugin.Managers;

public static class CustomizeManager
{
    private static void ApplyCustomizePlusProfile(string profileName)
    {
        try
        {
            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null) return;
            
            var result = CustomizeIpc.SetTempProfile?.InvokeFunc((ushort)local.ObjectIndex, profileName);
            Plugin.Log.Debug($"[ApplyCustomizePlusProfile] Applied '{profileName}', error: {result?.Item1}, guid: {result?.Item2}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"[ApplyCustomizePlusProfile] Failed: {ex.Message}");
        }
    }
}