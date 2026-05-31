using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using SimpleCharacterSelectPlugin.Managers;

namespace SimpleCharacterSelectPlugin;

public class Commands
{  
    private const string CommandName = "/scs";
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
        plugin.QuickSwitchWindow.Toggle();
    }
    
    private void OnSelectCommand(string command, string args)
    {
        // Handle random selection //TODO readd if someone asks for it
        // if (args.Trim().StartsWith("random", StringComparison.OrdinalIgnoreCase))
        // {
        //     var randomArgs = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //     if (randomArgs.Length == 1)
        //     {
        //         // /select random - random character and design
        //         //SelectRandomCharacterAndDesign();
        //     }
        //     else if (randomArgs.Length >= 2)
        //     {
        //         // Could be /select random GROUPNAME or /select random CHARACTER
        //         var targetName = string.Join(" ", randomArgs.Skip(1));
        //
        //         // Check if it's a group name first
        //         //var group = Configuration.RandomGroups.FirstOrDefault(g =>
        //         //    g.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        //
        //         // if (group != null)
        //         // {
        //         //     // /select random GROUPNAME - random from group
        //         //     //SelectRandomFromGroup(group);
        //         // }
        //         // else
        //         // {
        //         //     // /select random CHARACTER - random design only from specific character
        //         //     //SelectRandomDesignOnly(targetName);
        //         // }
        //     }
        //     return;
        // }

        if (args.Trim().StartsWith("revert", StringComparison.OrdinalIgnoreCase))
        {
            DesignManager.RevertAllChanges();
        }
        
        // Rest of the existing method remains the same...
        var matches = Regex.Matches(args, "\"([^\"]+)\"|\\S+")
            .Cast<Match>()
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Value)
            .ToArray();

        if (matches.Length < 1)
        {
            Plugin.ChatGui.PrintError($"[Simple Character Select] Invalid usage. Use {CommandName} <Character Name> [Design], {CommandName} revert");
            return;
        }
        
        string characterName = matches[0];
        string? designName = matches.Length > 1 ? string.Join(" ", matches.Skip(1)) : null;
        
        var character = config.Characters.FirstOrDefault(c =>
            c.Data.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        
        if (character == null)
        {
            Plugin.ChatGui.PrintError($"[Simple Character Select] Character '{characterName}' not found.");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(designName))
        {
            DesignManager.ApplyProfile(plugin.ActivePlayer.Pc, character, null);
        }
        else
        {
            var design = character.Data.Designs.FirstOrDefault(d => d.Name.Equals(designName, StringComparison.OrdinalIgnoreCase));
        
            if (design != null)
            {
                Plugin.ChatGui.Print($"[Simple Character Select] Applied design '{designName}' to {character.Data.Name}.");
                DesignManager.ApplyProfile(plugin.ActivePlayer.Pc, character, design.Id);
            }
            else
            {
                Plugin.ChatGui.PrintError($"[Simple Character Select] Design '{designName}' not found for {character.Data.Name}.");
            }
        }
    }

    public void RemoveHandlers()
    {
        commandManager.RemoveHandler(CommandName);
    }
}