using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace SimpleCharacterSelectPlugin
{
    /// <summary>
    /// IPC Provider for Simple Character Select to allow other plugins to interact with character switching
    /// </summary>
    public class SCSIPCProvider : IDisposable
    {
        private readonly Plugin plugin;
        private readonly IDalamudPluginInterface pluginInterface;
        
        // IPC Providers
        private readonly ICallGateProvider<string[]> getCharacterListProvider;
        private readonly ICallGateProvider<string> getCurrentCharacterProvider;
        private readonly ICallGateProvider<string, string[]> getCharacterDesignsProvider;
        private readonly ICallGateProvider<string, bool> switchToCharacterProvider;
        private readonly ICallGateProvider<string, string, bool> switchToCharacterDesignProvider;
        private readonly ICallGateProvider<bool> getQuickSwitchVisibilityProvider;
        private readonly ICallGateProvider<bool> toggleQuickSwitchProvider;
        private readonly ICallGateProvider<string, string, object> onCharacterChangedProvider;
        
        private readonly ICallGateProvider<int> getCharacterCountProvider;
        private readonly ICallGateProvider<List<(string, bool, string?)>> getCharactersProvider;

        public SCSIPCProvider(Plugin plugin, IDalamudPluginInterface pluginInterface)
        {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
            
            // Initialize IPC providers
            getCharacterListProvider = pluginInterface.GetIpcProvider<string[]>("CharacterSelect.GetCharacterList");
            getCurrentCharacterProvider = pluginInterface.GetIpcProvider<string>("CharacterSelect.GetCurrentCharacter");
            getCharacterDesignsProvider = pluginInterface.GetIpcProvider<string, string[]>("CharacterSelect.GetCharacterDesigns");
            switchToCharacterProvider = pluginInterface.GetIpcProvider<string, bool>("CharacterSelect.SwitchToCharacter");
            switchToCharacterDesignProvider = pluginInterface.GetIpcProvider<string, string, bool>("CharacterSelect.SwitchToCharacterDesign");
            getQuickSwitchVisibilityProvider = pluginInterface.GetIpcProvider<bool>("CharacterSelect.GetQuickSwitchVisibility");
            toggleQuickSwitchProvider = pluginInterface.GetIpcProvider<bool>("CharacterSelect.ToggleQuickSwitch");
            onCharacterChangedProvider = pluginInterface.GetIpcProvider<string, string, object>("CharacterSelect.OnCharacterChanged");
            
            getCharacterCountProvider = pluginInterface.GetIpcProvider<int>("CharacterSelect.GetCharacterCount");
            getCharactersProvider = pluginInterface.GetIpcProvider<List<(string, bool, string?)>>("CharacterSelect.GetCharacters");
            
            // Register IPC methods
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            getCharacterListProvider.RegisterFunc(GetCharacterList);
            getCurrentCharacterProvider.RegisterFunc(GetCurrentCharacter);
            getCharacterDesignsProvider.RegisterFunc(GetCharacterDesigns);
            switchToCharacterProvider.RegisterFunc(SwitchToCharacter);
            switchToCharacterDesignProvider.RegisterFunc(SwitchToCharacterDesign);
            getQuickSwitchVisibilityProvider.RegisterFunc(GetQuickSwitchVisibility);
            toggleQuickSwitchProvider.RegisterFunc(ToggleQuickSwitch);
            
            getCharacterCountProvider.RegisterFunc(GetCharacterCount);
            getCharactersProvider.RegisterFunc(GetCharacters);
            
            Plugin.Log.Info("[IPC] All IPC methods registered including GetCharacterCount and GetCharacters");
        }

        /// <summary>
        /// Get list of all character names
        /// </summary>
        private string[] GetCharacterList()
        {
            return plugin.Characters.Select(c => c.Data.Name).ToArray();
        }

        /// <summary>
        /// Get currently active character name, or empty string if none
        /// </summary>
        private string GetCurrentCharacter()
        {
            return plugin.PlayerCharacter.ActiveCharacter?.Data.Name ?? "";
        }

        /// <summary>
        /// Get list of design names for a specific character
        /// </summary>
        private string[] GetCharacterDesigns(string characterName)
        {
            var character = plugin.Characters.FirstOrDefault(c => c.Data.Name == characterName);
            if (character == null)
                return Array.Empty<string>();
            
            return character.Data.Designs.Select(d => d.Name).ToArray();
        }

        /// <summary>
        /// Switch to a character by name (no design)
        /// </summary>
        private bool SwitchToCharacter(string characterName)
        {
            var character = plugin.Characters.FirstOrDefault(c => c.Data.Name == characterName);
            if (character == null)
                return false;
            
            try
            {
                plugin.ApplyProfile(character, -1);
                NotifyCharacterChanged(characterName, "");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Switch to a character and specific design
        /// </summary>
        private bool SwitchToCharacterDesign(string characterName, string designName)
        {
            var character = plugin.Characters.FirstOrDefault(c => c.Data.Name == characterName);
            if (character == null)
                return false;
            
            var designIndex = character.Data.Designs.FindIndex(d => d.Name == designName);
            if (designIndex == -1)
                return false;
            
            try
            {
                plugin.ApplyProfile(character, designIndex);
                NotifyCharacterChanged(characterName, designName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get whether the Quick Switch window is currently visible
        /// </summary>
        private bool GetQuickSwitchVisibility()
        {
            return plugin.QuickSwitchWindow?.IsOpen ?? false;
        }

        /// <summary>
        /// Toggle the Quick Switch window visibility
        /// </summary>
        private bool ToggleQuickSwitch()
        {
            try
            {
                if (plugin.QuickSwitchWindow?.IsOpen ?? false)
                {
                    plugin.QuickSwitchWindow.IsOpen = false;
                }
                else
                {
                    plugin.QuickSwitchWindow.IsOpen = true;
                }
                return plugin.QuickSwitchWindow?.IsOpen ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get count of characters
        /// </summary>
        private int GetCharacterCount()
        {
            var count = plugin.Characters.Count;
            Plugin.Log.Info($"[IPC] GetCharacterCount called, returning: {count}");
            return count;
        }

        /// <summary>
        /// Get character information
        /// Returns list of tuples with (name, isActive, currentDesign)
        /// </summary>
        private List<(string, bool, string?)> GetCharacters()
        {
            var result = new List<(string, bool, string?)>();
            
            Plugin.Log.Info($"[IPC] GetCharacters called. Total characters: {plugin.Characters.Count}");
            Plugin.Log.Info($"[IPC] Active character: {plugin.PlayerCharacter.ActiveCharacter?.Data.Name ?? "None"}");
            
            foreach (var character in plugin.Characters)
            {
                bool isActive = plugin.PlayerCharacter.ActiveCharacter?.Data.Name == character.Data.Name;
                string? currentDesign = null;
                
                Plugin.Log.Info($"[IPC] Character: {character.Data.Name}, Active: {isActive}");
                result.Add((character.Data.Name, isActive, currentDesign));
            }
            
            Plugin.Log.Info($"[IPC] GetCharacters returning {result.Count} characters");
            return result;
        }

        /// <summary>
        /// Notify other plugins that character changed
        /// </summary>
        private void NotifyCharacterChanged(string characterName, string designName)
        {
            try
            {
                onCharacterChangedProvider.SendMessage(characterName, designName);
            }
            catch
            {
                // Ignore notification failures
            }
        }

        public void Dispose()
        {
            // IPC providers are automatically cleaned up by Dalamud
        }
    }
}