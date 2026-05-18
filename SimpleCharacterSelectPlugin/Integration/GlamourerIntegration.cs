using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SimpleCharacterSelectPlugin.Integration;

public class GlamourerIntegration
{
    public static void ApplyGlamourerDesign(string designName)
    {
        try
        {
            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null) return;

            // Get design list and find matching design
            var designs = GlamourerIpc.GetDesigns?.InvokeFunc();
            if (designs == null)
            {
                Plugin.Log.Warning("Glamourer Could not get Glamourer design list");
                return;
            }

            var matchingDesign = designs.FirstOrDefault(d =>
                d.Value.Equals(designName, StringComparison.OrdinalIgnoreCase));

            if (matchingDesign.Key == Guid.Empty)
            {
                Plugin.Log.Warning($"Glamourer Design '{designName}' not found in Glamourer");
                return;
            }

            // Apply design via IPC
            // DesignDefault = Once | Equipment | Customization = 0x07
            const ulong designDefaultFlags = 0x07uL;
            var result = GlamourerIpc.ApplyDesign?.InvokeFunc(matchingDesign.Key, (int)local.ObjectIndex, 0u, designDefaultFlags);
            Plugin.Log.Debug($"Glamourer Applied '{designName}', result: {result}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Glamourer Apply Failed: {ex.Message}");
        }
    }

    public static void RevertGlamourerState(ushort localObjectIndex)
    {
        try
        {
            const ulong designDefaultFlags = 0x07uL;
            var result = GlamourerIpc.RevertState?.InvokeFunc(localObjectIndex, 0u, designDefaultFlags);
            Plugin.Log.Debug($"Glamourer Reverted result: {result}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Glamourer Revert Failed: {ex.Message}");
        }
    }
}