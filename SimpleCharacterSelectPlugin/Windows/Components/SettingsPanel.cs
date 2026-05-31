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
        private bool mainCharacterSettingsOpen = false;
        private bool dialogueSettingsOpen = false;
        private bool characterAssignmentSettingsOpen = false;
        private bool gearsetAssignmentSettingsOpen = false;
        private bool conflictResolutionSettingsOpen = false;
        private bool backupSettingsOpen = false;
        private string? pendingExpandSection = null; // Section to force-expand on next draw
        private string backupNameBuffer = "";
        private List<BackupFileInfo> availableBackups = new();
        private string lastBackupStatusMessage = "";

        private DateTime lastBackupStatusTime = DateTime.MinValue;
        private string? pendingImportPath = null;

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

                // Behavior Settings Section
                behaviorSettingsOpen = DrawModernCollapsingHeader("Behavior Settings", new Vector4(1.0f, 0.9f, 0.3f, 1.0f), behaviorSettingsOpen);
                if (behaviorSettingsOpen)
                {
                    DrawBehaviorSettings();
                }

                // Main Character Section
                mainCharacterSettingsOpen = DrawModernCollapsingHeader("Main Character", new Vector4(0.3f, 0.9f, 0.4f, 1.0f), mainCharacterSettingsOpen);
                if (mainCharacterSettingsOpen)
                {
                    DrawMainCharacterSettings(labelWidth, inputWidth);
                }

                // Gearset Assignments
                gearsetAssignmentSettingsOpen = DrawModernCollapsingHeader("Gearset Assignments", new Vector4(0.2f, 0.8f, 0.85f, 1.0f), gearsetAssignmentSettingsOpen);
                if (gearsetAssignmentSettingsOpen)
                {
                    DrawGearsetAssignmentSettings();
                }

                // Backup & Restore
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

        // Job data for UI
        private void DrawGearsetAssignmentSettings()
        {
            // Enable Gearset -> Character switching
            bool enableGcSwitch = Plugin.Configuration.EnableGearsetDesignSwitching;
            if (ImGui.Checkbox("Enable Gearset → Design Assignments", ref enableGcSwitch))
            {
                Plugin.Configuration.EnableGearsetDesignSwitching = enableGcSwitch;
                Plugin.Configuration.Save();
            }
            DrawTooltip("Allow assigning a design to each gearset.\nWhen the gearset is applied, it will automatically switch to that design.\nConfigure assignments using the \'Manage Gearset Assignments\' button at the top left of the character grid.");
            
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
                ImGui.PushTextWrapPos(450f);
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
