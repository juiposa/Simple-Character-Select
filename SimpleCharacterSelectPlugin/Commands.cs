using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;
using SimpleCharacterSelectPlugin.Windows;

namespace SimpleCharacterSelectPlugin;

public class Commands
{  
    private const string CommandName = "/simplecs";
    private const string CommandNameShort = "/scs";
    private Plugin plugin;
    private ICommandManager commandManager;
    private IChatGui chatGui;
    private IPluginLog log;
    private Configuration config;

    public Commands(Plugin plugin, ICommandManager commandManager, IChatGui chatGui, IPluginLog log, Configuration config)
    {   
        this.plugin = plugin;
        this.commandManager = commandManager;
        this.chatGui = chatGui;
        this.log = log;
        this.config = config;
    }

    public void AddCommands()
    {   
        commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Simple Character Select UI"
        });
        commandManager.AddHandler(CommandNameShort, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Simple Character Select UI"
        });
        commandManager.AddHandler("/scsswitch", new CommandInfo(OnQuickSwitchCommand)
        {
            HelpMessage = "Opens the Quick Character Switcher UI."
        });
        
        commandManager.AddHandler("/scs", new CommandInfo(OnSelectCommand)
        {
            HelpMessage = "Use /select <Character Name> [Design Name] to apply a profile, /select random for random selection, /select jobchange on|off to toggle reapply on job change, /select idle to check current idle pose, /select mods to open Mod Manager, or /select save [CR] to save current look as design."
        });
        commandManager.AddHandler("/scsrevert", new CommandInfo((_, _) => RevertAllChanges())
        {
            HelpMessage = "Reverts all SCS changes (Glamourer, Honorific, Moodles, Customize+, Penumbra collection)"
        });
    }
    
    private void OnCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            this.plugin.MainWindow.Toggle();
        }
        else
        {
            OnSelectCommand(command, args);
        }
    }
    
    private void OnQuickSwitchCommand(string command, string args)
    {
        //QuickSwitchWindow.IsOpen = !QuickSwitchWindow.IsOpen; // Toggle Window On/Off
    }
    
    private void OnSelectCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            chatGui.PrintError("todo error");
            return;
        }

        // Handle random selection
        if (args.Trim().StartsWith("random", StringComparison.OrdinalIgnoreCase))
        {
            var randomArgs = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (randomArgs.Length == 1)
            {
                // /select random - random character and design
                //SelectRandomCharacterAndDesign();
                //TODO random
            }
            else if (randomArgs.Length >= 2)
            {
                // Could be /select random GROUPNAME or /select random CHARACTER
                var targetName = string.Join(" ", randomArgs.Skip(1));

                // Check if it's a group name first
                //var group = Configuration.RandomGroups.FirstOrDefault(g =>
                //    g.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));

                // if (group != null)
                // {
                //     // /select random GROUPNAME - random from group
                //     //SelectRandomFromGroup(group);
                // }
                // else
                // {
                //     // /select random CHARACTER - random design only from specific character
                //     //SelectRandomDesignOnly(targetName);
                // }
            }
            return;
        }

        // Handle jobchange on/off subcommand
        if (args.Trim().StartsWith("jobchange", StringComparison.OrdinalIgnoreCase))
        {
            var jobchangeArgs = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (jobchangeArgs.Length == 2)
            {
                string setting = jobchangeArgs[1].ToLower();
                // if (setting == "on")
                // {
                //     Configuration.ReapplyDesignOnJobChange = true;
                //     Configuration.Save();
                //     ChatGui.Print("[Simple Character Select] Reapply design on job change: Enabled");
                //     return;
                // }
                // else if (setting == "off")
                // {
                //     Configuration.ReapplyDesignOnJobChange = false;
                //     Configuration.Save();
                //     ChatGui.Print("[Simple Character Select] Reapply design on job change: Disabled");
                //     return;
                // }
            }
            chatGui.PrintError("[Simple Character Select] Usage: /select jobchange on|off");
            return;
        }

        // Handle idle subcommand - /select idle [0-6]
        if (args.Trim().StartsWith("idle", StringComparison.OrdinalIgnoreCase))
        {
            // var idleArgs = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // if (idleArgs.Length == 1)
            // {
            //     // /select idle - check current pose
            //     if (ObjectTable.LocalPlayer != null)
            //     {
            //         unsafe
            //         {
            //             var charPtr = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)ObjectTable.LocalPlayer.Address;
            //             var currentIdle = charPtr->EmoteController.CPoseState;
            //
            //             ChatGui.Print($"[SCS] Current idle pose: {currentIdle} (range: 0-6)");
            //         }
            //     }
            //     else
            //     {
            //         ChatGui.PrintError("[SCS] You must be logged in to check idle pose.");
            //     }
            // }
            // else if (idleArgs.Length >= 2 && byte.TryParse(idleArgs[1], out var poseIndex))
            // {
            //     // /select idle <0-6> - set pose
            //     PoseManager.ApplyPose(EmoteController.PoseType.Idle, poseIndex);
            //     ExecuteMacro("/penumbra redraw self");
            // }
            // else
            // {
            //     ChatGui.PrintError("[SCS] Usage: /select idle [0-6]");
            // }
            return;
        }
        
        // Handle save subcommand
        if (args.Trim().StartsWith("save", StringComparison.OrdinalIgnoreCase))
        {
            //HandleSaveCommand(args);
            return;
        }

        // Rest of the existing method remains the same...
        var matches = Regex.Matches(args, "\"([^\"]+)\"|\\S+")
            .Cast<Match>()
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Value)
            .ToArray();

        // if (matches.Length < 1)
        // {
        //     ChatGui.PrintError("[Simple Character Select] Invalid usage. Use /select <Character Name> [Design], /select random [Name], /select idle|sit|groundsit|doze [0-6], /select mods, /select save [CR], or /select whatsnew");
        //     return;
        // }
        //
        // string characterName = matches[0];
        // string? designName = matches.Length > 1 ? string.Join(" ", matches.Skip(1)) : null;
        //
        // var character = Characters.FirstOrDefault(c =>
        //     c.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        //
        // if (character == null)
        // {
        //     ChatGui.PrintError($"[Simple Character Select] Character '{characterName}' not found.");
        //     return;
        // }
        //
        // if (string.IsNullOrWhiteSpace(designName))
        // {
        //     ApplyProfile(character, -1);
        // }
        // else
        // {
        //     var design = character.Designs.FirstOrDefault(d => d.Name.Equals(designName, StringComparison.OrdinalIgnoreCase));
        //
        //     if (design != null)
        //     {
        //         var designIndex = character.Designs.IndexOf(design);
        //         ChatGui.Print($"[Simple Character Select] Applied design '{designName}' to {character.Name}.");
        //         ApplyProfile(character, designIndex);
        //     }
        //     else
        //     {
        //         ChatGui.PrintError($"[Simple Character Select] Design '{designName}' not found for {character.Name}.");
        //     }
        // }
    }

    public void RemoveHandlers()
    {
        commandManager.RemoveHandler(CommandName);
        commandManager.RemoveHandler(CommandNameShort);
        commandManager.RemoveHandler("/spose");
        commandManager.RemoveHandler("/gallery");
        commandManager.RemoveHandler("/selectrevert");
    }
    
    public void SaveAfterCommand(Character character, string designName)
    {
        config.LastUsedCharacterKey = character.Data.Name;

        if (!string.IsNullOrEmpty(designName))
        {
            config.LastUsedDesignCharacterKey = character.Data.Name;
            config.LastUsedDesignByCharacter[character.Data.Name] = designName;
            log.Debug($"[MacroTracker] Saved last design {designName} for {character.Data.Name}");
        }
        else
        {
            config.LastUsedDesignCharacterKey = null;
            config.LastUsedDesignByCharacter.Remove(character.Data.Name);
            log.Debug($"[MacroTracker] Cleared design for {character.Data.Name}");
        }

        try
        {
            config.Save();
        }
        catch (Exception ex)
        {
            log.Error($"Failed to save configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Reverts all SCS changes - Glamourer, Honorific, Moodles, Penumbra collection, and clears active character.
    /// </summary>
    public static void RevertAllChanges()
    {
        // try
        // {
        //     var local = ObjectTable.LocalPlayer;
        //     if (local == null)
        //     {
        //         Log.Warning("[RevertAllChanges] No local player - cannot revert");
        //         return;
        //     }
        //
        //     int objectIndex = (int)local.ObjectIndex;
        //     nint playerAddress = local.Address;
        //     
        //     
        //     // TODO why are *we* using IPC instead of just prompting glamourer to do it???
        //     // 1. Revert Glamourer to game state via IPC
        //     // RevertState(objectIndex, key, flags) - key=0, flags=6 (Equipment | Customization)
        //     try
        //     {
        //         const ulong RevertFlags = 0x02 | 0x04; // Equipment | Customization
        //         var result = glamourerRevertStateIpc?.InvokeFunc(objectIndex, 0, RevertFlags);
        //         Log.Debug($"[RevertAllChanges] Glamourer RevertState result: {result}");
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Warning($"[RevertAllChanges] Glamourer revert failed: {ex.Message}");
        //     }
        //
        //     // 2. Clear Honorific forced title via command (no IPC exists for forced titles)
        //     try
        //     {
        //         CommandManager.ProcessCommand("/honorific force clear | silent");
        //         Log.Debug("[RevertAllChanges] Honorific forced title cleared via command");
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Warning($"[RevertAllChanges] Honorific clear failed: {ex.Message}");
        //     }
        //     
        //     // TODO ditto
        //     // 3. Clear Moodles via IPC
        //     try
        //     {
        //         moodlesClearStatusIpc?.InvokeAction(playerAddress);
        //         Log.Debug("[RevertAllChanges] Moodles cleared");
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Warning($"[RevertAllChanges] Moodles clear failed: {ex.Message}");
        //     }
        //     
        //     // TODO ditto
        //     // 4. Disable Customize+ active profile via IPC
        //     try
        //     {
        //         // First get the active profile ID
        //         var activeResult = customizePlusGetActiveProfileIpc?.InvokeFunc((ushort)objectIndex);
        //         if (activeResult?.Item1 == 0 && activeResult?.Item2 != null)
        //         {
        //             // Disable the profile by its GUID
        //             var disableResult = customizePlusDisableProfileIpc?.InvokeFunc(activeResult.Value.Item2.Value);
        //             Log.Debug($"[RevertAllChanges] Customize+ disable profile result: {disableResult}");
        //         }
        //         else
        //         {
        //             Log.Debug($"[RevertAllChanges] No active Customize+ profile to disable (result: {activeResult?.Item1})");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Warning($"[RevertAllChanges] Customize+ disable failed: {ex.Message}");
        //     }
        //     // 5. Reset Penumbra collection to "Your Character" collection
        //     try
        //     {
        //         var penumbraResult = PenumbraIntegration?.ResetCollectionToDefault();
        //         Log.Debug($"[RevertAllChanges] Penumbra reset result: {penumbraResult}");
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Warning($"[RevertAllChanges] Penumbra reset failed: {ex.Message}");
        //     }
        //     
        //     // 6. Redraw via Penumbra IPC (objectIndex, redrawType=0 for normal redraw)
        //     try
        //     {
        //         penumbraRedrawIpc?.InvokeAction(objectIndex, 0);
        //         Log.Debug("[RevertAllChanges] Penumbra redraw triggered");
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Warning($"[RevertAllChanges] Penumbra redraw failed: {ex.Message}");
        //     }
        //
        //     // 7. Clear SCS internal state
        //     string localName = local.Name.TextValue;
        //     string worldName = local.HomeWorld.Value.Name.ToString();
        //     string fullKey = $"{localName}@{worldName}";
        //     ActiveProfilesByPlayerName.Remove(fullKey);
        //     activeCharacter = null;
        //
        //     // 8. Refresh party list to restore original name
        //     playerNameProcessor?.RefreshPartyList();
        //
        //     // 9. Chat feedback
        //     var builder = new SeStringBuilder();
        //     builder.AddText("[").AddBlue("SCS", true).AddText("] ");
        //     builder.AddText("Reverted to default state");
        //     ChatGui.Print(builder.BuiltString);
        //
        //     Log.Info("[RevertAllChanges] Successfully reverted all SCS changes via IPC");
        // }
        // catch (Exception ex)
        // {
        //     Log.Error($"[RevertAllChanges] Failed to revert: {ex.Message}");
        //     ChatGui.PrintError("[SCS] Failed to revert some changes");
        // }
    }
}