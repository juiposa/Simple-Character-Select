using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Managers;

public static class GameCommandManager
{       
    internal static IPluginLog Log { get; private set; } = null!;
    internal static ICommandManager CommandManager { get; private set; } = null!;
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    public static void Init(IPluginLog log, ICommandManager commandManager, IDalamudPluginInterface pluginInterface)
    {
        Log = log;
        CommandManager = commandManager;
        PluginInterface = pluginInterface;
    }

    public static void ExecuteCommand(string command)
    {
        ExecuteCommands(new List<string>{command});
    }
    
    // Executes a macro by sending text commands to the game.
    public static void ExecuteCommands(List<string> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];

            // Try as plugin command first, fall back to game macro
            bool handled = CommandManager.ProcessCommand(cmd);
            if (handled)
            {
                Log.Debug($"Plugin command executed: '{cmd}'");
            }
            else
            {
                // Native game command — send through game macro system
                ExecuteGameCommands(new List<string> { cmd });
                Log.Debug($"Game command executed: '{cmd}'");
            }
        }
    }

    // Execute game commands using the macro system
    private static unsafe void ExecuteGameCommands(List<string> commands)
    {
        if (commands.Count == 0) return;
        if (commands.Count > 15)
        {
            Plugin.Log.Warning($"Too many game commands ({commands.Count}), max is 15. Truncating.");
            commands = commands.Take(15).ToList();
        }

        var raptureShellModule = RaptureShellModule.Instance();
        if (raptureShellModule == null)
        {
            Plugin.Log.Warning("Could not get RaptureShellModule instance");
            return;
        }

        // Clean up the previous macro if one exists
        
        RaptureMacroModule.Macro* macroToRun = null;
        macroToRun = (RaptureMacroModule.Macro*)System.Runtime.InteropServices.Marshal.AllocHGlobal(
            sizeof(RaptureMacroModule.Macro));
        macroToRun->Name.Ctor();
        foreach (ref var line in macroToRun->Lines)
        {
            line.Ctor();
        }

        try
        {
            // Set up the macro lines
            for (int i = 0; i < commands.Count && i < 15; i++)
            {
                var cmd = commands[i];
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    macroToRun->Lines[i].Clear();
                    continue;
                }

                var encoded = System.Text.Encoding.UTF8.GetBytes(cmd + "\0");
                if (encoded.Length == 0)
                {
                    macroToRun->Lines[i].Clear();
                    continue;
                }

                fixed (byte* encodedPtr = encoded)
                {
                    macroToRun->Lines[i].SetString(encodedPtr);
                }
            }

            // Execute — macro memory persists until next call or plugin dispose
            raptureShellModule->ExecuteMacro(macroToRun);
            Plugin.Log.Debug($"Executed {commands.Count} game commands via macro system");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to execute game commands: {ex.Message}");
        }
    }
    
}