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
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public List<Character> Characters { get; set; } = new List<Character>();
        public Dictionary<string, PlayerCharacter> PlayerCharacters { get; set; } = new Dictionary<string, PlayerCharacter>();
        public Vector3 NewCharacterColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = false;

        // Profile Settings
        public float ProfileImageScale { get; set; } = 1.0f; // Image scaling
        public int ProfileColumns { get; set; } = 3;        // Number of profiles per row
        public float ProfileSpacing { get; set; } = 10.0f;  // Default spacing between profiles

        private IDalamudPluginInterface pluginInterface;
        public int CurrentSortIndex { get; set; } = 0; // Default to Manual (SortType.Manual = 0)
        public PersistentPoseSet DefaultPoses { get; set; } = new();
        public bool IsQuickSwitchWindowOpen { get; set; } = false;
        public bool RememberMainWindowState { get; set; } = false;
        public bool IsMainWindowOpen { get; set; } = false;
        public bool EnableAutomations { get; set; } = false;
        public List<string> KnownTags { get; set; } = new();
        
        public bool EnableJobAssignments { get; set; } = false;
        public bool EnableGearsetAssignments { get; set; } = false;
        public bool EnableLastUsedCharacterAutoload { get; set; } = false;
        public bool EnableLastUsedDesignAutoload { get; set; } = false;
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
        public bool ApplyIdleOnLogin { get; set; } = true;
        public bool ReapplyDesignOnJobChange { get; set; } = false;
        
        // Pose Settings
        public bool? UseCommandBasedPoses { get; set; } = true;
        
        // Design Sorting
        public int CurrentDesignSortIndex { get; set; } = 1;
        public string? LastUsedDesignCharacterKey { get; set; } = null;
        public string? LastUsedCharacterKey { get; set; } = null;
        [JsonProperty]
        public bool EnablePoseAutoSave { get; set; } = true;
        public bool EnableSafeMode { get; set; } = false;
        public bool QuickSwitchCompact { get; set; } = false;
        public bool QuickSwitchIgnoreEscape { get; set; } = true;
        public bool EnableCharacterHoverEffects { get; set; } = false;
        public bool UseImGuiFilePicker { get; set; } = false;
        public List<string> PinnedFileBrowserPaths { get; set; } = new();
        public string? LastBrowserDirectory { get; set; }
        
        public Dictionary<uint, uint> GearsetJobMapping { get; set; } = new();
        [DefaultValue(false)]
        public bool RandomSelectionFavoritesOnly { get; set; } = false;

        // Random Groups - custom groups of characters for /select random <groupname>
        public List<RandomGroup> RandomGroups { get; set; } = new();

        public string? MainCharacterName { get; set; } = null; 
        public bool EnableMainCharacterOnly { get; set; } = false;
        public bool ShowMainCharacterCrown { get; set; } = true;
        public float DesignPanelWidth { get; set; } = 300f;
        
        [JsonPropertyName("enableDialogueIntegration")]
        public bool EnableDialogueIntegration { get; set; } = false;

        [JsonPropertyName("replaceNameInDialogue")]
        public bool ReplaceNameInDialogue { get; set; } = true;

        [JsonPropertyName("replacePronounsInDialogue")]
        public bool ReplacePronounsInDialogue { get; set; } = true;

        [JsonPropertyName("enableSmartGrammarInDialogue")]
        public bool EnableSmartGrammarInDialogue { get; set; } = true;

        [JsonPropertyName("showDialogueReplacementPreview")]
        public bool ShowDialogueReplacementPreview { get; set; } = false;
        
        // Enhanced dialogue
        [JsonPropertyName("enableLuaHookDialogue")]
        public bool EnableLuaHookDialogue { get; set; } = true;

        [JsonPropertyName("replaceGenderedTerms")]
        public bool ReplaceGenderedTerms { get; set; } = true;

        [JsonPropertyName("enableAdvancedTitleReplacement")]
        public bool EnableAdvancedTitleReplacement { get; set; } = true;

        [JsonPropertyName("theyThemStyle")]
        public GenderNeutralStyle TheyThemStyle { get; set; } = GenderNeutralStyle.Friend;

        [JsonPropertyName("customGenderNeutralTitle")]
        public string CustomGenderNeutralTitle { get; set; } = "friend";
        
        [JsonPropertyName("useFlagBasedDialogueOnly")]
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
        public class PersistentPoseSet
        {
            public byte Idle { get; set; } = 255;
            public byte Sit { get; set; } = 255;
            public byte GroundSit { get; set; } = 255;
            public byte Doze { get; set; } = 255;
        }
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

        public string GetGenderNeutralTitle()
        {
            return TheyThemStyle switch
            {
                GenderNeutralStyle.Friend => "friend",
                GenderNeutralStyle.HonoredOne => "Mx.",
                GenderNeutralStyle.Traveler => "traveler",
                GenderNeutralStyle.Adventurer => "adventurer",
                GenderNeutralStyle.Custom => CustomGenderNeutralTitle,
                _ => "friend"
            };
        }

        public string GetGenderNeutralFormalTitle()
        {
            return TheyThemStyle switch
            {
                GenderNeutralStyle.Friend => "friend",
                GenderNeutralStyle.HonoredOne => "Mx.",
                GenderNeutralStyle.Traveler => "traveler",
                GenderNeutralStyle.Adventurer => "adventurer",
                GenderNeutralStyle.Custom => CustomGenderNeutralTitle,
                _ => "friend"
            };
        }

        public void Save()
        {
            try
            {
                // Ensure pluginInterface is set
                if (pluginInterface == null)
                {
                    Plugin.Log.Warning("[Configuration.Save] pluginInterface is null, skipping save");
                    return;
                }

                pluginInterface.SavePluginConfig(this);
            }
            catch (Exception ex)
            {
                // Log but don't crash - file permission issues (antivirus, cloud sync, etc) shouldn't crash the UI
                Plugin.Log.Error($"[Configuration.Save] Failed to save configuration: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Custom theme colour and style overrides.
    /// </summary>
    [Serializable]
    public class CustomThemeConfig
    {
        /// <summary>
        /// Colour overrides (packed RGBA). Keys: "color.windowBg", "color.text", etc.
        /// </summary>
        public Dictionary<string, uint?> ColorOverrides { get; set; } = new();

        /// <summary>
        /// Background image path for main window.
        /// </summary>
        public string? BackgroundImagePath { get; set; }

        /// <summary>Background image opacity (0.0-1.0).</summary>
        public float BackgroundImageOpacity { get; set; } = 0.5f;

        /// <summary>Background zoom (0.5-3.0, 1.0 = fit).</summary>
        public float BackgroundImageZoom { get; set; } = 1.0f;

        /// <summary>Background X offset (-1.0 to 1.0).</summary>
        public float BackgroundImageOffsetX { get; set; } = 0f;

        /// <summary>Background Y offset (-1.0 to 1.0).</summary>
        public float BackgroundImageOffsetY { get; set; } = 0f;

        /// <summary>Favourite icon ID (0 = default Star).</summary>
        public int FavoriteIconId { get; set; } = 0;

        /// <summary>Use nameplate colour for card glow instead of custom colour.</summary>
        public bool UseNameplateColorForCardGlow { get; set; } = true;

        /// <summary>Button opacity for Compact Quick Switch (0.0-1.0).</summary>
        public float CompactQuickSwitchButtonOpacity { get; set; } = 1.0f;

        /// <summary>Deep copy for preset saving.</summary>
        public CustomThemeConfig Clone()
        {
            return new CustomThemeConfig
            {
                ColorOverrides = new Dictionary<string, uint?>(this.ColorOverrides),
                BackgroundImagePath = this.BackgroundImagePath,
                BackgroundImageOpacity = this.BackgroundImageOpacity,
                BackgroundImageZoom = this.BackgroundImageZoom,
                BackgroundImageOffsetX = this.BackgroundImageOffsetX,
                BackgroundImageOffsetY = this.BackgroundImageOffsetY,
                FavoriteIconId = this.FavoriteIconId,
                UseNameplateColorForCardGlow = this.UseNameplateColorForCardGlow,
                CompactQuickSwitchButtonOpacity = this.CompactQuickSwitchButtonOpacity
            };
        }

        /// <summary>Copy settings from another config.</summary>
        public void CopyFrom(CustomThemeConfig other)
        {
            this.ColorOverrides = new Dictionary<string, uint?>(other.ColorOverrides);
            this.BackgroundImagePath = other.BackgroundImagePath;
            this.BackgroundImageOpacity = other.BackgroundImageOpacity;
            this.BackgroundImageZoom = other.BackgroundImageZoom;
            this.BackgroundImageOffsetX = other.BackgroundImageOffsetX;
            this.BackgroundImageOffsetY = other.BackgroundImageOffsetY;
            this.FavoriteIconId = other.FavoriteIconId;
            this.UseNameplateColorForCardGlow = other.UseNameplateColorForCardGlow;
            this.CompactQuickSwitchButtonOpacity = other.CompactQuickSwitchButtonOpacity;
        }
    }

    /// <summary>Saved theme preset.</summary>
    [Serializable]
    public class ThemePreset
    {
        public string Name { get; set; } = "New Preset";
        public CustomThemeConfig Config { get; set; } = new();
    }

    /// <summary>Versioned feature keys for new-badge tracking.</summary>
    public static class FeatureKeys
    {
        public const string CustomTheme = "CustomTheme_v2.1";
        public const string JobAssignments = "JobAssignments_v2.1";
        public const string Honorific = "Honorific_v2.1";
    }
}
