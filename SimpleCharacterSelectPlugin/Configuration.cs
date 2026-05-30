using SimpleCharacterSelectPlugin.Windows;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public List<Character> Characters { get; set; } = new List<Character>();
        public Dictionary<string, PlayerCharacter> PlayerCharacters { get; set; } = new Dictionary<string, PlayerCharacter>();

        public Dictionary<int, GearsetAssignment> GearsetAssignments { get; set; } = new Dictionary<int, GearsetAssignment>();

        // Profile Settings
        public float ProfileImageScale { get; set; } = 1.0f; // Image scaling
        public int ProfileColumns { get; set; } = 3;        // Number of profiles per row
        public float ProfileSpacing { get; set; } = 10.0f;  // Default spacing between profiles

        private IDalamudPluginInterface pluginInterface;
        public int CurrentSortIndex { get; set; } = 0; // Default to Manual (SortType.Manual = 0)
        public bool IsQuickSwitchWindowOpen { get; set; } = false;
        //public bool EnableAutomations { get; set; } = false; TODO I need to figure out wtf
        public List<string> KnownTags { get; set; } = new();
        public bool EnableGearsetDesignSwitching { get; set; } = false;
        public bool EnableDesignGearsetSwitching { get; set; } = false;
        public bool EnableLastUsedCharacterAutoload { get; set; } = false;
        public List<uint> FavoriteIconIds { get; set; } = new();
        [JsonProperty]
        private float _uiScaleMultiplier = 1.0f;
        
        /// <summary>
        /// UI scale multiplier (0.5-2.0). Legacy setting, no longer in UI.
        /// </summary>
        public float UIScaleMultiplier 
        { 
            get => _uiScaleMultiplier;
            set => _uiScaleMultiplier = Math.Clamp(value, 0.5f, 2.0f);
        }
        [DefaultValue(true)]
        public bool ReapplyDesignOnJobChange { get; set; } = false;
        
        // Pose Settings
        
        // Design Sorting
        public int CurrentDesignSortIndex { get; set; } = 1;
        [JsonProperty]
        public bool EnableSafeMode { get; set; } = false;
        public bool QuickSwitchCompact { get; set; } = false;
        public bool QuickSwitchIgnoreEscape { get; set; } = true;
        public List<string> PinnedFileBrowserPaths { get; set; } = new();
        public string? LastBrowserDirectory { get; set; }
        
        [DefaultValue(false)]
        public bool RandomSelectionFavoritesOnly { get; set; } = false;

        // Random Groups - custom groups of characters for /scs random <groupname>
        public List<RandomGroup> RandomGroups { get; set; } = new();

        public string? MainCharacterName { get; set; } = null; 
        public bool EnableMainCharacterOnly { get; set; } = false;
        public bool ShowMainCharacterCrown { get; set; } = true;
        public float DesignPanelWidth { get; set; } = 300f;
        
        public bool EnableDialogueIntegration { get; set; } = false;

        public bool ReplaceNameInDialogue { get; set; } = true;

        public bool ReplacePronounsInDialogue { get; set; } = true;

        public bool EnableSmartGrammarInDialogue { get; set; } = true;

        public bool ShowDialogueReplacementPreview { get; set; } = false;
        
        // Enhanced dialogue
        public bool EnableLuaHookDialogue { get; set; } = true;

        public bool ReplaceGenderedTerms { get; set; } = true;

        public bool EnableAdvancedTitleReplacement { get; set; } = true;

        public GenderNeutralStyle TheyThemStyle { get; set; } = GenderNeutralStyle.Friend;

        public string CustomGenderNeutralTitle { get; set; } = "friend";
        
        public bool UseFlagBasedDialogueOnly { get; set; } = true;
        
        public Configuration(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public static Configuration Load(IDalamudPluginInterface pluginInterface)
        {
            var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration(pluginInterface);
            config.pluginInterface = pluginInterface;

            // Validate sort index
            if (config.CurrentSortIndex < 0 || config.CurrentSortIndex > 4)
                config.CurrentSortIndex = 0;

            return config;
        }
        [Serializable]
        public enum GenderNeutralStyle
        {
            Friend,
            HonoredOne,
            Traveler,
            Adventurer,
            Custom
        }

        public enum RevealNamesKeyOption
        {
            Alt,
            Ctrl,
            Shift
        }

        /// <summary>
        /// A custom group of characters for random selection.
        /// Users can create groups like "DPS", "Tanks", etc. and use /select random groupname
        /// </summary>
        [Serializable]
        public class RandomGroup
        {
            public string Name { get; set; } = "";
            public List<string> CharacterNames { get; set; } = new();
        }
        
        public static Configuration LoadConfigurationSafely(IDalamudPluginInterface pluginInterface)
        {
            try
            {
                // Try to load normal configuration
                var config = Configuration.Load(pluginInterface);
                Plugin.Log.Debug("[Config] Configuration loaded successfully");
                return config;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Config] Failed to load configuration: {ex.Message}");

                // Try to restore from backup
                Plugin.Log.Info("[Config] Attempting to restore from backup...");
                var backupConfig = BackupManager.RestoreFromBackup();

                if (backupConfig != null)
                {
                    Plugin.Log.Info("[Config] Configuration restored from backup successfully!");
                    return backupConfig;
                }
                else
                {
                    Plugin.Log.Warning("[Config] Backup restoration failed, creating new configuration");
                    return new Configuration(pluginInterface);
                }
            }
        }

        public void Save()
        {
            try
            {
                // Ensure pluginInterface is set
                if (pluginInterface == null)
                {
                    Plugin.Log.Warning("pluginInterface is null, skipping save");
                    return;
                }

                pluginInterface.SavePluginConfig(this);
            }
            catch (Exception ex)
            {
                // Log but don't crash - file permission issues (antivirus, cloud sync, etc) shouldn't crash the UI
                Plugin.Log.Error($"Failed to save configuration: {ex.Message}");
            }
        }
    }
}
