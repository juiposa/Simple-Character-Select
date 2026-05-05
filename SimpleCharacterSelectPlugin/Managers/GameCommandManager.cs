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
    
    // Executes a macro by sending text commands to the game.
    public static void ExecuteMacro(string macroText)
    {
        ExecuteMacro(macroText, null, null);
    }
    public static void ExecuteMacro(string macroText, Character? character, string? designName)
    {
        ExecuteMacro(macroText, character, designName, false);
    }

    public static void ExecuteMacro(string macroText, Character? character, string? designName, bool filterJobChanges = true)
    {
        if (string.IsNullOrWhiteSpace(macroText))
            return;
        
        Log.Debug($"[ExecuteMacro] ELSE BRANCH - Manual application detected");

        if (character != null && !string.IsNullOrEmpty(designName))
        {
            Log.Debug($"[ExecuteMacro] Checking for recent application...");

            // Get the target gearset
            string targetGearset = null; // TODO GetTargetGearsetFromMacro(macroText);

            if (!string.IsNullOrEmpty(targetGearset))
            {
                string trackingKey = $"{character.Data.Name}_{designName}_{targetGearset}";
                
                // Update tracking with string key
                Log.Debug($"[ExecuteMacro] Updated tracking for key: {trackingKey}");
            }
        }
        
        // Build command list
        var allCommands = new List<string>();
        foreach (var raw in macroText.Split('\n'))
        {
            var cmd = raw.Trim();
            if (cmd.Length > 0 && cmd.StartsWith("/"))
                allCommands.Add(cmd);
        }

        // Execute commands, handling /wait with our own timing
        ExecuteCommands(allCommands);
    }
    
    private static void ExecuteCommands(List<string> commands)
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
    
    public static string GenerateTargetMacro(string original)
    {
        var lines = original.Split('\n');
        var result = new List<string>();

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip lines that should never apply to targets
            if (
                line.StartsWith("/customize profile disable", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("/honorific", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("/moodle", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("/spose", StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            // Rewriting self-targeting lines to <t>
            bool shouldTarget =
                line.Contains("/penumbra", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("/glamour", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("/customize profile enable", StringComparison.OrdinalIgnoreCase);

            if (shouldTarget)
            {
                line = line
                    .Replace(" self", " <t>")
                    .Replace(" Self", " <t>")
                    .Replace("| self", "| <t>")
                    .Replace("| Self", "| <t>")
                    .Replace("<me>", "<t>");
            }

            // Specific override
            if (line.StartsWith("/penumbra redraw", StringComparison.OrdinalIgnoreCase))
                line = "/penumbra redraw target";

            result.Add(line);
        }

        return string.Join("\n", result);
    }
    
    public static string SanitizeMacro(string macro, Character character)
    {
        var lines = macro.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();

        void AddOrReplace(string command, string? fullLine = null, bool insertAtTop = false)
        {
            if (!lines.Any(l => l.StartsWith(command, StringComparison.OrdinalIgnoreCase)))
            {
                if (insertAtTop)
                    lines.Insert(0, fullLine ?? command);
                else
                    lines.Add(fullLine ?? command);
            }
        }

        // Remove old pose commands and replace with new ones (always do this)
        lines = lines
            .Where(l => !l.TrimStart().StartsWith("/savepose", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Migrate old pose commands to new ones (always do this)
        for (int i = 0; i < lines.Count; i++)
        {
            lines[i] = lines[i]
                .Replace("/spose", "/sidle")
                .Replace("/sitpose", "/ssit")
                .Replace("/groundsitpose", "/sgroundsit")
                .Replace("/dozepose", "/sdoze");
        }

        // Insert /glamour automation enable {X} after last /glamour apply
        if (PluginInterface.GetPluginConfig() is Configuration config && config.EnableAutomations)
        {
            string automation = string.IsNullOrWhiteSpace(character.Data.CharacterAutomation) ? "None" : character.Data.CharacterAutomation.Trim();

            if (!lines.Any(l => l.StartsWith("/glamour automation enable", StringComparison.OrdinalIgnoreCase)))
            {
                int lastGlamourIndex = -1;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith("/glamour apply", StringComparison.OrdinalIgnoreCase))
                        lastGlamourIndex = i;
                }

                string automationLine = $"/glamour automation enable {automation}";

                if (lastGlamourIndex != -1)
                    lines.Insert(lastGlamourIndex + 1, automationLine);
                else
                    lines.Insert(0, automationLine);
            }
        }

        // For non-Advanced Mode characters, do full sanitization
        AddOrReplace("/customize profile disable <me>");
        AddOrReplace("/honorific force clear", "/honorific force clear | silent");
        AddOrReplace("/moodle remove self preset all");

        if (!lines.Any(l => l.Contains("/penumbra redraw self")))
            lines.Add("/penumbra redraw self");

        // Handle Customize+ profile enabling
        if (!string.IsNullOrWhiteSpace(character.Data.CustomizeProfile))
        {
            string enableLine = $"/customize profile enable <me>, {character.Data.CustomizeProfile}";
            if (!lines.Any(l => l.Equals(enableLine, StringComparison.OrdinalIgnoreCase)))
            {
                int disableIndex = lines.FindIndex(l => l.StartsWith("/customize profile disable", StringComparison.OrdinalIgnoreCase));
                if (disableIndex != -1)
                    lines.Insert(disableIndex + 1, enableLine);
                else
                    lines.Insert(0, enableLine);
            }
        }

        return string.Join("\n", lines);
    }

    public static string SanitizeDesignMacro(string macro, CharacterDesign design, Character character, bool enableAutomations)
    {
        var lines = macro.Split('\n').Select(l => l.Trim()).ToList();

        // Remove automation lines if automations are disabled
        if (!enableAutomations)
        {
            lines.RemoveAll(l => l.StartsWith("/glamour automation enable", StringComparison.OrdinalIgnoreCase));
        }
        // Add automation if missing (only if enabled)
        else if (!lines.Any(l => l.StartsWith("/glamour automation enable", StringComparison.OrdinalIgnoreCase)))
        {
            string automationName = !string.IsNullOrWhiteSpace(design.Automation)
                ? design.Automation
                : (!string.IsNullOrWhiteSpace(character.Data.CharacterAutomation)
                    ? character.Data.CharacterAutomation
                    : "None");

            int index = lines.FindIndex(l => l.StartsWith("/penumbra redraw", StringComparison.OrdinalIgnoreCase));
            if (index != -1)
                lines.Insert(index, $"/glamour automation enable {automationName}");
            else
                lines.Add($"/glamour automation enable {automationName}");
        }

        // Customize+ lines to always disable first, then enable (if needed)

        // Remove ALL existing customize lines
        lines.RemoveAll(l => l.StartsWith("/customize profile", StringComparison.OrdinalIgnoreCase));

        // Always insert disable before redraw
        int redrawIndex = lines.FindIndex(l => l.StartsWith("/penumbra redraw", StringComparison.OrdinalIgnoreCase));
        int insertIndex = redrawIndex != -1 ? redrawIndex : lines.Count;
        lines.Insert(insertIndex, "/customize profile disable <me>");

        // Conditionally insert enable right after disable
        string customizeProfile = !string.IsNullOrWhiteSpace(design.CustomizePlusProfile)
            ? design.CustomizePlusProfile
            : character.Data.CustomizeProfile;

        if (!string.IsNullOrWhiteSpace(customizeProfile))
        {
            lines.Insert(insertIndex + 1, $"/customize profile enable <me>, {customizeProfile}");
        }


        return string.Join("\n", lines);
    }
    
    public static string GenerateMacro(Character character)
    {
        CharacterData data = character.Data;
        if (string.IsNullOrWhiteSpace(data.PenumbraCollection) || string.IsNullOrWhiteSpace(data.GlamourerDesign))
            return "/penumbra redraw self";

        string macro = $"/penumbra collection individual | {data.PenumbraCollection} | self\n";
        macro += $"/glamour apply {data.GlamourerDesign} | self\n";
        
        
        //TODO readd
        // if (plugin.Configuration.EnableAutomations)
        // {
        //     if (string.IsNullOrWhiteSpace(automation))
        //         macro += "/glamour automation enable None\n";
        //     else
        //         macro += $"/glamour automation enable {automation}\n";
        // }

        macro += "/customize profile disable <me>\n";
        if (!string.IsNullOrWhiteSpace(data.CustomizeProfile))
            macro += $"/customize profile enable <me>, {data.CustomizeProfile}\n";

        macro += "/honorific force clear | silent\n";
        Honorific h = data.Honorific;
        if (!string.IsNullOrWhiteSpace(data.Honorific.Title))
        {
            string colorHex = $"#{(int)(h.Color.X * 255):X2}{(int)(h.Color.Y * 255):X2}{(int)(h.Color.Z * 255):X2}";
            string glowHex = $"#{(int)(h.Glow.X * 255):X2}{(int)(h.Glow.Y * 255):X2}{(int)(h.Glow.Z * 255):X2}";
            int? gradientSet = h.GradientSet;
            string? animStyle = h.AnimationStyle;
            Vector3? color3 = h.Color3;

            string gradientPart = "";
            if (gradientSet.HasValue && !string.IsNullOrEmpty(animStyle))
            {
                if (gradientSet.Value == -1 && color3.HasValue)
                {
                    // Two-colour gradient: include Color3 in the command
                    string color3Hex = $"#{(int)(color3.Value.X * 255):X2}{(int)(color3.Value.Y * 255):X2}{(int)(color3.Value.Z * 255):X2}";
                    gradientPart = $" | {color3Hex} | +-1/{animStyle}";
                }
                else
                {
                    gradientPart = $" | +{gradientSet.Value}/{animStyle}";
                }
            }

            macro += $"/honorific force set {h.Title} | {h.Title} | {colorHex} | {glowHex}{gradientPart} | silent\n";
        }

        macro += "/moodle remove self preset all\n";
        if (!string.IsNullOrWhiteSpace(data.MoodlePreset))
            macro += $"/moodle apply self preset \"{data.MoodlePreset}\"\n";

        if (data.IdlePoseIndex != 7)
            macro += $"/sidle {data.IdlePoseIndex}\n";

        macro += "/penumbra redraw self";

        return macro;
    }
}