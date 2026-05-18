using System;
using SimpleCharacterSelectPlugin.Managers;

namespace SimpleCharacterSelectPlugin.Integration;

public class MoodlesIntegration
{
    private const string RemoveCommand = "/moodle remove self preset all";
    public static void ApplyMoodlesProfile((Guid, string) preset)
    {
        try
        {
            GameCommandManager.ExecuteCommand(RemoveCommand);
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

    public static void RevertMoodles()
    {
        try
        {
            GameCommandManager.ExecuteCommand(RemoveCommand);
            Plugin.Log.Debug($"Moodles Cleared");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Moodles Revert Failed: {ex.Message}");
        }
    }
}