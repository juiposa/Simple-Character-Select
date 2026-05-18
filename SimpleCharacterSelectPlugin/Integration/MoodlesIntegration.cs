using System;
using SimpleCharacterSelectPlugin.Managers;

namespace SimpleCharacterSelectPlugin.Integration;

public class MoodlesIntegration
{
    public static void ApplyMoodlesProfile((Guid, string) preset)
    {
        /// moodle apply self preset "ABC"
        try
        {
            if (preset.Item1 == Guid.Empty)
                return;
            
            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null) return;
            
            //TODO IPC method not available, check back in
            //MoodlesIpc.ApplyPresetByPlayerV2?.InvokeFunc(preset.Item1, local);
            //Plugin.Log.Debug($"Moodles Applied Moodle Preset '{preset.Item2}'.'");
            
            GameCommandManager.ExecuteCommand($"/moodle apply self preset \"{preset.Item2}\"");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Moodles Failed: {ex.Message}");
        }
    }
}