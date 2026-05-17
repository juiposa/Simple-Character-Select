using System;

namespace SimpleCharacterSelectPlugin.Integration;

public class MoodlesIntegration
{
    public static void ApplyMoodlesProfile((Guid, string) preset)
    {
        /// moodle apply self preset "ABC"
        try
        {
            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null) return;
            
            MoodlesIpc.ApplyPresetByPlayerV2?.InvokeFunc(preset.Item1, local);
            Plugin.Log.Debug($"Moodles Applied Moodle Preset '{preset.Item2}'.'");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Moodles Failed: {ex.Message}");
        }
    }
}