using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Integration;

public static class HonorificIntegration
{
    public static void ApplyTitle(Honorific? title)
    {
        try
        {
            var clearCommand = "/honorific force clear"; //at least clear it
            GameCommandManager.ExecuteCommand(clearCommand);
            
            if (title == null || title.Title == "")
                return;
            
            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null) return;
            
            //var jsonString = title.ToJson(); TODO IPC method is awol
            //Plugin.Log.Debug($"Honorific Apply Title {jsonString}");
            //HonorificIpc.SetCharacterTitle?.InvokeFunc(local.ObjectIndex, jsonString);

            var tType = title.IsPrefix ? "prefix" : "suffix";
            var setCommand =
                $"/honorific force set {title.Title} | {tType} | {GetHexCode(title.Color)} | {GetHexCode(title.Glow)}";

            GameCommandManager.ExecuteCommand(setCommand);
            Plugin.Log.Debug($"Honorific Applied Title '{title.Title}'");
            
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Honorific Failed: {ex.Message}");
        }
    }

    private static string GetHexCode(Vector3 color)
    {
        return $"#{(int)(color.X * 255):X2}{(int)(color.Y * 255):X2}{(int)(color.Z * 255):X2}";
    }
}