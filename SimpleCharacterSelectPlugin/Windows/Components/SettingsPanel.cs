using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using SimpleCharacterSelectPlugin.Windows.Styles;
using System.Collections.Generic;
using SimpleCharacterSelectPlugin.Managers;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Windows.Components
{
    public class SettingsPanel : IDisposable
    {
        private Plugin plugin;
        private UIStyles uiStyles;
        private MainWindow mainWindow;

        // Dynamic sizing
        private bool visualSettingsOpen = true;  // Default
        private bool automationSettingsOpen = false;
        private bool behaviorSettingsOpen = false;
        private bool randomGroupsSettingsOpen = false;
        private bool mainCharacterSettingsOpen = false;
        private bool dialogueSettingsOpen = false;
        private bool characterAssignmentSettingsOpen = false;
        private bool gearsetAssignmentSettingsOpen = false;
        private bool conflictResolutionSettingsOpen = false;
        private bool backupSettingsOpen = false;
        private string? pendingExpandSection = null; // Section to force-expand on next draw
        private int selectedBlockedUserIndex = -1;
        private bool editingAssignmentUseDesign = false;
        private string editingAssignmentDesignBuffer = "";
        private string backupNameBuffer = "";
        private List<BackupFileInfo> availableBackups = new();
        private string lastBackupStatusMessage = "";

        // Random Groups
        private string newRandomGroupName = "";
        private DateTime lastBackupStatusTime = DateTime.MinValue;
        private string? pendingImportPath = null;
        private bool isCapturingRevealKey = false;

        public SettingsPanel(Plugin plugin, UIStyles uiStyles, MainWindow mainWindow)
        {
            this.plugin = plugin;
            this.uiStyles = uiStyles;
            this.mainWindow = mainWindow;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (!plugin.WindowState.IsSettingsOpen)
                return;

            // Calculate dynamic height based on expanded sections
            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier);

            var windowWidth = 480f * totalScale;

            // Calculate height based on actual content
            float baseHeight = 120f * totalScale; // Header + padding
            float sectionHeaderHeight = 30f * totalScale; // Each collapsed section header
            float totalContentHeight = 0f;

            // Add heights for expanded sections
            if (visualSettingsOpen)
                totalContentHeight += 220f * totalScale;
            else
                totalContentHeight += sectionHeaderHeight;

            if (automationSettingsOpen)
                totalContentHeight += 80f * totalScale; // Warning + checkbox
            else
                totalContentHeight += sectionHeaderHeight;

            if (behaviorSettingsOpen)
                totalContentHeight += 150f * totalScale;
            else
                totalContentHeight += sectionHeaderHeight;

            if (mainCharacterSettingsOpen)
                totalContentHeight += 120f * totalScale;
            else
                totalContentHeight += sectionHeaderHeight;

            if (dialogueSettingsOpen)
                totalContentHeight += 200f * totalScale;
            else
                totalContentHeight += sectionHeaderHeight;

            if (characterAssignmentSettingsOpen)
                totalContentHeight += 250f * totalScale;
            else
                totalContentHeight += sectionHeaderHeight;

            if (conflictResolutionSettingsOpen)
                totalContentHeight += 180f * totalScale; // Warnings + checkbox + description
            else
                totalContentHeight += sectionHeaderHeight;

            if (backupSettingsOpen)
                totalContentHeight += 300f * totalScale; // Backup controls + status + file list
            else
                totalContentHeight += sectionHeaderHeight;

            var windowHeight = Math.Min(baseHeight + totalContentHeight, 700f * totalScale); // Cap at reasonable max
            var minHeight = 200f * totalScale; // Minimum height
            windowHeight = Math.Max(windowHeight, minHeight);

            // Center window
            var viewport = ImGui.GetMainViewport();
            var centerPos = new Vector2(
                viewport.Pos.X + (viewport.Size.X - windowWidth) * 0.5f,
                viewport.Pos.Y + (viewport.Size.Y - windowHeight) * 0.5f
            );

            ImGui.SetNextWindowPos(centerPos, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight), ImGuiCond.Always);

            bool isSettingsOpen = plugin.WindowState.IsSettingsOpen;
            var windowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;

            if (ImGui.Begin("Simple Character Select Settings", ref isSettingsOpen, windowFlags))
            {
                if (!isSettingsOpen)
                    plugin.WindowState.IsSettingsOpen = false;

                ApplyFixedStyles(totalScale);

                try
                {
                    DrawFixedSettingsContent();
                }
                finally
                {
                    ImGui.PopStyleVar(4);
                    ImGui.PopStyleColor(6);
                }
            }
            ImGui.End();
        }

        private void ApplyFixedStyles(float totalScale)
        {
            // Styling
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.1f, 0.98f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.12f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.95f, 0.95f, 0.95f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.16f, 0.16f, 0.2f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.22f, 0.22f, 0.28f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.28f, 0.28f, 0.35f, 1.0f));

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f * totalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f * totalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8 * totalScale, 5 * totalScale));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6 * totalScale, 3 * totalScale));
        }

        private void DrawFixedSettingsContent()
        {
            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier);
            var contentWidth = ImGui.GetContentRegionAvail().X;
            var labelWidth = 140f * totalScale;
            var inputWidth = contentWidth - labelWidth - (20f * totalScale);

            // Scrollable content area for all settings
            if (ImGui.BeginChild("AllSettings", new Vector2(0, 0), false))
            {
                // Visual Settings Section
                visualSettingsOpen = DrawModernCollapsingHeader("Visual Settings", new Vector4(1.0f, 0.35f, 0.35f, 1.0f), visualSettingsOpen);
                if (visualSettingsOpen)
                {
                    DrawVisualSettings(labelWidth, inputWidth);
                }

                // Glamourer Automations Section TODO readd if needed
                // automationSettingsOpen = DrawModernCollapsingHeader("Glamourer Automations", new Vector4(1.0f, 0.6f, 0.2f, 1.0f), automationSettingsOpen);
                // if (automationSettingsOpen)
                // {
                //     DrawAutomationSettings();
                // }

                // Behavior Settings Section
                behaviorSettingsOpen = DrawModernCollapsingHeader("Behavior Settings", new Vector4(1.0f, 0.9f, 0.3f, 1.0f), behaviorSettingsOpen);
                if (behaviorSettingsOpen)
                {
                    DrawBehaviorSettings();
                }

                // Random Groups Section TODO readd if anyone asks for it
                // randomGroupsSettingsOpen = DrawModernCollapsingHeader("Random Groups", new Vector4(0.85f, 0.95f, 0.3f, 1.0f), randomGroupsSettingsOpen);
                // if (randomGroupsSettingsOpen)
                // {
                //     DrawRandomGroupsSettings();
                // }

                // Main Character Section
                mainCharacterSettingsOpen = DrawModernCollapsingHeader("Main Character", new Vector4(0.3f, 0.9f, 0.4f, 1.0f), mainCharacterSettingsOpen);
                if (mainCharacterSettingsOpen)
                {
                    DrawMainCharacterSettings(labelWidth, inputWidth);
                }

                // Character Assignments
                characterAssignmentSettingsOpen = DrawModernCollapsingHeader("Character Assignments", new Vector4(0.3f, 0.9f, 0.9f, 1.0f), characterAssignmentSettingsOpen);
                if (characterAssignmentSettingsOpen)
                {
                    DrawSoonge();
                }

                // Gearset Assignments
                gearsetAssignmentSettingsOpen = DrawModernCollapsingHeader("Gearset Assignments", new Vector4(0.2f, 0.8f, 0.85f, 1.0f), gearsetAssignmentSettingsOpen);
                if (gearsetAssignmentSettingsOpen)
                {
                    DrawGearsetAssignmentSettings();
                }

                // Immersive Dialogue (Blue)
                dialogueSettingsOpen = DrawModernCollapsingHeader("Immersive Dialogue", new Vector4(0.4f, 0.6f, 1.0f, 1.0f), dialogueSettingsOpen);
                if (dialogueSettingsOpen)
                {
                    DrawSoonge();
                }

                // Backup & Restore (Pink/Magenta)
                backupSettingsOpen = DrawModernCollapsingHeader("Backup & Restore", new Vector4(1.0f, 0.45f, 0.7f, 1.0f), backupSettingsOpen);
                if (backupSettingsOpen)
                {
                    DrawBackupSettings();
                }
            }
            ImGui.EndChild();
        }

        private bool DrawModernCollapsingHeader(string title, Vector4 titleColor, bool currentState)
        {
            return DrawModernCollapsingHeader(title, titleColor, currentState, null);
        }

        private bool DrawModernCollapsingHeader(string title, Vector4 titleColor, bool currentState, string? featureKey)
        {
            var flags = currentState ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.SpanFullWidth;

            // Check if this section should be force-expanded (from ExpandSection call)
            if (pendingExpandSection == title)
            {
                ImGui.SetNextItemOpen(true);
                pendingExpandSection = null;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White text
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(titleColor.X * 0.6f, titleColor.Y * 0.6f, titleColor.Z * 0.6f, 0.7f)); // More vibrant
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(titleColor.X * 0.7f, titleColor.Y * 0.7f, titleColor.Z * 0.7f, 0.8f)); // More vibrant
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(titleColor.X * 0.8f, titleColor.Y * 0.8f, titleColor.Z * 0.8f, 0.9f)); // More vibrant

            bool isOpen = ImGui.CollapsingHeader(title, flags);

            ImGui.PopStyleColor(4);

            if (isOpen)
            {
                ImGui.Spacing();
            }

            return isOpen;
        }

        private void DrawVisualSettings(float labelWidth, float inputWidth)
        {
            // Profile Image Scale
            DrawFixedSetting("Profile Image Scale:", labelWidth, inputWidth, () =>
            {
                float tempScale = plugin.WindowState.ProfileImageScale;
                if (ImGui.SliderFloat("##ProfileImageScale", ref tempScale, 0.5f, 2.0f, "%.1f"))
                {
                    plugin.WindowState.ProfileImageScale = tempScale;
                    plugin.SaveConfiguration();
                    // Force MainWindow layout invalidation
                    mainWindow.InvalidateLayout();
                    Plugin.Log.Debug($"[Settings] Profile Image Scale changed to {tempScale}");
                }
                DrawTooltip("Adjusts the size of character profile images in the grid.");
            });

            // Profiles Per Row
            DrawFixedSetting("Profiles Per Row:", labelWidth, inputWidth * 0.5f, () =>
            {
                int tempColumns = plugin.WindowState.ProfileColumns;
                if (ImGui.InputInt("##ProfilesPerRow", ref tempColumns, 1, 1))
                {
                    tempColumns = Math.Clamp(tempColumns, 1, 6);
                    plugin.WindowState.ProfileColumns = tempColumns;
                    plugin.SaveConfiguration();
                    // Force MainWindow layout invalidation
                    mainWindow.InvalidateLayout();
                    Plugin.Log.Debug($"[Settings] Profile Columns changed to {tempColumns}");
                }
                DrawTooltip("Number of character profiles to display per row.");
            });

            // Profile Spacing
            DrawFixedSetting("Profile Spacing:", labelWidth, inputWidth, () =>
            {
                float tempSpacing = plugin.WindowState.ProfileSpacing;
                if (ImGui.SliderFloat("##ProfileSpacing", ref tempSpacing, 0.0f, 50.0f, "%.1f"))
                {
                    plugin.WindowState.ProfileSpacing = tempSpacing;
                    plugin.SaveConfiguration();
                    // Force MainWindow layout invalidation
                    mainWindow.InvalidateLayout();
                    Plugin.Log.Debug($"[Settings] Profile Spacing changed to {tempSpacing}");
                }
                DrawTooltip("Spacing between character profile cards.");
            });


            // Sort Characters By
            DrawFixedSetting("Sort Characters By:", labelWidth, inputWidth, () =>
            {
                var currentSort = (Plugin.SortType)Plugin.Configuration.CurrentSortIndex;
                if (ImGui.BeginCombo("##SortDropdown", currentSort.ToString()))
                {
                    if (ImGui.Selectable("Favourites", currentSort == Plugin.SortType.Favorites))
                    {
                        Plugin.Configuration.CurrentSortIndex = (int)Plugin.SortType.Favorites;
                        Plugin.Configuration.Save();
                        mainWindow.UpdateSortType();
                    }
                    if (ImGui.Selectable("Alphabetical", currentSort == Plugin.SortType.Alphabetical))
                    {
                        Plugin.Configuration.CurrentSortIndex = (int)Plugin.SortType.Alphabetical;
                        Plugin.Configuration.Save();
                        mainWindow.UpdateSortType();
                    }
                    if (ImGui.Selectable("Most Recent", currentSort == Plugin.SortType.Recent))
                    {
                        Plugin.Configuration.CurrentSortIndex = (int)Plugin.SortType.Recent;
                        Plugin.Configuration.Save();
                        mainWindow.UpdateSortType();
                    }
                    if (ImGui.Selectable("Oldest", currentSort == Plugin.SortType.Oldest))
                    {
                        Plugin.Configuration.CurrentSortIndex = (int)Plugin.SortType.Oldest;
                        Plugin.Configuration.Save();
                        mainWindow.UpdateSortType();
                    }
                    if (ImGui.Selectable("Manual", currentSort == Plugin.SortType.Manual))
                    {
                        Plugin.Configuration.CurrentSortIndex = (int)Plugin.SortType.Manual;
                        Plugin.Configuration.Save();
                        mainWindow.UpdateSortType();
                    }
                    ImGui.EndCombo();
                }
                DrawTooltip("Choose how characters are sorted in the main grid.");
            });

            ImGui.Spacing();
        }

        private void DrawHonorificSettings()
        {
            // Important setup info
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.85f, 0.4f, 1.0f));
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf071"); // Warning icon
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text("Note");
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.9f, 0.7f, 1.0f));
            ImGui.TextWrapped("Animated title glows (Wave, Pulse, Static) require the corresponding option to be enabled in Honorific's plugin settings as well.");
            ImGui.PopStyleColor();

            ImGui.Spacing();
        }

        private void DrawAutomationSettings()
        {
            // // Warning
            // ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.7f, 0.3f, 1f));
            // ImGui.TextWrapped("Warning: Requires 'None' automation in Glamourer");
            // ImGui.PopStyleColor();
            // ImGui.Spacing();
            //
            // bool automationToggle = Plugin.Configuration.EnableAutomations;
            // if (ImGui.Checkbox("Enable Automations", ref automationToggle))
            // {
            //     Plugin.Configuration.EnableAutomations = automationToggle;
            //     UpdateAutomationSettings(automationToggle);
            // }
            // DrawTooltip("Enable support for Glamourer Automations in Characters & Designs.\n\nWhen enabled, you'll be able to assign an Automation to each character & design.\nCharacters & Designs without automations will require a fallback Automation in Glamourer named: \"None\"\nYou also must enter your player character name in Glamourer next to \"Any World\" and Set to character.Data.");
            //
            // ImGui.Spacing();
        }

        private void DrawBehaviorSettings()
        {
            bool enableCompactQuickSwitch = Plugin.Configuration.QuickSwitchCompact;
            if (ImGui.Checkbox("Compact Quick Switch Bar", ref enableCompactQuickSwitch))
            {
                Plugin.Configuration.QuickSwitchCompact = enableCompactQuickSwitch;
                Plugin.Configuration.Save();
            }
            DrawTooltip("When enabled, the Quick Switch window will hide its title bar and frame, showing only the dropdowns and apply button.");

            bool quickSwitchIgnoreEscape = Plugin.Configuration.QuickSwitchIgnoreEscape;
            if (ImGui.Checkbox("Quick Switch ignores Escape key", ref quickSwitchIgnoreEscape))
            {
                Plugin.Configuration.QuickSwitchIgnoreEscape = quickSwitchIgnoreEscape;
                Plugin.Configuration.Save();
            }
            DrawTooltip("When enabled, pressing Escape won't close the Quick Switch window.\nThis also prevents Quick Switch from stealing focus when opened.");

            bool enableAutoload = Plugin.Configuration.EnableLastUsedCharacterAutoload;
            if (ImGui.Checkbox("Auto-Apply Last Used Design on Login", ref enableAutoload))
            {
                Plugin.Configuration.EnableLastUsedCharacterAutoload = enableAutoload;
                Plugin.Configuration.Save();
            }
            DrawTooltip("Automatically applies the last character and design you used when logging into the game.");

            // bool applyIdle = Plugin.Configuration.ApplyIdleOnLogin; //TODO readd if anyone asks for it
            // if (ImGui.Checkbox("Apply idle pose on login", ref applyIdle))
            // {
            //     Plugin.Configuration.ApplyIdleOnLogin = applyIdle;
            //     Plugin.Configuration.Save();
            // }
            // DrawTooltip("Automatically applies your idle pose after logging in. Disable if you're seeing pose bugs.");

            bool reapplyDesign = Plugin.Configuration.ReapplyDesignOnJobChange;
            if (ImGui.Checkbox("Reapply last design on job change", ref reapplyDesign))
            {
                Plugin.Configuration.ReapplyDesignOnJobChange = reapplyDesign;
                Plugin.Configuration.Save();
            }
            DrawTooltip("If checked, Simple Character Select will reapply the last used design when you switch jobs.");

            // bool randomFavoritesOnly = Plugin.Configuration.RandomSelectionFavoritesOnly;
            // if (ImGui.Checkbox("Random Selection: Favourites Only", ref randomFavoritesOnly))
            // {
            //     Plugin.Configuration.RandomSelectionFavoritesOnly = randomFavoritesOnly;
            //     Plugin.Configuration.Save();
            // }
            // DrawTooltip("When enabled, random selection will only choose from favourited characters and designs.\nRequires at least one favourited character to work.");
            
            ImGui.Spacing();
        }

        private void DrawRandomGroupsSettings()
        {
            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier);

            ImGui.TextWrapped("Create custom groups of characters for random selection. Use /select random <groupname> to pick randomly from a group.");
            ImGui.Spacing();

            // Create new group row
            ImGui.SetNextItemWidth(180 * totalScale);
            ImGui.InputTextWithHint("##NewGroupName", "Group name...", ref newRandomGroupName, 50);
            ImGui.SameLine();

            bool canCreate = !string.IsNullOrWhiteSpace(newRandomGroupName) &&
                !Plugin.Configuration.RandomGroups.Any(g => g.Name.Equals(newRandomGroupName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!canCreate) ImGui.BeginDisabled();
            if (ImGui.Button("Create Group"))
            {
                Plugin.Configuration.RandomGroups.Add(new Configuration.RandomGroup
                {
                    Name = newRandomGroupName.Trim()
                });
                Plugin.Configuration.Save();
                newRandomGroupName = "";
            }
            if (!canCreate) ImGui.EndDisabled();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (Plugin.Configuration.RandomGroups.Count == 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
                ImGui.TextWrapped("No groups yet. Create one above to get started.");
                ImGui.PopStyleColor();
            }
            else
            {
                // List existing groups
                int groupToDelete = -1;

                for (int i = 0; i < Plugin.Configuration.RandomGroups.Count; i++)
                {
                    var group = Plugin.Configuration.RandomGroups[i];
                    ImGui.PushID($"RandomGroup_{i}");

                    bool isExpanded = expandedRandomGroups.Contains(i);
                    var characterCount = group.CharacterNames.Count;

                    // Header row with expand icon, name, count, and delete button
                    var icon = isExpanded ? FontAwesomeIcon.ChevronDown.ToIconString() : FontAwesomeIcon.ChevronRight.ToIconString();
                    ImGui.PushID($"Expand_{i}");
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(icon, new Vector2(24 * totalScale, 0)))
                    {
                        if (isExpanded)
                            expandedRandomGroups.Remove(i);
                        else
                            expandedRandomGroups.Add(i);
                    }
                    ImGui.PopFont();
                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.Text(group.Name);

                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1f));
                    ImGui.Text($"({characterCount})");
                    ImGui.PopStyleColor();

                    // Delete button on right
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - 35 * totalScale);
                    ImGui.PushID($"Delete_{i}");
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.3f, 0.3f, 1f));
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Times.ToIconString(), new Vector2(24 * totalScale, 0)))
                    {
                        groupToDelete = i;
                    }
                    ImGui.PopFont();
                    ImGui.PopStyleColor();
                    ImGui.PopID();
                    DrawTooltip("Delete group");

                    // Expanded content
                    if (isExpanded)
                    {
                        ImGui.Indent(24 * totalScale);

                        // Command hint
                        var cmdName = group.Name.ToLower().Replace(" ", "");
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.65f, 0.4f, 1f));
                        ImGui.Text($"/select random {cmdName}");
                        ImGui.PopStyleColor();

                        ImGui.Spacing();

                        // Character checkboxes
                        var characters = Plugin.Configuration.Characters;
                        if (characters.Count == 0)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
                            ImGui.Text("No characters created yet.");
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            // Calculate column width for 2 columns
                            var availWidth = ImGui.GetContentRegionAvail().X;
                            var colWidth = availWidth / 2 - 5;

                            for (int c = 0; c < characters.Count; c++)
                            {
                                var character = characters[c];
                                bool isInGroup = group.CharacterNames.Contains(character.Data.Name);

                                // Start new row for even indices
                                if (c % 2 == 1)
                                {
                                    ImGui.SameLine(24 * totalScale + colWidth + 10);
                                }

                                ImGui.PushID($"Char_{c}");
                                ImGui.SetNextItemWidth(colWidth);
                                if (ImGui.Checkbox(character.Data.Name.Length > 20 ? character.Data.Name.Substring(0, 17) + "..." : character.Data.Name, ref isInGroup))
                                {
                                    if (isInGroup && !group.CharacterNames.Contains(character.Data.Name))
                                        group.CharacterNames.Add(character.Data.Name);
                                    else if (!isInGroup)
                                        group.CharacterNames.Remove(character.Data.Name);
                                    Plugin.Configuration.Save();
                                }
                                if (character.Data.Name.Length > 20)
                                    DrawTooltip(character.Data.Name);
                                ImGui.PopID();
                            }
                        }

                        ImGui.Unindent(24 * totalScale);
                        ImGui.Spacing();
                    }

                    ImGui.PopID();
                }

                // Handle deletion outside the loop
                if (groupToDelete >= 0)
                {
                    Plugin.Configuration.RandomGroups.RemoveAt(groupToDelete);
                    Plugin.Configuration.Save();
                    expandedRandomGroups.Remove(groupToDelete);
                    // Adjust expanded indices for groups after the deleted one
                    var newExpanded = new HashSet<int>();
                    foreach (var idx in expandedRandomGroups)
                    {
                        if (idx > groupToDelete)
                            newExpanded.Add(idx - 1);
                        else
                            newExpanded.Add(idx);
                    }
                    expandedRandomGroups = newExpanded;
                }
            }
        }

        // Tracks which random groups are expanded
        private HashSet<int> expandedRandomGroups = new();

        private void DrawMainCharacterSettings(float labelWidth, float inputWidth)
        {
            bool enableMainCharacterOnly = Plugin.Configuration.EnableMainCharacterOnly;
            if (ImGui.Checkbox("Enable Main Character Only Mode", ref enableMainCharacterOnly))
            {
                Plugin.Configuration.EnableMainCharacterOnly = enableMainCharacterOnly;
                Plugin.Configuration.Save();
            }
            DrawTooltip("When enabled, only your designated main character will auto-apply on login.\nIf no main character is set, the normal auto-apply behavior will be used.");

            bool showCrown = Plugin.Configuration.ShowMainCharacterCrown;
            if (ImGui.Checkbox("Show Crown Icon on Main Character", ref showCrown))
            {
                Plugin.Configuration.ShowMainCharacterCrown = showCrown;
                Plugin.Configuration.Save();
            }
            DrawTooltip("When enabled, the main character will display a crown icon in the top corner of their image.");

            DrawFixedSetting("Select Main Character:", labelWidth, inputWidth, () =>
            {
                string currentMainChar = Plugin.Configuration.MainCharacterName ?? "None";

                if (ImGui.BeginCombo("##MainCharacterDropdown", currentMainChar))
                {
                    if (ImGui.Selectable("None", currentMainChar == "None"))
                    {
                        Plugin.Configuration.MainCharacterName = null;
                        Plugin.Configuration.Save();
                    }

                    foreach (var character in plugin.Characters)
                    {
                        bool isSelected = character.Data.Name == currentMainChar;
                        if (ImGui.Selectable(character.Data.Name, isSelected))
                        {
                            Plugin.Configuration.MainCharacterName = character.Data.Name;
                            Plugin.Configuration.Save();
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                DrawTooltip("Select which character should be designated as your main character.Data.\nThe main character will be marked with a crown icon and can be set to auto-apply exclusively on login.");
            });

            // Status display
            if (!string.IsNullOrEmpty(Plugin.Configuration.MainCharacterName))
            {
                var mainCharacter = plugin.Characters.FirstOrDefault(c => c.Data.Name == Plugin.Configuration.MainCharacterName);
                if (mainCharacter != null)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.9f, 0.7f, 1f));
                    ImGui.Text($"Current Main: {mainCharacter.Data.Name}");
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.6f, 0.6f, 1f));
                    ImGui.Text("Main character not found");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    if (ImGui.SmallButton("Clear"))
                    {
                        Plugin.Configuration.MainCharacterName = null;
                        Plugin.Configuration.Save();
                    }
                }
            }

            ImGui.Spacing();
        }


        private void DrawSoonge()
        {
            ImGui.TextWrapped("This feature will be returning in the near future.");
        }
        private void DrawDialogueSettings()
        {   
            //TODO readd
            // Warning
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.4f, 1f));
            ImGui.TextWrapped("Uses your SCS Character's name and pronouns in NPC dialogue");
            ImGui.PopStyleColor();

            // They/Them pronoun chat display warning
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.5f, 1f));
            ImGui.TextWrapped("Note: Users with They/Them pronouns may occasionally see garbled text in chat. Simply switch between chat tabs to refresh the display if this occurs.");
            ImGui.PopStyleColor();

            ImGui.Spacing();

            bool enableDialogue = Plugin.Configuration.EnableDialogueIntegration;
            if (ImGui.Checkbox("Enable Immersive Dialogue", ref enableDialogue))
            {
                Plugin.Configuration.EnableDialogueIntegration = enableDialogue;

                // Reset all dialogue sub-settings when disabled
                if (!enableDialogue)
                {
                    Plugin.Configuration.EnableLuaHookDialogue = false;
                    Plugin.Configuration.ReplaceNameInDialogue = false;
                    Plugin.Configuration.ReplacePronounsInDialogue = false;
                    Plugin.Configuration.ReplaceGenderedTerms = false;
                    Plugin.Configuration.EnableAdvancedTitleReplacement = false;
                    Plugin.Configuration.EnableSmartGrammarInDialogue = false;
                    //Plugin.Configuration.EnableRaceReplacement = false;
                    Plugin.Configuration.ShowDialogueReplacementPreview = false; // Off by default
                }
                else
                {
                    // Set good defaults when enabling
                    Plugin.Configuration.EnableLuaHookDialogue = true;
                    Plugin.Configuration.ReplaceNameInDialogue = true;
                    Plugin.Configuration.ReplacePronounsInDialogue = true;
                    Plugin.Configuration.ReplaceGenderedTerms = true;
                    Plugin.Configuration.EnableAdvancedTitleReplacement = true;
                    //Plugin.Configuration.EnableSmartGrammarInDialogue = true; TODO
                    Plugin.Configuration.ShowDialogueReplacementPreview = false; // Keep off by default
                }

                Plugin.Configuration.Save();
            }
            DrawTooltip("Replaces NPC dialogue text to use your SCS Character's name and pronouns instead of your game character.Data.\nRequires an active SCS character with RP Profile data.");

            if (Plugin.Configuration.EnableDialogueIntegration)
            {
                ImGui.Indent();

                // Simplified user-facing options
                bool replaceName = Plugin.Configuration.ReplaceNameInDialogue;
                if (ImGui.Checkbox("Use SCS Character Name", ref replaceName))
                {
                    Plugin.Configuration.ReplaceNameInDialogue = replaceName;
                    Plugin.Configuration.Save();
                }
                DrawTooltip("Replace your real character name with your SCS character name in dialogue.");

                bool replacePronouns = Plugin.Configuration.ReplacePronounsInDialogue;
                if (ImGui.Checkbox("Use SCS Character Pronouns", ref replacePronouns))
                {
                    Plugin.Configuration.ReplacePronounsInDialogue = replacePronouns;
                    Plugin.Configuration.Save();
                }
                DrawTooltip("Replace pronouns in dialogue with your character's pronouns from their RP Profile.");

                bool replaceGenderedTerms = Plugin.Configuration.ReplaceGenderedTerms;
                if (ImGui.Checkbox("Use Gender-Neutral Terms", ref replaceGenderedTerms))
                {
                    Plugin.Configuration.ReplaceGenderedTerms = replaceGenderedTerms;
                    Plugin.Configuration.Save();
                }
                DrawTooltip("Replace gendered terms like 'sir/lady', 'man/woman' with appropriate alternatives based on your character's pronouns.");

                //bool replaceRace = Plugin.Configuration.EnableRaceReplacement; TODO
                //if (ImGui.Checkbox("Use SCS Character Race", ref replaceRace))
                //{
                //    Plugin.Configuration.EnableRaceReplacement = replaceRace;
                //    Plugin.Configuration.Save();
                //}
                //DrawTooltip("Replace your race with your SCS character's race from their RP Profile.");

                // They/Them settings section
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.9f, 1.0f));
                ImGui.Text("They/Them Pronoun Settings");
                ImGui.PopStyleColor();
                ImGui.Spacing();

                // Use proper fixed layout like other settings
                var contentWidth = ImGui.GetContentRegionAvail().X;
                var labelWidth = 140f;
                var inputWidth = contentWidth - labelWidth - 20f;

                DrawFixedSetting("Neutral Title Style:", labelWidth, inputWidth, () =>
                {
                    var currentStyle = (int)Plugin.Configuration.TheyThemStyle;
                    string[] styleOptions = { "Friend", "Mx.", "Traveler", "Adventurer", "Custom" };

                    if (ImGui.Combo("##TheyThemStyle", ref currentStyle, styleOptions, styleOptions.Length))
                    {
                        Plugin.Configuration.TheyThemStyle = (Configuration.GenderNeutralStyle)currentStyle;
                        Plugin.Configuration.Save();
                    }
                    DrawTooltip("Friend: \"honored sir\" → \"honored friend\"\nMx.: \"honored sir\" → \"honored Mx.\"\nTraveler: \"honored sir\" → \"honored traveler\"\nAdventurer: \"honored sir\" → \"honored adventurer\"");
                });

                if (Plugin.Configuration.TheyThemStyle == Configuration.GenderNeutralStyle.Custom)
                {
                    DrawFixedSetting("Custom Title:", labelWidth, inputWidth, () =>
                    {
                        var customTitle = Plugin.Configuration.CustomGenderNeutralTitle;
                        if (ImGui.InputText("##CustomGenderNeutral", ref customTitle, 50))
                        {
                            Plugin.Configuration.CustomGenderNeutralTitle = customTitle;
                            Plugin.Configuration.Save();
                        }
                        DrawTooltip("Enter your preferred gender-neutral title (e.g., \"Warrior\", \"Champion\", \"Canadian\")");
                    });
                }

                // Preview with proper styling
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
                var characterName = plugin.Characters.FirstOrDefault()?.Data.Name ?? "Warrior of Light";
                ImGui.Text($"Preview: \"Sir {characterName}\" -> \"{GenderManager.GetGenderNeutralTitle()} {characterName}\"");
                ImGui.PopStyleColor();

                ImGui.Unindent();
            }

            ImGui.Spacing();
        }

        private void DrawCharacterAssignmentSettings()
        {
            // Warning if Auto-Apply Last Used Character is disabled
            if (!Plugin.Configuration.EnableLastUsedCharacterAutoload)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.7f, 0.3f, 1f));
            
                // Warning icon
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("\uf071"); // FontAwesome warning triangle
                ImGui.PopFont();
            
                ImGui.SameLine();
                ImGui.TextWrapped("Auto-Apply Last Used Character on Login is disabled - Character Assignments require this feature.");
                ImGui.PopStyleColor();
            
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.8f, 1f));
                ImGui.TextWrapped("Enable Auto-Apply Last Used Character on Login in the Automation Settings section to use assignments.");
                ImGui.PopStyleColor();
            
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
            
            // Warning if Main Character Only Mode is enabled
            if (Plugin.Configuration.EnableMainCharacterOnly)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.7f, 0.3f, 1f));
            
                // Warning icon
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("\uf071"); // FontAwesome warning triangle
                ImGui.PopFont();
            
                ImGui.SameLine();
                ImGui.TextWrapped("Main Character Only Mode is enabled - Character Assignments will be ignored.");
                ImGui.PopStyleColor();
            
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.8f, 1f));
                ImGui.TextWrapped("Disable Main Character Only Mode in the Main Character section to use assignments.");
                ImGui.PopStyleColor();
            
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
            
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.9f, 1.0f, 1f));
            ImGui.TextWrapped("Assign specific SCS Characters to auto-apply when logging into specific player characters.");
            ImGui.PopStyleColor();
            
            ImGui.Spacing();
            
            Character? currentlySelectedChar = null;
            PlayerCharacter? currentlySelectedPc = null;
            PlayerCharacter? newCharacter = null;
            
            // Display current assignments
            List<PlayerCharacter> assignedCharacters =
                CharacterManager.GetPlayerCharactersWithAssignments(Plugin.Configuration.PlayerCharacters);
            if (assignedCharacters.Any())
            {
                ImGui.Text("Current Assignments:");
                ImGui.Spacing();
            
                // Calculate button widths for layout
                float editButtonWidth = ImGui.CalcTextSize("Edit").X + ImGui.GetStyle().FramePadding.X * 2 + 4;
                float removeButtonWidth = ImGui.CalcTextSize("Remove").X + ImGui.GetStyle().FramePadding.X * 2 + 4;
                float buttonSpacing = ImGui.GetStyle().ItemSpacing.X;
                float totalButtonWidth = editButtonWidth + removeButtonWidth + buttonSpacing * 2;
                float availableWidth = ImGui.GetContentRegionAvail().X;
                float maxTextWidth = availableWidth - totalButtonWidth - 10; // 10px padding
            
                foreach (var a in assignedCharacters)
                {
                    // Build the full display text
                    string displayText = $"{a.FullName} → {a.AssignedCharacter!.Data.Name}";
            
                    // Truncate if too long
                    string truncatedText = displayText;
                    if (ImGui.CalcTextSize(displayText).X > maxTextWidth)
                    {
                        while (truncatedText.Length > 3 && ImGui.CalcTextSize(truncatedText + "...").X > maxTextWidth)
                        {
                            truncatedText = truncatedText.Substring(0, truncatedText.Length - 1);
                        }
                        truncatedText += "...";
                    }
            
                    // Draw coloured text segments
                    var arrowIndex = truncatedText.IndexOf(" → ");
                    if (arrowIndex > 0)
                    {
                        var inGamePart = truncatedText.Substring(0, arrowIndex);
                        var restPart = truncatedText.Substring(arrowIndex);
            
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.9f, 0.7f, 1f));
                        ImGui.Text(inGamePart);
                        ImGui.PopStyleColor();
            
                        ImGui.SameLine(0, 0);
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.6f, 1f));
                        ImGui.Text(restPart);
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.Text(truncatedText);
                    }
            
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(displayText);
                    }
            
                    // Position buttons on the right
                    ImGui.SameLine(availableWidth - totalButtonWidth + buttonSpacing);
            
                    // Edit button
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.6f, 0.8f, 0.6f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.7f, 0.9f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.8f, 1.0f, 1.0f));
            
                    if (ImGui.SmallButton($"Edit##{a.FullName}"))
                    {
                        DrawEditAssignment(a);
                    }
                    ImGui.PopStyleColor(3);
            
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Edit assignment for {a.FullName}");
                    }
            
                    ImGui.SameLine();
            
                    // Remove button
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.3f, 0.3f, 0.6f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.4f, 0.4f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
            
                    if (ImGui.SmallButton($"Remove##{a.FullName}"))
                    {
                        a.AssignedCharacter = null;
                        Plugin.Configuration.PlayerCharacters[a.FullName] = a;
                    }
                    ImGui.PopStyleColor(3);
            
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Remove assignment for {a.FullName}");
                    }
                }
            
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
            
            // Add new assignment section
            ImGui.Text("Add New Assignment:");
            ImGui.Spacing();
            
            // Get list of known real characters from existing tracking
            var knownPCs = Plugin.Configuration.PlayerCharacters;
            
            ImGui.Text("Player Character:");
            ImGui.SetNextItemWidth(300f);
            
            if (knownPCs.Any())
            {
                // Dropdown of known characters
                if (ImGui.BeginCombo("##RealCharSelect", currentlySelectedPc != null ? currentlySelectedPc.FullName : "--------" ))
                {
                    foreach (var realChar in knownPCs)
                    {
                        bool isSelected = realChar.Value.FullName == currentlySelectedPc?.FullName;
                        if (ImGui.Selectable(realChar.Value.FullName, isSelected))
                        {
                            currentlySelectedPc = realChar.Value;
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                DrawTooltip("Select from characters the plugin has seen before, or type manually below.");
            
                // Manual input as backup
                string newCharacterName = "";
                ImGui.Text("Or enter manually:");
                ImGui.SetNextItemWidth(300f);
                if (ImGui.InputTextWithHint("##RealCharManual", "First Last@WorldName", ref newCharacterName, 100))
                {
                    newCharacter = CharacterManager.NewPlayerCharacter(newCharacterName);
                    // TODO if name matches existing
                }
                DrawTooltip("Enter the exact character name and world as it appears in-game.\nExample: James Stone@Hyperion");
            }
            
            ImGui.Spacing();
            
            ImGui.Text("SCS Character:");
            ImGui.SetNextItemWidth(300f);
            if (ImGui.BeginCombo("##SCSChar", currentlySelectedChar != null ? currentlySelectedChar.Data.Name : "--------" ))
            {
                // Add "None" option first
                if (ImGui.Selectable("None", currentlySelectedChar == null))
                {
                    currentlySelectedChar = null;
                }
            
                // Add separator
                ImGui.Separator();
            
                // Add all SCS characters
                foreach (var character in plugin.Characters)
                {
                    if (ImGui.Selectable(character.Data.Name, character.Data.Name == currentlySelectedChar?.Data.Name))
                    {
                        if (character.Data.Name != currentlySelectedChar?.Data.Name)
                        {
                            currentlySelectedChar = character;
                        }
                    }
                }
                ImGui.EndCombo();
            }
            DrawTooltip("Choose which SCS character should auto-apply for this player character.\nSelect 'None' to prevent any auto-application for this player character.");
            
            bool canAdd = currentlySelectedPc != null && currentlySelectedChar != null && !Plugin.Configuration.PlayerCharacters.ContainsKey(currentlySelectedPc.FullName);
            
            if (!canAdd)
                ImGui.BeginDisabled();
            
            if (ImGui.Button("Add Assignment"))
            {
                Plugin.Configuration.PlayerCharacters[currentlySelectedPc.FullName] = currentlySelectedPc;
                Plugin.Configuration.Save();
                Plugin.Log.Debug($"[CharacterAssignment] Added: {currentlySelectedPc.FullName} → {currentlySelectedChar?.Data.Name}");
            }
            
            if (!canAdd)
            {
                ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.6f, 0.6f, 1f));
                ImGui.Text("Assignment already exists");
                ImGui.PopStyleColor();
            }
            
            ImGui.Spacing();
        }
        
        private void DrawEditAssignment(PlayerCharacter pc)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.6f, 1f));
            ImGui.Text($"Editing Assignment: {pc.FullName}");
            ImGui.PopStyleColor();
            ImGui.Spacing();

            Character? currentlySelectedChar = pc.AssignedCharacter;
            
            ImGui.Text("SCS Character:");
            ImGui.SetNextItemWidth(300f);
            if (ImGui.BeginCombo("##EditSCSChar", currentlySelectedChar != null ? currentlySelectedChar.Data.Name : "--------" ))
            {
                // Add "None" option first
                if (ImGui.Selectable("None", currentlySelectedChar == null))
                {
                    currentlySelectedChar = null;
                }

                // Add separator
                ImGui.Separator();

                // Add all SCS characters
                foreach (var character in plugin.Characters)
                {
                    if (ImGui.Selectable(character.Data.Name, character.Data.Name == currentlySelectedChar?.Data.Name))
                    {
                        if (character.Data.Name != currentlySelectedChar?.Data.Name)
                        {
                            currentlySelectedChar = character;
                        }
                    }
                }
                ImGui.EndCombo();
            }
            DrawTooltip("Choose which SCS character should auto-apply for this player character.\nSelect 'None' to prevent any auto-application for this player character.");
            
            if (currentlySelectedChar != null)
            {
                ImGui.Spacing();
                if (ImGui.Checkbox("Use specific design##Edit", ref editingAssignmentUseDesign))
                {
                    if (!editingAssignmentUseDesign)
                        editingAssignmentDesignBuffer = "";
                }

                if (editingAssignmentUseDesign)
                {
                    ImGui.SetNextItemWidth(300f);
                    if (ImGui.BeginCombo("##EditDesign", string.IsNullOrEmpty(editingAssignmentDesignBuffer) ? "Select Design" : editingAssignmentDesignBuffer))
                    {
                        foreach (var design in currentlySelectedChar.Data.Designs.OrderBy(d => d.Name))
                        {
                            bool isSelected = design.Name == editingAssignmentDesignBuffer;
                            if (ImGui.Selectable(design.Name, isSelected))
                            {
                                editingAssignmentDesignBuffer = design.Name;
                            }
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
            }

            ImGui.Spacing();

            // Save and Cancel buttons
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.8f, 0.3f, 0.6f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.9f, 0.4f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 1.0f, 0.5f, 1.0f));
            
            if (ImGui.Button("Save Changes"))
            {
                pc.AssignedCharacter = currentlySelectedChar;
                Plugin.Configuration.PlayerCharacters[pc.FullName] = pc;
                Plugin.Configuration.Save();
            }
            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.6f, 0.6f, 0.6f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.7f, 0.7f, 0.7f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
            
            if (ImGui.Button("Cancel"))
            {
                
            }
            ImGui.PopStyleColor(3);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        // Job data for UI
        private void DrawGearsetAssignmentSettings()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.4f, 1f));
            ImGui.TextWrapped("This feature will be returning in the near future.");
            ImGui.PopStyleColor();
            // Enable Gearset -> Character switching
            bool enableGcSwitch = Plugin.Configuration.EnableGearsetDesignSwitching;
            if (ImGui.Checkbox("Enable Gearset → Design Assignments", ref enableGcSwitch))
            {
                Plugin.Configuration.EnableGearsetDesignSwitching = enableGcSwitch;
                Plugin.Configuration.Save();
            }
            DrawTooltip("Allow assigning a design to each gearset.\nWhen the gearset is applied, it will automatically switch to that design.\nConfigure assignments using the \'Add Gearset Assignments\' button in the character grid/");
            
            // Enable Gearset -> Character switching
            bool enableCgSwitch = Plugin.Configuration.EnableDesignGearsetSwitching;
            if (ImGui.Checkbox("Enable Design → Gearset Assignments", ref enableCgSwitch))
            {
                Plugin.Configuration.EnableDesignGearsetSwitching = enableCgSwitch;
                Plugin.Configuration.Save();
            }
            DrawTooltip("Allow assigning a gearset to each character/design.\nWhen the design is applied, it will automatically switch to that gearset.\nConfigure assignments in the Add/Edit Character or Design forms.");
        }

        private void DrawFixedSetting(string label, float labelWidth, float inputWidth, Action drawControl)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.SameLine(labelWidth);
            ImGui.SetNextItemWidth(inputWidth);
            drawControl();
            ImGui.Spacing();
        }

        private void UpdateAutomationSettings(bool enableAutomations)
        {
            // bool changed = false;
            //
            // // Character-level Automation Handling
            // foreach (var character in plugin.Characters)
            // {
            //     if (!enableAutomations)
            //     {
            //         character.Data.CharacterAutomation = string.Empty;
            //     }
            //     else if (string.IsNullOrWhiteSpace(character.Data.CharacterAutomation))
            //     {
            //         character.Data.CharacterAutomation = "None";
            //     }
            // }
            //
            // if (changed)
            //     plugin.SaveConfiguration();
        }

        private float GetSafeScale(float baseScale)
        {
            return Math.Clamp(baseScale, 0.3f, 5.0f); // Prevent extreme scaling
        }

        private void DrawBackupSettings()
        {
            // Check for pending import file
            if (pendingImportPath != null)
            {
                string importPath;
                lock (this)
                {
                    importPath = pendingImportPath;
                    pendingImportPath = null;
                }

                if (File.Exists(importPath))
                {
                    Plugin.Log.Info($"[Settings] Processing import file: {importPath}");
                    AddImportedFileToBackups(importPath);
                }
                else
                {
                    lastBackupStatusMessage = "❌ Selected file does not exist";
                    lastBackupStatusTime = DateTime.Now;
                }
            }

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.9f, 1.0f, 1f));
            ImGui.TextWrapped("Create manual backups and restore configurations from backup files.");
            ImGui.PopStyleColor();

            ImGui.Spacing();

            // Current backup status (refresh each time)
            var backupInfo = BackupManager.GetBackupInfo();
            RefreshAvailableBackups(); // Make sure we have current data
            
            ImGui.Text("Backup Status:");
            ImGui.Indent();
            
            if (backupInfo.BackupExists)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.9f, 0.7f, 1f));
                ImGui.Text($"Last automatic backup: {backupInfo.LastBackupDate?.ToString("yyyy-MM-dd HH:mm")}");
                ImGui.Text($"Total backups: {availableBackups.Count}"); // Use the current count
                if (!string.IsNullOrEmpty(backupInfo.LastBackupVersion))
                    ImGui.Text($"Version: {backupInfo.LastBackupVersion}");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0.6f, 1f));
                ImGui.Text($"Total backups: {availableBackups.Count}");
                if (availableBackups.Count == 0)
                    ImGui.Text("No backups found");
                ImGui.PopStyleColor();
            }
            
            ImGui.Unindent();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Manual backup section
            ImGui.Text("Create Manual Backup:");
            ImGui.Spacing();

            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier);
            var contentWidth = ImGui.GetContentRegionAvail().X;
            var labelWidth = 120f * totalScale;
            var inputWidth = contentWidth - labelWidth - (20f * totalScale);

            DrawFixedSetting("Backup Name:", labelWidth, inputWidth * 0.7f, () =>
            {
                if (ImGui.InputTextWithHint("##BackupName", "Optional custom name", ref backupNameBuffer, 50))
                {
                    // Sanitize input
                    backupNameBuffer = string.Join("_", backupNameBuffer.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                }
                DrawTooltip("Optional custom name for the backup. If empty, a timestamp will be used.");
            });

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.8f, 0.5f, 0.7f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.9f, 0.6f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 1.0f, 0.7f, 1.0f));

            if (ImGui.Button("Create Manual Backup", new Vector2(200f * totalScale, 30f * totalScale)))
            {
                CreateManualBackup();
            }
            ImGui.PopStyleColor(3);

            DrawTooltip("Creates a backup of your current configuration in the plugin's backup folder that you can restore later.");

            // Show status message if recent
            if (!string.IsNullOrEmpty(lastBackupStatusMessage) && 
                DateTime.Now - lastBackupStatusTime < TimeSpan.FromSeconds(5))
            {
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.9f, 0.7f, 1f));
                ImGui.Text(lastBackupStatusMessage);
                ImGui.PopStyleColor();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Restore section
            ImGui.Text("Restore Configuration:");
            ImGui.Spacing();

            if (availableBackups.Any())
            {
                ImGui.Text("Available Backups:");
                ImGui.Spacing();

                // Backup list with restore buttons
                if (ImGui.BeginChild("BackupList", new Vector2(0, 120f * totalScale), true))
                {
                    foreach (var backup in availableBackups.Take(10)) // Show only first 10
                    {
                        // Calculate positions for proper alignment
                        var availableWidth = ImGui.GetContentRegionAvail().X;
                        var restoreButtonWidth = 70f * totalScale;
                        var deleteButtonWidth = 60f * totalScale;
                        var buttonSpacing = 5f * totalScale;
                        var totalButtonWidth = restoreButtonWidth + deleteButtonWidth + buttonSpacing;
                        var textWidth = availableWidth - totalButtonWidth - (10f * totalScale); // 10f for spacing
                        
                        // Display backup name with color coding
                        var displayColor = backup.IsManual ? new Vector4(0.8f, 0.9f, 1.0f, 1f) : new Vector4(0.7f, 0.7f, 0.8f, 1f);
                        
                        ImGui.PushStyleColor(ImGuiCol.Text, displayColor);
                        
                        // Truncate text if too long
                        var displayText = backup.GetDisplayName();
                        if (displayText.Length > 45) // Adjust for smaller space due to two buttons
                        {
                            displayText = displayText.Substring(0, 42) + "...";
                        }
                        
                        ImGui.Text(displayText);
                        ImGui.PopStyleColor();

                        // Position buttons on the same line
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (textWidth - ImGui.CalcTextSize(displayText).X));

                        // Restore button
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.6f, 0.3f, 0.7f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.7f, 0.4f, 0.8f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1.0f, 0.8f, 0.5f, 1.0f));

                        if (ImGui.Button($"Restore##{backup.FileName}", new Vector2(restoreButtonWidth, 0)))
                        {
                            RestoreFromBackup(backup.FilePath);
                        }
                        ImGui.PopStyleColor(3);

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip($"Restore configuration from:\n{backup.FileName}\nCreated: {backup.CreatedDate:yyyy-MM-dd HH:mm:ss}");
                        }

                        // Delete button
                        ImGui.SameLine();
                        
                        // Check if Ctrl+Shift is held for delete functionality
                        bool isCtrlShiftHeld = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
                        
                        // Dim the button if Ctrl+Shift is not held
                        if (!isCtrlShiftHeld)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.2f, 0.2f, 0.4f));
                            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.25f, 0.25f, 0.5f));
                            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.3f, 0.3f, 0.6f));
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.3f, 0.3f, 0.7f));
                            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.4f, 0.4f, 0.8f));
                            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
                        }

                        bool deleteClicked = ImGui.Button($"Delete##{backup.FileName}", new Vector2(deleteButtonWidth, 0));
                        
                        if (deleteClicked && isCtrlShiftHeld)
                        {
                            DeleteBackup(backup.FilePath, backup.FileName);
                        }
                        ImGui.PopStyleColor(3);

                        if (ImGui.IsItemHovered())
                        {
                            if (isCtrlShiftHeld)
                            {
                                ImGui.SetTooltip($"Delete backup file:\n{backup.FileName}\nThis action cannot be undone!");
                            }
                            else
                            {
                                ImGui.SetTooltip($"Delete backup file:\n{backup.FileName}\n\nHold Ctrl+Shift and click to delete\n(prevents accidental deletion)");
                            }
                        }
                    }
                }
                ImGui.EndChild();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1f));
                ImGui.Text("No backup files found");
                ImGui.PopStyleColor();
            }

            ImGui.Spacing();

            // Import config file button
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.9f, 0.7f, 0.4f, 0.7f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.8f, 0.5f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1.0f, 0.9f, 0.6f, 1.0f));

            if (ImGui.Button("Add Config File...", new Vector2(200f * totalScale, 30f * totalScale)))
            {
                ImportConfigurationFile();
            }
            ImGui.PopStyleColor(3);

            DrawTooltip("Opens a file browser to select and add a CharacterSelectPlus configuration file to your Available Backups list.");

            ImGui.Spacing();

            // Warning about restore
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0.6f, 1f));
            
            // Warning icon
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text($"{FontAwesomeIcon.ExclamationTriangle.ToIconString()}");
            ImGui.PopFont();
            
            ImGui.SameLine();
            ImGui.TextWrapped("Restoring will overwrite your current configuration. A backup will be created automatically before restoring.");
            ImGui.PopStyleColor();

            ImGui.Spacing();
        }

        private void CreateManualBackup()
        {
            try
            {
                string? customName = string.IsNullOrWhiteSpace(backupNameBuffer) ? null : backupNameBuffer.Trim();
                string? backupPath = BackupManager.CreateManualBackup(Plugin.Configuration, customName);
                
                if (!string.IsNullOrEmpty(backupPath))
                {
                    lastBackupStatusMessage = $"✓ Backup created: {Path.GetFileName(backupPath)}";
                    lastBackupStatusTime = DateTime.Now;
                    backupNameBuffer = ""; // Clear the input
                    RefreshAvailableBackups();
                }
                else
                {
                    lastBackupStatusMessage = "❌ Failed to create backup";
                    lastBackupStatusTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Settings] Error creating manual backup: {ex.Message}");
                lastBackupStatusMessage = "❌ Error creating backup";
                lastBackupStatusTime = DateTime.Now;
            }
        }


        private void ImportConfigurationFile()
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "JSON Configuration Files (*.json)|*.json|All Files (*.*)|*.*";
                        openFileDialog.Title = "Select Configuration File to Import";

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            lock (this)
                            {
                                pendingImportPath = openFileDialog.FileName;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"[Settings] Error in import file dialog thread: {ex.Message}");
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void RestoreFromBackup(string backupPath)
        {
            try
            {
                // Create emergency backup before restoring
                BackupManager.CreateEmergencyBackup(Plugin.Configuration);
                
                var restoredConfig = BackupManager.ImportConfiguration(backupPath);
                if (restoredConfig != null)
                {
                    // Update the plugin interface reference using reflection
                    var pluginInterfaceField = restoredConfig.GetType()
                        .GetField("pluginInterface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    pluginInterfaceField?.SetValue(restoredConfig, Plugin.PluginInterface);
                    
                    // Copy all the important configuration data back to the current config
                    // This preserves the plugin instance while updating the data
                    var currentConfig = Plugin.Configuration;
                    
                    // Copy character data
                    currentConfig.Characters.Clear();
                    currentConfig.Characters.AddRange(restoredConfig.Characters);
                    
                    // Copy all configuration properties using reflection
                    var configType = typeof(Configuration);
                    var properties = configType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .Where(p => p.CanWrite && p.Name != "Characters");
                    
                    foreach (var prop in properties)
                    {
                        try
                        {
                            var value = prop.GetValue(restoredConfig);
                            prop.SetValue(currentConfig, value);
                        }
                        catch (Exception propEx)
                        {
                            Plugin.Log.Warning($"[Settings] Could not restore property {prop.Name}: {propEx.Message}");
                        }
                    }
                    
                    // Save the updated configuration
                    currentConfig.Save();
                    
                    lastBackupStatusMessage = $"✓ Configuration restored from {Path.GetFileName(backupPath)}";
                    lastBackupStatusTime = DateTime.Now;
                    
                    Plugin.Log.Info($"[Settings] Successfully restored configuration from {backupPath}");
                }
                else
                {
                    lastBackupStatusMessage = "❌ Failed to restore configuration";
                    lastBackupStatusTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Settings] Error restoring from backup: {ex.Message}");
                lastBackupStatusMessage = "❌ Error restoring configuration";
                lastBackupStatusTime = DateTime.Now;
            }
        }

        private void RefreshAvailableBackups()
        {
            try
            {
                availableBackups = BackupManager.GetAvailableBackups();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Settings] Error refreshing available backups: {ex.Message}");
                availableBackups.Clear();
            }
        }

        private void AddImportedFileToBackups(string importPath)
        {
            try
            {
                var backupDirectory = Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), "Backups");
                Directory.CreateDirectory(backupDirectory);

                string originalFileName = Path.GetFileName(importPath);
                string destinationPath = Path.Combine(backupDirectory, originalFileName);

                // If file already exists, add timestamp to avoid overwriting
                if (File.Exists(destinationPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                    string extension = Path.GetExtension(originalFileName);
                    originalFileName = $"{nameWithoutExt}_{timestamp}{extension}";
                    destinationPath = Path.Combine(backupDirectory, originalFileName);
                }

                File.Copy(importPath, destinationPath, overwrite: false);

                // Update the file's LastWriteTime to current time so it appears at top of list
                File.SetLastWriteTime(destinationPath, DateTime.Now);

                lastBackupStatusMessage = $"✓ Imported file added to backups: {originalFileName}";
                lastBackupStatusTime = DateTime.Now;
                RefreshAvailableBackups();

                Plugin.Log.Info($"[Settings] Successfully imported file to backups: {destinationPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Settings] Error adding imported file to backups: {ex.Message}");
                lastBackupStatusMessage = "❌ Error importing file to backups";
                lastBackupStatusTime = DateTime.Now;
            }
        }

        private void DeleteBackup(string backupFilePath, string backupFileName)
        {
            try
            {
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                    lastBackupStatusMessage = $"✓ Deleted backup: {backupFileName}";
                    lastBackupStatusTime = DateTime.Now;
                    RefreshAvailableBackups();
                    Plugin.Log.Info($"[Settings] Successfully deleted backup: {backupFilePath}");
                }
                else
                {
                    lastBackupStatusMessage = "❌ Backup file not found";
                    lastBackupStatusTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Settings] Error deleting backup: {ex.Message}");
                lastBackupStatusMessage = "❌ Error deleting backup";
                lastBackupStatusTime = DateTime.Now;
            }
        }

        private void DrawTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        /// <summary>
        /// Expands a specific settings section by name.
        /// Used by feature spotlight cards to navigate directly to relevant settings.
        /// </summary>
        public void ExpandSection(string sectionName)
        {
            // First collapse all sections
            visualSettingsOpen = false;
            automationSettingsOpen = false;
            behaviorSettingsOpen = false;
            mainCharacterSettingsOpen = false;
            dialogueSettingsOpen = false;
            characterAssignmentSettingsOpen = false;
            gearsetAssignmentSettingsOpen = false;
            conflictResolutionSettingsOpen = false;
            backupSettingsOpen = false;

            // Then expand the requested section
            switch (sectionName)
            {
                case "Visual Settings":
                    visualSettingsOpen = true;
                    break;
                case "Glamourer Automations":
                    automationSettingsOpen = true;
                    break;
                case "Behavior Settings":
                    behaviorSettingsOpen = true;
                    break;
                case "Main Character":
                    mainCharacterSettingsOpen = true;
                    break;
                case "Immersive Dialogue":
                    dialogueSettingsOpen = true;
                    break;
                case "Character Assignments":
                    characterAssignmentSettingsOpen = true;
                    break;
                case "Gearset Assignments":
                    gearsetAssignmentSettingsOpen = true;
                    break;
                case "Backup & Restore":
                    backupSettingsOpen = true;
                    break;
                default:
                    Plugin.Log.Warning($"[SettingsPanel] Unknown section name: {sectionName}");
                    return; // Don't set pending if unknown section
            }

            // Set pending section to force ImGui to open it on next draw
            pendingExpandSection = sectionName;
        }

        /// <summary>
        /// Parses a character assignment value into character name and optional design name.
        /// Supports formats: "CharName" (legacy), "Character:CharName", "Design:CharName:DesignName"
        /// </summary>
        private (string CharacterName, string? DesignName) ParseCharacterAssignmentValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "None")
                return (value ?? "", null);

            if (value.StartsWith("Design:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Substring("Design:".Length).Split(':', 2);
                return parts.Length >= 2 ? (parts[0], parts[1]) : (parts[0], null);
            }

            if (value.StartsWith("Character:", StringComparison.OrdinalIgnoreCase))
            {
                return (value.Substring("Character:".Length), null);
            }

            // Legacy format - just the character name
            return (value, null);
        }
    }
}
