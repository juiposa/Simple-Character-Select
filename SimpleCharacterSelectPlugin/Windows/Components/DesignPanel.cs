using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using SimpleCharacterSelectPlugin.Windows.Styles;
using SimpleCharacterSelectPlugin.Windows.Utils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Dalamud.Interface.Textures.TextureWraps;
using SimpleCharacterSelectPlugin;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Windows.Components
{
    public class DesignPanel : IDisposable
    {
        private Plugin plugin;
        private UIStyles uiStyles;

        public bool IsOpen { get; private set; } = false;
        private int activeCharacterIndex = -1;

        // Resizable panel
        public float PanelWidth { get; private set; } = 300f; // Default width
        private const float MinPanelWidth = 250f;
        private const float MaxPanelWidth = 600f;
        private bool isResizing = false;
        private float resizeHandleWidth = 8f;

        // Search functionality
        private bool showSearchBar = false;
        private string searchQuery = "";
        private string selectedTag = "All";
        private bool showTagFilter = false;
        
        // Search cache for performance
        private List<CharacterDesign> cachedFilteredDesigns = new();
        private bool filterCacheDirty = true;
        private string lastSearchQuery = "";
        private string lastSelectedTag = "All";
        private int lastDesignCount = -1;
        
        // Design editing state
        private bool isEditDesignWindowOpen = false;
        private bool isAdvancedModeDesign = false;
        private bool isAdvancedModeWindowOpen = false;
        private bool isNewDesign = false;
        private bool isSecretDesignMode = false;

        // Edit fields
        private string editedDesignName = "";
        private string editedDesignMacro = "";
        private string editedGlamourerDesign = "";
        private string editedAutomation = "";
        private string editedCustomizeProfile = "";
        private int? editedGearset = null;
        private string editedDesignPreviewPath = "";
        private string advancedDesignMacroText = "";
        private string originalAdvancedMacroText = "";
        private string originalDesignName = "";
        private string? pendingDesignImagePath = null;
        private string? pendingPastedImagePath = null;
        
        // Temporary Secret Mode state for new designs
        private Dictionary<string, bool>? temporaryDesignSecretModState = null;
        private HashSet<string>? temporaryDesignSecretModPinOverrides = null;

        // Design sorting
        private enum DesignSortType { Favorites, Alphabetical, Recent, Oldest, Manual }
        private DesignSortType currentDesignSort => GetDesignSortFromConfig();

        // Folder management
        private string newFolderName = "";
        private bool isRenamingFolder = false;
        private Guid renameFolderId;
        private string renameFolderBuf = "";
        private DesignFolder? draggedFolder = null;
        private CharacterDesign? draggedDesign = null;
        private Vector3? newFolderSelectedColor = null;

        // Import window
        private bool isImportWindowOpen = false;
        private Character? targetForDesignImport = null;

        // Snapshot dialog
        private bool isSnapshotDialogOpen = false;
        private string snapshotDesignName = "";
        private bool snapshotUseConflictResolution = true;
        private Character? snapshotTargetCharacter = null;
        private HashSet<string> snapshotDetectedMods = new();
        private string? snapshotDetectedCustomizePlusProfile = null;
        private bool snapshotHasClipboardImage = false;
        private bool snapshotIsProcessing = false;
        private string snapshotStatusMessage = "";

        public DesignPanel(Plugin plugin, UIStyles uiStyles)
        {
            this.plugin = plugin;
            this.uiStyles = uiStyles;

            // Load saved panel width or use default
            PanelWidth = plugin.Configuration.DesignPanelWidth;
        }

        public void Dispose()
        {
            // Save panel width on dispose
            plugin.Configuration.DesignPanelWidth = PanelWidth;
            plugin.Configuration.Save();
        }

        public void Draw()
        {
            if (!IsOpen) return;

            // Calculate responsive sizing
            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier);

            // Scale the panel dimensions
            float scaledPanelWidth = PanelWidth * GetSafeScale(totalScale);
            float scaledMinWidth = MinPanelWidth * totalScale;
            float scaledMaxWidth = MaxPanelWidth * totalScale;
            float scaledHandleWidth = resizeHandleWidth * totalScale;

            DrawDesignPanelContent(totalScale, scaledPanelWidth);
            DrawResizeHandle(totalScale, scaledPanelWidth, scaledMinWidth, scaledMaxWidth, scaledHandleWidth);

            DrawImportWindow(totalScale);
            //DrawSnapshotDialog(totalScale);
        }

        private void DrawResizeHandle(float totalScale, float scaledPanelWidth, float scaledMinWidth, float scaledMaxWidth, float scaledHandleWidth)
        {
            // Current window position and size
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();

            // Position handle at the very left edge of the design panel window
            var handleMin = new Vector2(windowPos.X, windowPos.Y);
            var handleMax = new Vector2(windowPos.X + scaledHandleWidth, windowPos.Y + windowSize.Y);

            // Check if mouse is over the handle area
            bool hovered = ImGui.IsMouseHoveringRect(handleMin, handleMax);

            // Capture mouse input when over resize handle to prevent window dragging
            if (hovered || isResizing)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

                if (hovered && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Left)))
                {
                    ImGui.SetItemAllowOverlap();

                    // Create an invisible button over the resize area to capture input
                    ImGui.SetCursorScreenPos(handleMin);
                    ImGui.InvisibleButton("##resize_handle", new Vector2(scaledHandleWidth, windowSize.Y));

                    if (ImGui.IsItemActive() || isResizing)
                    {
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            isResizing = true;
                        }
                    }
                }
            }

            // Handle resizing
            if (isResizing)
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    // Current mouse position
                    float currentMouseX = ImGui.GetMousePos().X;
                    // Calculate new width based on mouse position relative to the window's right edge
                    float windowRightEdge = ImGui.GetWindowPos().X + ImGui.GetWindowSize().X;
                    float newScaledWidth = windowRightEdge - currentMouseX;
                    // Convert to base units and clamp
                    float newWidth = newScaledWidth / totalScale;
                    PanelWidth = Math.Clamp(newWidth, MinPanelWidth, MaxPanelWidth);
                    // Save the new width immediately for responsiveness
                    plugin.Configuration.DesignPanelWidth = PanelWidth;
                    // Force main window to recalculate layout
                    if (plugin.MainWindow != null)
                    {
                        plugin.MainWindow.InvalidateLayout();
                    }
                }
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    isResizing = false;
                    // Save configuration
                    plugin.Configuration.Save();
                }
            }

            // Draw visual resize handle
            var drawList = ImGui.GetWindowDrawList();
            uint handleColor = hovered || isResizing
                ? ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.8f, 0.8f))
                : ImGui.GetColorU32(new Vector4(0.4f, 0.4f, 0.6f, 0.3f));

            // Subtle line at left edge
            drawList.AddLine(
                new Vector2(handleMin.X + 2 * totalScale, handleMin.Y + 10 * totalScale),
                new Vector2(handleMin.X + 2 * totalScale, handleMax.Y - 10 * totalScale),
                handleColor,
                2f * totalScale
            );

            // Draw resize grip dots when hovered
            if (hovered || isResizing)
            {
                float dotSize = 2f * totalScale;
                float dotSpacing = 6f * totalScale;
                var centerX = handleMin.X + scaledHandleWidth / 2;
                var centerY = handleMin.Y + windowSize.Y / 2;
                for (int i = -2; i <= 2; i++)
                {
                    drawList.AddCircleFilled(
                        new Vector2(centerX, centerY + i * dotSpacing),
                        dotSize,
                        handleColor
                    );
                }
            }
        }
        private float GetSafeScale(float baseScale)
        {
            return Math.Clamp(baseScale, 0.3f, 5.0f);
        }

        public void Open(int characterIndex)
        {
            activeCharacterIndex = characterIndex;
            IsOpen = true;
            plugin.WindowState.IsDesignPanelOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
            activeCharacterIndex = -1;
            plugin.WindowState.IsDesignPanelOpen = false;
            
            CloseDesignEditor();
        }

        private void DrawDesignPanelContent(float totalScale, float scaledPanelWidth)
        {
            if (activeCharacterIndex < 0 || activeCharacterIndex >= plugin.Characters.Count)
                return;

            var character = plugin.Characters[activeCharacterIndex];

            ApplyScaledStyles(totalScale);

            try
            {
                DrawHeader(character, totalScale);

                if (isEditDesignWindowOpen)
                {
                    DrawDesignForm(character, totalScale);
                    ImGui.Separator();
                }

                DrawSortingControls(character, totalScale);
                ImGui.Separator();

                DrawDesignList(character, totalScale);
            }
            finally
            {
                PopScaledStyles();
            }
        }

        private void ApplyScaledStyles(float scale)
        {
            // Check for custom Design Panel background colour
            var designPanelBg = new Vector4(0.08f, 0.08f, 0.1f, 0.98f);
            var designPanelChildBg = new Vector4(0.1f, 0.1f, 0.12f, 0.95f);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, designPanelBg);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, designPanelChildBg);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.95f, 0.95f, 0.95f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.16f, 0.16f, 0.2f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.22f, 0.22f, 0.28f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.28f, 0.28f, 0.35f, 1.0f));

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8 * scale, 5 * scale));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6 * scale, 3 * scale));
        }

        private void PopScaledStyles()
        {
            ImGui.PopStyleVar(4);
            ImGui.PopStyleColor(6);
        }

        private void DrawHeader(Character character, float scale)
        {
            float buttonSize = 25f * scale;
            float spacing = 2f * scale;

            
            ImGui.BeginGroup();

            // Add and Folder buttons
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.27f, 1.07f, 0.27f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.35f, 0.35f, 1f));

            if (ImGui.Button("+##AddDesign", new Vector2(buttonSize, buttonSize)))
            {
                var io = ImGui.GetIO();
                bool ctrlHeld = io.KeyCtrl;
                bool shiftHeld = io.KeyShift;
                
                AddNewDesign();
                editedDesignMacro = GenerateDesignMacro(character);
                if (isAdvancedModeDesign)
                    advancedDesignMacroText = editedDesignMacro;
            }

            plugin.WindowState.DesignPanelAddButtonPos = ImGui.GetItemRectMin();
            plugin.WindowState.DesignPanelAddButtonSize = ImGui.GetItemRectSize();

            ImGui.PopStyleColor(4);

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Click to add a new design\nHold Shift to import from another character");
                ImGui.EndTooltip();
            }

            ImGui.SameLine(0, spacing);

            // Folder Button
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.3f, 1.0f)); // Yellow
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.35f, 0.35f, 1f));

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button("\uf07b##AddFolder"))
                ImGui.OpenPopup("CreateFolderPopup");
            ImGui.PopFont();

            ImGui.PopStyleColor(4);

            DrawFolderCreationPopup(character, scale);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Add Folder");
            }

            // Search button
            ImGui.SameLine(0, spacing);
            if (uiStyles.IconButton("\uf002", "Search designs"))
            {
                showSearchBar = !showSearchBar;
                if (!showSearchBar)
                {
                    searchQuery = "";
                    InvalidateFilterCache();
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Search designs");

            // // Snapshot button TODO probably removing
            // ImGui.SameLine();
            // float availableWidth = ImGui.GetContentRegionAvail().X;
            // ImGui.SetCursorPosX(ImGui.GetCursorPosX() + availableWidth - (buttonSize * 2) - (5 * scale));
            //
            // ImGui.PushFont(UiBuilder.IconFont);
            // ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));        // Dark gray
            // ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.4f, 0.4f, 0.9f)); // Medium gray  
            // ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));  // Light gray
            // ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));          // White text
            // ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));        // Center icon
            //
            // if (ImGui.Button($"\uf030##CreateSnapshot"))
            // {
            //     if (activeCharacterIndex >= 0 && activeCharacterIndex < plugin.Characters.Count)
            //     {
            //         var io = ImGui.GetIO();
            //         var selectedCharacter = plugin.Characters[activeCharacterIndex];
            //         
            //         if (io.KeyCtrl && io.KeyShift)
            //         {
            //             // Ctrl+Shift: Smart snapshot with CR
            //             CreateSmartSnapshot(selectedCharacter, useConflictResolution: true);
            //         }
            //         else
            //         {
            //             // Regular click: Smart snapshot without CR
            //             CreateSmartSnapshot(selectedCharacter, useConflictResolution: false);
            //         }
            //     }
            // }
            //
            // ImGui.PopStyleVar(1);
            // ImGui.PopStyleColor(4);
            // ImGui.PopFont();
            //
            // if (ImGui.IsItemHovered())
            // {
            //     string tooltip = "Create Design from Current Look\n• Click: Smart snapshot";
            //     ImGui.SetTooltip(tooltip);
            // }

            // Close button
            ImGui.SameLine(0, spacing);

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.27f, 0.27f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.2f, 0.2f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.3f, 0.3f, 1f));

            if (ImGui.Button("×##CloseDesignPanel"))
            {
                Close();
            }

            ImGui.PopStyleColor(4);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Close Design Panel");
            }

            ImGui.EndGroup();

            ImGui.Spacing();

            // Character name
            string name = $"Designs for {character.Data.Name}";
            ImGui.TextUnformatted(name);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(name);

            ImGui.Spacing();
        }

        private void DrawFolderCreationPopup(Character character, float scale)
        {
            if (ImGui.BeginPopup("CreateFolderPopup"))
            {
                ImGui.Text("New Folder Name:");
                ImGui.SetNextItemWidth(200 * scale);
                ImGui.InputText("##NewFolder", ref newFolderName, 100);

                ImGui.Spacing();
                ImGui.Text("Folder Color:");

                // Colour selection
                var quickColors = new[]
                {
                    (Vector3?)null, // Auto
                    new Vector3(0.8f, 0.2f, 0.2f), // Red
                    new Vector3(0.3f, 0.8f, 0.3f), // Green
                    new Vector3(0.3f, 0.5f, 0.9f), // Blue
                    new Vector3(0.7f, 0.3f, 0.9f)  // Purple
                };

                float colorButtonSize = 30f * scale;
                for (int i = 0; i < quickColors.Length; i++)
                {
                    var color = quickColors[i];
                    bool isSelected = (newFolderSelectedColor == null && color == null) ||
                                     (newFolderSelectedColor != null && color != null &&
                                      Vector3.Distance(newFolderSelectedColor.Value, color.Value) < 0.1f);

                    if (i > 0) ImGui.SameLine();

                    Vector4 buttonColor = color.HasValue
                        ? new Vector4(color.Value.X, color.Value.Y, color.Value.Z, 1.0f)
                        : new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

                    ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(buttonColor.X * 1.2f, buttonColor.Y * 1.2f, buttonColor.Z * 1.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(buttonColor.X * 0.8f, buttonColor.Y * 0.8f, buttonColor.Z * 0.8f, 1.0f));

                    if (isSelected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 1, 1, 1));
                        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 3f * scale);
                    }

                    if (ImGui.Button($"##Color{i}", new Vector2(colorButtonSize, colorButtonSize)))
                    {
                        newFolderSelectedColor = color;
                    }

                    if (isSelected)
                    {
                        ImGui.PopStyleVar();
                        ImGui.PopStyleColor();
                    }

                    ImGui.PopStyleColor(3);
                }

                ImGui.Separator();

                float buttonWidth = 60f * scale;
                if (ImGui.Button("Create", new Vector2(buttonWidth, 0)))
                {
                    var folder = new DesignFolder(newFolderName, Guid.NewGuid())
                    {
                        ParentFolderId = null,
                        SortOrder = character.Data.DesignFolders.Count,
                        CustomColor = newFolderSelectedColor
                    };
                    character.Data.DesignFolders.Add(folder);
                    plugin.SaveConfiguration();
                    newFolderName = "";
                    newFolderSelectedColor = null;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
                {
                    newFolderName = "";
                    newFolderSelectedColor = null;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        private void DrawDesignForm(Character character, float scale)
        {
            float formHeight = 320f * scale;
            ImGui.BeginChild("EditDesignForm", new Vector2(0, formHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize);

            bool isNewDesignForm = string.IsNullOrEmpty(editedDesignName);
            ImGui.Text(isNewDesignForm ? "Add Design" : "Edit Design");

            float inputWidth = Math.Max(150f * scale, ImGui.GetContentRegionAvail().X - (50f * scale));

            // Design Name
            ImGui.Text("Design Name*");
            ImGui.SetCursorPosX(10 * scale);
            ImGui.SetNextItemWidth(inputWidth);
            if (ImGui.InputText("##DesignName", ref editedDesignName, 100))
            {
                plugin.WindowState.EditedDesignName = editedDesignName;
            }
            plugin.WindowState.DesignNameFieldPos = ImGui.GetItemRectMin();
            plugin.WindowState.DesignNameFieldSize = ImGui.GetItemRectSize();

            ImGui.Separator();

            DrawGlamourerField(character, inputWidth, scale);

            if (plugin.Configuration.EnableAutomations)
            {
                DrawAutomationField(inputWidth, scale);
            }

            DrawCustomizeField(inputWidth, scale);

            if (plugin.Configuration.EnableGearsetAssignments)
            {
                DrawGearsetField(inputWidth, scale);
            }

            DrawPreviewImageField(scale);

            ImGui.Separator();

            DrawFormActionButtons(character, scale);

            ImGui.EndChild();
        }

        private void DrawGlamourerField(Character character, float inputWidth, float scale)
        {
            ImGui.Text("Glamourer Design*");

            // Tooltip
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("Select the Glamourer design for this outfit. Right-click to clear.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.SetCursorPosX(10 * scale);
            var glamourerOptions = plugin.IntegrationListProvider?.GetGlamourerDesigns() ?? Array.Empty<string>();

            if (AutocompleteCombo.Draw("##GlamourerDesign", ref editedGlamourerDesign, glamourerOptions, inputWidth, "Select design..."))
            {
                plugin.WindowState.EditedGlamourerDesign = editedGlamourerDesign;
                editedDesignMacro = GenerateDesignMacro(character);
            }
            plugin.WindowState.DesignGlamourerFieldPos = ImGui.GetItemRectMin();
            plugin.WindowState.DesignGlamourerFieldSize = ImGui.GetItemRectSize();
        }

        private void DrawAutomationField(float inputWidth, float scale)
        {
            ImGui.Text("Glamourer Automation");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("Optional: Enter the name of a Glamourer automation for this design.\n⚠️ Must match the automation name EXACTLY as shown in Glamourer.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.SetCursorPosX(10 * scale);
            // Glamourer doesn't expose an IPC to get automation names, so use plain text input
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.InputText("##GlamourerAutomation", ref editedAutomation, 100);
        }

        private void DrawCustomizeField(float inputWidth, float scale)
        {
            ImGui.Text("Customize+ Profile");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("Optional: Select a Customize+ profile for this design. Right-click to clear.\nIf left blank, uses the character's profile or disables all profiles.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.SetCursorPosX(10 * scale);
            var customizeOptions = plugin.IntegrationListProvider?.GetCustomizePlusProfiles() ?? Array.Empty<string>();
            var currentCustomize = plugin.IntegrationListProvider?.GetCurrentCustomizePlusProfile();

            if (AutocompleteCombo.Draw("##CustomizePlus", ref editedCustomizeProfile, customizeOptions, inputWidth, "Select profile...", currentActive: currentCustomize))
            {
                editedDesignMacro = GenerateDesignMacro(plugin.Characters[activeCharacterIndex]);
            }
        }

        private void DrawGearsetField(float inputWidth, float scale)
        {
            ImGui.Text("Assigned Gearset");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("Optional: Automatically switch to this gearset when applying this design.\nChoose 'None' to use the character's setting or not change gearsets.\nDesign setting overrides character setting.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.SetCursorPosX(10 * scale);
            ImGui.SetNextItemWidth(inputWidth);

            // Get available gearsets
            //var gearsets = plugin.GetPlayerGearsets();

            // Build display text for current selection
            string currentDisplay = "None (use character setting)";
            if (editedGearset.HasValue)
            {
                //var matchingGearset = gearsets.FirstOrDefault(g => g.Number == editedGearset.Value);
                // if (matchingGearset.Number > 0)
                // {
                //     //currentDisplay = plugin.GetGearsetDisplayName(matchingGearset.Number, matchingGearset.JobId, matchingGearset.Name);
                // }
                // else
                // {
                //     currentDisplay = $"Gearset {editedGearset.Value}";
                // }
            }

            if (ImGui.BeginCombo("##AssignedGearset", currentDisplay))
            {
                // "None" option
                if (ImGui.Selectable("None (use character setting)", !editedGearset.HasValue))
                {
                    editedGearset = null;
                }
                if (!editedGearset.HasValue)
                    ImGui.SetItemDefaultFocus();

                // Gearset options
                // foreach (var gearset in gearsets)
                // {
                //     string displayName = plugin.GetGearsetDisplayName(gearset.Number, gearset.JobId, gearset.Name);
                //     bool isSelected = editedGearset.HasValue && editedGearset.Value == gearset.Number;
                //
                //     if (ImGui.Selectable(displayName, isSelected))
                //     {
                //         editedGearset = gearset.Number;
                //     }
                //     if (isSelected)
                //         ImGui.SetItemDefaultFocus();
                // }

                ImGui.EndCombo();
            }
        }

        private void DrawPreviewImageField(float scale)
        {
            ImGui.Text("Preview Image (Optional)");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("Optional: Choose an image to show when hovering over this design.\nThis helps you quickly identify designs at a glance.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.SetCursorPosX(10 * scale);
            if (ImGui.Button("Browse..."))
            {
                SelectPreviewImage();
            }

            // Add Paste button
            ImGui.SameLine();
            bool clipboardHasImage = IsClipboardImageAvailable();
            
            if (!clipboardHasImage)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            }
            
            if (ImGui.Button("Paste"))
            {
                if (clipboardHasImage)
                {
                    PasteImageFromClipboard();
                }
            }
            
            if (!clipboardHasImage)
            {
                ImGui.PopStyleVar();
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (clipboardHasImage)
                {
                    ImGui.Text("Paste image from clipboard");
                }
                else
                {
                    ImGui.Text("No image in clipboard\nCopy a screenshot first (Win+Shift+S)");
                }
                ImGui.EndTooltip();
            }

            // Add Clear button
            ImGui.SameLine();
            if (ImGui.Button("Clear") && !string.IsNullOrEmpty(editedDesignPreviewPath))
            {
                editedDesignPreviewPath = "";
            }

            // Apply pending image path from file picker
            if (pendingDesignImagePath != null)
            {
                lock (this)
                {
                    editedDesignPreviewPath = pendingDesignImagePath;
                    pendingDesignImagePath = null;
                }
            }

            // Apply pending pasted image path
            if (pendingPastedImagePath != null)
            {
                lock (this)
                {
                    editedDesignPreviewPath = pendingPastedImagePath;
                    pendingPastedImagePath = null;
                }
            }

            // Show current preview
            if (!string.IsNullOrEmpty(editedDesignPreviewPath) && File.Exists(editedDesignPreviewPath))
            {
                var texture = Plugin.TextureProvider.GetFromFile(editedDesignPreviewPath).GetWrapOrDefault();
                if (texture != null)
                {
                    float maxSize = 100f * scale;
                    var (width, height) = CalculateImageDimensions(texture, maxSize);
                    ImGui.Image((ImTextureID)texture.Handle, new Vector2(width, height));
                }
            }
            else if (!string.IsNullOrEmpty(editedDesignPreviewPath))
            {
                ImGui.Text("Preview: " + Path.GetFileName(editedDesignPreviewPath));
            }
        }

        private void DrawFormActionButtons(Character character, float scale)
        {
            float buttonWidth = 85 * scale;
            float buttonHeight = 20 * scale;
            float buttonSpacing = 8 * scale;
            float totalButtonWidth = (buttonWidth * 2 + buttonSpacing);
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float buttonPosX = (availableWidth > totalButtonWidth) ? (availableWidth - totalButtonWidth) / 2f : 0;

            ImGui.SetCursorPosX(buttonPosX);

            bool canSave = !string.IsNullOrWhiteSpace(editedDesignName) && !string.IsNullOrWhiteSpace(editedGlamourerDesign);

            // Center text in buttons
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4 * scale, 4 * scale));
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));

            // Save button styling
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.3f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.6f, 0.4f, 1.0f));

            if (!canSave)
                ImGui.BeginDisabled();

            if (ImGui.Button("Save Design", new Vector2(buttonWidth, 0)))
            {
                SaveDesign(character);
                CloseDesignEditor();
            }
            plugin.WindowState.SaveDesignButtonPos = ImGui.GetItemRectMin();
            plugin.WindowState.SaveDesignButtonSize = ImGui.GetItemRectSize();

            if (!canSave)
                ImGui.EndDisabled();

            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            // Cancel button styling
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.2f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.3f, 0.3f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.4f, 0.4f, 1.0f));

            if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
            {
                CloseDesignEditor();
            }

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(2);
        }

        private void DrawSortingControls(Character character, float scale)
        {
            ImGui.Text("Sort Designs By:");
            ImGui.SameLine();

            float comboWidth = Math.Max(120f * scale, ImGui.GetContentRegionAvail().X - (20f * scale));
            ImGui.SetNextItemWidth(comboWidth);

            if (ImGui.BeginCombo("##DesignSortDropdown", currentDesignSort.ToString()))
            {
                if (ImGui.Selectable("Favourites", currentDesignSort == DesignSortType.Favorites))
                {
                    SetDesignSort(0); // Favorites
                    SortDesigns(character);
                }
                if (ImGui.Selectable("Alphabetical", currentDesignSort == DesignSortType.Alphabetical))
                {
                    SetDesignSort(1); // Alphabetical
                    SortDesigns(character);
                }
                if (ImGui.Selectable("Newest", currentDesignSort == DesignSortType.Recent))
                {
                    SetDesignSort(2); // Recent
                    SortDesigns(character);
                }
                if (ImGui.Selectable("Oldest", currentDesignSort == DesignSortType.Oldest))
                {
                    SetDesignSort(3); // Oldest
                    SortDesigns(character);
                }
                if (ImGui.Selectable("Manual", currentDesignSort == DesignSortType.Manual))
                {
                    SetDesignSort(4); // Manual
                }
                ImGui.EndCombo();
            }
            
            // Search input field
            if (showSearchBar)
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputTextWithHint("##SearchDesigns", "Search designs...", ref searchQuery, 100))
                {
                    InvalidateFilterCache();
                }
            }
        }
        
        private void InvalidateFilterCache()
        {
            filterCacheDirty = true;
        }

        private void DrawDesignList(Character character, float scale)
        {
            float remainingHeight = ImGui.GetContentRegionAvail().Y;

            // Minimum height
            remainingHeight = Math.Max(remainingHeight, 100f * scale);

            ImGui.BeginChild("DesignListBackground", new Vector2(0, remainingHeight), true, ImGuiWindowFlags.AlwaysVerticalScrollbar);

            // Build unified list of folders and designs
            var renderItems = BuildRenderItems(character);

            // Render each item
            bool anyRowHovered = false;
            bool anyHeaderHovered = false;

            foreach (var entry in renderItems)
            {
                if (entry.isFolder)
                {
                    var folder = (DesignFolder)entry.item;
                    bool folderWasHovered = false;
                    DrawFolderItem(character, folder, ref folderWasHovered, scale);
                    if (folderWasHovered) anyHeaderHovered = true;
                }
                else
                {
                    var design = (CharacterDesign)entry.item;
                    DrawDesignRow(character, design, false, scale);
                    if (ImGui.IsItemHovered()) anyRowHovered = true;
                }
            }

            // Handle dropping outside any header
            HandleDropToRoot(anyHeaderHovered, anyRowHovered, character);

            ImGui.EndChild();
        }

        private void DrawFolderItem(Character character, DesignFolder folder, ref bool wasHovered, float scale)
        {
            bool isRenaming = isRenamingFolder && folder.Id == renameFolderId;
            bool open = false;

            // Get folder colour
            var folderColor = GetFolderColor(character, folder);

            if (isRenaming)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1f));
                ImGui.SetNextItemWidth(200 * scale);
                if (ImGui.InputText("##InlineRename", ref renameFolderBuf, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    folder.Name = renameFolderBuf;
                    isRenamingFolder = false;
                    plugin.SaveConfiguration();
                }
                ImGui.PopStyleColor();
            }
            else
            {
                // Style the folder header with custom colour
                ImGui.PushStyleColor(ImGuiCol.Header, folderColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(folderColor.X * 1.2f, folderColor.Y * 1.2f, folderColor.Z * 1.2f, folderColor.W));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(folderColor.X * 1.4f, folderColor.Y * 1.4f, folderColor.Z * 1.4f, folderColor.W));

                open = ImGui.CollapsingHeader($"{folder.Name}##F{folder.Id}", ImGuiTreeNodeFlags.SpanFullWidth);

                ImGui.PopStyleColor(3);

                // Drag source
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceAllowNullId))
                {
                    draggedFolder = folder;
                    ImGui.SetDragDropPayload("FOLDER_MOVE", ReadOnlySpan<byte>.Empty, ImGuiCond.None);
                    ImGui.TextUnformatted($"Moving Folder: {folder.Name}");
                    ImGui.EndDragDropSource();
                }

                // Context menu
                DrawFolderContextMenu(character, folder, scale);
            }

            // Handle hover and drop logic
            var hdrMin = ImGui.GetItemRectMin();
            var hdrMax = ImGui.GetItemRectMax();
            bool overHeader = ImGui.IsMouseHoveringRect(hdrMin, hdrMax, true);
            wasHovered = overHeader;

            if ((draggedDesign != null || draggedFolder != null) && overHeader)
            {
                var dl = ImGui.GetWindowDrawList();
                uint col = ImGui.GetColorU32(new Vector4(0.3f, 0.5f, 1f, 1f));
                dl.AddRect(hdrMin, hdrMax, col, 0, ImDrawFlags.None, 2 * scale);
            }

            // Drop handling
            if (overHeader && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                if (draggedDesign != null)
                {
                    draggedDesign.FolderId = folder.Id;
                    plugin.SaveConfiguration();
                    draggedDesign = null;
                }
                else if (draggedFolder != null && draggedFolder != folder)
                {
                    draggedFolder.ParentFolderId = folder.Id;
                    plugin.SaveConfiguration();
                    draggedFolder = null;
                }
            }

            // Draw folder content
            if (open)
            {
                DrawFolderContents(character, folder, scale);
            }
        }

        private void DrawFolderContextMenu(Character character, DesignFolder folder, float scale)
        {
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup($"FolderCtx{folder.Id}");

            if (ImGui.BeginPopup($"FolderCtx{folder.Id}"))
            {
                if (ImGui.MenuItem("Rename Folder"))
                {
                    renameFolderId = folder.Id;
                    renameFolderBuf = folder.Name;
                    isRenamingFolder = true;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.Separator();

                // Folder colour menu
                if (ImGui.BeginMenu("Folder Colour"))
                {
                    // Auto colour option
                    if (ImGui.MenuItem("Auto Colour", "", folder.CustomColor == null))
                    {
                        folder.CustomColor = null;
                        plugin.SaveConfiguration();
                    }

                    ImGui.Separator();

                    // Preset colours
                    var presetColors = new[]
                    {
                        ("Red", new Vector3(0.8f, 0.2f, 0.2f)),
                        ("Green", new Vector3(0.3f, 0.8f, 0.3f)),
                        ("Blue", new Vector3(0.3f, 0.5f, 0.9f)),
                        ("Yellow", new Vector3(0.9f, 0.8f, 0.2f)),
                        ("Purple", new Vector3(0.7f, 0.3f, 0.9f)),
                        ("Orange", new Vector3(1.0f, 0.6f, 0.2f)),
                        ("Pink", new Vector3(0.9f, 0.4f, 0.7f)),
                        ("Cyan", new Vector3(0.3f, 0.8f, 0.8f))
                    };

                    foreach (var (colorName, color) in presetColors)
                    {
                        bool isSelected = folder.CustomColor.HasValue &&
                            Vector3.Distance(folder.CustomColor.Value, color) < 0.1f;

                        if (ImGui.MenuItem(colorName, "", isSelected))
                        {
                            folder.CustomColor = color;
                            plugin.SaveConfiguration();
                        }
                    }

                    ImGui.Separator();

                    // Custom colour picker
                    ImGui.Text("Custom Colour:");
                    Vector3 tempColor = folder.CustomColor ?? GetAutoGeneratedColor(character, folder);

                    if (ImGui.ColorEdit3("##CustomFolderColour", ref tempColor, ImGuiColorEditFlags.NoInputs))
                    {
                        folder.CustomColor = tempColor;
                        plugin.SaveConfiguration();
                    }

                    ImGui.EndMenu();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Delete Folder"))
                {
                    DeleteFolder(character, folder);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void DrawFolderContents(Character character, DesignFolder folder, float scale)
        {
            float indentAmount = 15f * scale;

            // Apply search filter
            var foldersToShow = character.Data.DesignFolders
                     .Where(f => f.ParentFolderId == folder.Id);
            var designsToShow = character.Data.Designs
                     .Where(d => d.FolderId == folder.Id);
                     
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                foldersToShow = foldersToShow.Where(f => FolderContainsMatchingDesigns(character, f));
                designsToShow = designsToShow.Where(d => MatchesSearchQuery(d));
            }

            // Child folders
            foreach (var child in foldersToShow.OrderBy(f => f.SortOrder))
            {
                ImGui.Indent(indentAmount);
                bool childWasHovered = false;
                DrawFolderItem(character, child, ref childWasHovered, scale);
                ImGui.Unindent(indentAmount);
            }

            foreach (var design in designsToShow.OrderBy(d => d.SortOrder))
            {
                ImGui.Indent(indentAmount);
                DrawDesignRow(character, design, true, scale);
                ImGui.Unindent(indentAmount);
            }

            // Visual separation
            ImGui.Spacing();
            ImGui.Separator();
        }

        private void DrawDesignRow(Character character, CharacterDesign design, bool isInsideFolder, float scale)
        {
            ImGui.PushID(design.Name);

            var rowMin = ImGui.GetCursorScreenPos();
            float rowW = ImGui.GetContentRegionAvail().X;
            float rowH = 32f * scale;
            ImGui.Dummy(new Vector2(rowW, rowH));
            var rowMax = rowMin + new Vector2(rowW, rowH);

            bool hovered = ImGui.IsMouseHoveringRect(rowMin, rowMax, true);

            // Dark row background
            if (hovered)
            {
                var hoverColor = ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.25f, 0.8f));
                ImGui.GetWindowDrawList().AddRectFilled(rowMin, rowMax, hoverColor, 4f * scale);
            }

            // Draw design row content with compact styling, america's next top model has nothing on me now!
            DrawDesignRowContent(character, design, rowMin, rowMax, rowH, hovered, rowW, scale);

            // Handle drag and drop
            HandleDesignDragDrop(character, design, rowMin, rowMax, hovered, scale);

            ImGui.PopID();
            ImGui.SetCursorScreenPos(new Vector2(rowMin.X, rowMin.Y + rowH));

            // Subtle separator
            if (!isInsideFolder)
            {
                var separatorColor = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
                ImGui.GetWindowDrawList().AddLine(
                    new Vector2(rowMin.X + (10 * scale), rowMax.Y),
                    new Vector2(rowMax.X - (10 * scale), rowMax.Y),
                    separatorColor, 1f * scale
                );
            }
        }

        private void DrawDesignRowContent(Character character, CharacterDesign design, Vector2 rowMin, Vector2 rowMax, float rowH, bool hovered, float rowW, float scale)
        {
            float pad = 8f * scale;
            float spacing = 4f * scale;
            float btnSize = 24f * scale;
            float x = rowMin.X + (2f * scale);

            // Drag handle
            if (hovered)
            {
                float handleWidth = 12f * scale;
                float handleHeight = rowH * 0.6f;
                float yOff = (rowH - handleHeight) / 2;

                ImGui.SetCursorScreenPos(new Vector2(x + pad, rowMin.Y + yOff));

                var handleColor = new Vector4(character.Data.NameplateColor.X, character.Data.NameplateColor.Y, character.Data.NameplateColor.Z, 0.8f);

                ImGui.PushStyleColor(ImGuiCol.Button, handleColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, handleColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, handleColor);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f * scale);

                ImGui.Button($"##handle_{design.Name}", new Vector2(handleWidth, handleHeight));

                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);

                // Enable drag and drop
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left) &&
                    ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceAllowNullId))
                {
                    draggedDesign = design;
                    ImGui.SetDragDropPayload("DESIGN_MOVE", ReadOnlySpan<byte>.Empty, ImGuiCond.None);

                    // Ghost image
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.1f, 0.9f));
                    ImGui.BeginGroup();
                    ImGui.Text("📄");
                    ImGui.SameLine();
                    ImGui.Text(design.Name);
                    ImGui.EndGroup();
                    ImGui.PopStyleColor(2);
                    ImGui.EndDragDropSource();
                }

                x += handleWidth + spacing;
            }

            // Favourite star/ghost
            ImGui.SetCursorScreenPos(new Vector2(x, rowMin.Y + (rowH - btnSize) / 2));

            string star;
            star = design.IsFavorite ? "★" : "☆"; // Normal stars
                
            Vector4 starColor;
            starColor = design.IsFavorite
                ? new Vector4(1f, 0.8f, 0.2f, hovered ? 1f : 0.7f) // Gold for normal favourites
                : new Vector4(0.5f, 0.5f, 0.5f, hovered ? 0.8f : 0.4f); // Grey for normal unfavourited

            ImGui.PushStyleColor(ImGuiCol.Text, starColor);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f)); // CENTER ICON

            bool buttonClicked = ImGui.Button($"{star}##{design.Name}", new Vector2(btnSize, btnSize));

            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
            
            if (buttonClicked)
            {
                bool wasFavorite = design.IsFavorite;
                design.IsFavorite = !design.IsFavorite;

                // Trigger particle effect
                Vector2 effectPos = ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2;
                string effectKey = $"{character.Data.Name}_{design.Name}";

                plugin.SaveConfiguration();
                SortDesigns(character);
            }
            
            // Add tooltip for all favourite buttons
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(design.IsFavorite ? "Remove from favourites" : "Add to favourites");
            }

            x += btnSize + spacing;

            // Design name styling
            float rightZone = hovered ? (3 * btnSize + 2 * spacing + pad) : 0; // Only show buttons on hover
            float availW = rowW - (x - rowMin.X) - rightZone - pad;

            ImGui.SetCursorScreenPos(new Vector2(x, rowMin.Y + (rowH - ImGui.GetTextLineHeight()) / 2));

            var name = design.Name;
            if (ImGui.CalcTextSize(name).X > availW)
                name = TruncateWithEllipsis(name, availW);

            // Design name
            bool isActive = IsDesignCurrentlyActive(character, design);
            var textColor = isActive ? new Vector4(0.2f, 0.9f, 0.2f, 1f) : new Vector4(0.9f, 0.9f, 0.9f, 1f); // Green for active, light gray for inactive
            ImGui.PushStyleColor(ImGuiCol.Text, textColor);
            ImGui.TextUnformatted(name);
            ImGui.PopStyleColor();

            // Action buttons (only when hovered, compact)
            if (hovered)
            {
                DrawCompactDesignActionButtons(character, design, rowMin, rowW, rowH, btnSize, spacing, pad, scale);
            }
        }

        private void DrawCompactDesignActionButtons(Character character, CharacterDesign design, Vector2 rowMin, float rowW, float rowH, float btnSize, float spacing, float pad, float scale)
        {
            // Position buttons
            float startX = rowMin.X + rowW - (3 * btnSize + 2 * spacing + pad);
            float buttonY = rowMin.Y + (rowH - btnSize) / 2;

            // Dark button styling
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.35f, 0.35f, 1f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f * scale);

            // Apply button
            ImGui.SetCursorScreenPos(new Vector2(startX, buttonY));
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.8f, 0.3f, 1f)); // Green
            if (ImGui.Button("\uf00c", new Vector2(btnSize, btnSize)))
            {
                // Switch gearset if assigned (design overrides character)
                if (plugin.Configuration.EnableGearsetAssignments)
                {
                    var effectiveGearset = design.AssignedGearset ?? character.Data.AssignedGearset;
                    if (effectiveGearset.HasValue)
                    {
                        //plugin.SwitchToGearset(effectiveGearset.Value);
                    }
                }

                // Check if this is a Secret Mode (Conflict Resolution) design
                if (design.SecretModState != null && design.SecretModState.Any())
                {
                    // Ensure the correct Penumbra collection is assigned before CR modifies it
                    if (!string.IsNullOrWhiteSpace(character.Data.PenumbraCollection))
                    {
                        plugin.EnsurePenumbraCollectionAssignment(character.Data.PenumbraCollection);
                    }
                }
                else
                {
                    // Regular design - just execute the macro
                    GameCommandManager.ExecuteMacro(design.Macro, character, design.Name);
                    // Track last used design and character for auto-reapplication and UI feedback
                    plugin.Configuration.LastUsedDesignByCharacter[character.Data.Name] = design.Name;
                    plugin.Configuration.LastUsedDesignCharacterKey = character.Data.Name;
                    plugin.Configuration.LastUsedCharacterKey = character.Data.Name;
                    
                    // Update player-specific character tracking for green highlighting
                    if (Plugin.ObjectTable.LocalPlayer is { } player && player.HomeWorld.IsValid)
                    {
                        string localName = player.Name.TextValue;
                        string worldName = player.HomeWorld.Value.Name.ToString();
                        string fullKey = $"{localName}@{worldName}";
                        string pluginCharacterKey = $"{character.Data.Name}@{worldName}";
                        plugin.Configuration.LastUsedCharacterByPlayer[fullKey] = pluginCharacterKey;
                    }
                    
                    plugin.Configuration.Save();
                }
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Apply Design");

                // Preview image in tooltip
                if (!string.IsNullOrEmpty(design.PreviewImagePath) && File.Exists(design.PreviewImagePath))
                {
                    var texture = Plugin.TextureProvider.GetFromFile(design.PreviewImagePath).GetWrapOrDefault();
                    if (texture != null)
                    {
                        float maxSize = 300f * scale;
                        var (displayWidth, displayHeight) = CalculateImageDimensions(texture, maxSize);
                        ImGui.Image((ImTextureID)texture.Handle, new Vector2(displayWidth, displayHeight));
                    }
                }
                ImGui.EndTooltip();
            }

            // Edit button
            ImGui.SetCursorScreenPos(new Vector2(startX + btnSize + spacing, buttonY));
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.7f, 1f, 1f)); // Blue
            if (ImGui.Button("\uf044", new Vector2(btnSize, btnSize)))
            {
                // Open edit window first
                OpenEditDesignWindow(character, design);
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Edit Design");

            // Delete button
            ImGui.SetCursorScreenPos(new Vector2(startX + 2 * (btnSize + spacing), buttonY));
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f)); // Red
            var io = ImGui.GetIO();
            if (ImGui.Button("\uf2ed", new Vector2(btnSize, btnSize)) && io.KeyCtrl && io.KeyShift)
            {
                character.Data.Designs.Remove(design);
                plugin.SaveConfiguration();
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold Ctrl+Shift to delete");

            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
        }

        private void HandleDesignDragDrop(Character character, CharacterDesign design, Vector2 rowMin, Vector2 rowMax, bool hovered, float scale)
        {
            // Manual drop target
            if (draggedDesign != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                ImGui.IsMouseHoveringRect(rowMin, rowMax, true) && draggedDesign != design)
            {
                var list = character.Data.Designs;
                list.Remove(draggedDesign);
                int idx = list.IndexOf(design);
                draggedDesign.FolderId = design.FolderId;
                list.Insert(idx, draggedDesign);
                draggedDesign = null;
                plugin.SaveConfiguration();
            }

            // Blue outline while dragging over
            if (draggedDesign != null && hovered)
            {
                var dl = ImGui.GetWindowDrawList();
                uint col = ImGui.GetColorU32(new Vector4(0.27f, 0.53f, 0.90f, 1f));
                dl.AddRect(rowMin, rowMax, col, 0, ImDrawFlags.None, 2 * scale);
            }
        }

        private void HandleDropToRoot(bool anyHeaderHovered, bool anyRowHovered, Character character)
        {
            if (draggedDesign != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                !anyHeaderHovered && !anyRowHovered)
            {
                draggedDesign.FolderId = null;
                plugin.SaveConfiguration();
                draggedDesign = null;
            }

            if (draggedFolder != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                !anyHeaderHovered && !anyRowHovered)
            {
                draggedFolder.ParentFolderId = null;
                plugin.SaveConfiguration();
                draggedFolder = null;
            }
        }

        private void DrawImportWindow(float scale)
        {
            if (!isImportWindowOpen || targetForDesignImport == null)
                return;

            var windowSize = new Vector2(400 * scale, 450 * scale);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Import Designs", ref isImportWindowOpen, ImGuiWindowFlags.NoCollapse))
            {
                ApplyScaledStyles(scale);

                ImGui.Text($"Import designs to: {targetForDesignImport.Data.Name}");
                ImGui.Separator();

                ImGui.BeginChild("ImportScrollArea", new Vector2(0, -40 * scale), false);

                var charactersWithDesigns = plugin.Characters
                    .Where(c => c != targetForDesignImport && c.Data.Designs.Count > 0)
                    .OrderBy(c => c.Data.Name)
                    .ToList();

                foreach (var character in charactersWithDesigns)
                {
                    if (ImGui.CollapsingHeader($"{character.Data.Name} ({character.Data.Designs.Count} designs)"))
                    {
                        float indentAmount = 15f * scale;
                        ImGui.Indent(indentAmount);

                        foreach (var design in character.Data.Designs)
                        {
                            float buttonSize = 18f * scale;

                            // Green plus symbol — use design GUID for unique ID (names can collide)
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
                            ImGui.PushFont(UiBuilder.IconFont);

                            if (ImGui.Selectable($"\uf067##import_{design.Id}", false, ImGuiSelectableFlags.None, new Vector2(buttonSize, buttonSize)))
                            {
                                // Clone the entire design using JSON serialization (exact copy like copy-paste in config)
                                var json = JsonConvert.SerializeObject(design);
                                var clone = JsonConvert.DeserializeObject<CharacterDesign>(json);
                                clone.Name = design.Name + " (Copy)";
                                clone.Id = Guid.NewGuid();
                                clone.DateAdded = DateTime.UtcNow;
                                clone.FolderId = null; // reset so it appears at root level

                                targetForDesignImport.Data.Designs.Add(clone);
                                plugin.SaveConfiguration();
                            }

                            ImGui.PopFont();
                            ImGui.PopStyleColor();

                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip($"Import '{design.Name}'");
                            }

                            ImGui.SameLine();
                            ImGui.Text(design.Name);
                        }

                        ImGui.Unindent(indentAmount);
                    }
                }

                ImGui.EndChild();

                ImGui.Separator();
                if (ImGui.Button("Close"))
                {
                    isImportWindowOpen = false;
                }

                PopScaledStyles();
            }
            ImGui.End();
        }
        
        // Utility methods
        private void SelectPreviewImage()
        {
            plugin.OpenFilePicker(
                "Select Design Preview Image",
                "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|PNG files (*.png)|*.png",
                (selectedPath) =>
                {
                    lock (this)
                    {
                        pendingDesignImagePath = selectedPath;
                    }
                }
            );
        }

        private void PasteImageFromClipboard()
        {
            try
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        // Check if clipboard contains image data
                        if (!Clipboard.ContainsImage())
                        {
                            Plugin.Log.Warning("No image found in clipboard");
                            return;
                        }

                        // Get image from clipboard
                        using (var clipboardImage = Clipboard.GetImage())
                        {
                            if (clipboardImage == null)
                            {
                                Plugin.Log.Warning("Failed to get image from clipboard");
                                return;
                            }

                            // Create directory if it doesn't exist
                            string configDir = plugin.PluginPath;
                            string imagesDir = Path.Combine(configDir, "Images");
                            string previewsDir = Path.Combine(imagesDir, "DesignPreviews");
                            
                            Directory.CreateDirectory(previewsDir);

                            // Generate unique filename with timestamp
                            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                            string fileName = $"design_preview_{timestamp}.png";
                            string fullPath = Path.Combine(previewsDir, fileName);

                            // Save image as PNG
                            clipboardImage.Save(fullPath, ImageFormat.Png);

                            // Set the path for UI update
                            lock (this)
                            {
                                pendingPastedImagePath = fullPath;
                            }

                            Plugin.Log.Info($"Pasted image saved to: {fullPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Error($"Error pasting image from clipboard: {ex.Message}");
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Critical clipboard paste error: {ex.Message}");
            }
        }

        private bool IsClipboardImageAvailable()
        {
            try
            {
                return Clipboard.ContainsImage();
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        private (float width, float height) CalculateImageDimensions(IDalamudTextureWrap texture, float maxSize)
        {
            float originalWidth = texture.Width;
            float originalHeight = texture.Height;
            float aspectRatio = originalWidth / originalHeight;

            if (aspectRatio > 1) // Landscape
            {
                return (maxSize, maxSize / aspectRatio);
            }
            else // Portrait or Square
            {
                return (maxSize * aspectRatio, maxSize);
            }
        }

        private void AddNewDesign()
        {
            isNewDesign = true;
            isEditDesignWindowOpen = true;
            plugin.WindowState.IsEditDesignWindowOpen = true;
            editedDesignName = "";
            editedGlamourerDesign = "";
            editedDesignMacro = "";
            isAdvancedModeDesign = false;
            editedAutomation = "";
            editedCustomizeProfile = "";
            editedGearset = null;
            editedDesignPreviewPath = "";
            plugin.WindowState.EditedDesignName = editedDesignName;
            plugin.WindowState.EditedGlamourerDesign = editedGlamourerDesign;
        }

        private void OpenEditDesignWindow(Character character, CharacterDesign design)
        {
            isNewDesign = false;
            isEditDesignWindowOpen = true;
            plugin.WindowState.IsEditDesignWindowOpen = true;
            originalDesignName = design.Name;
            editedDesignName = design.Name;
            editedDesignMacro = design.IsAdvancedMode ? design.AdvancedMacro ?? "" : design.Macro ?? "";
            editedGlamourerDesign = !string.IsNullOrWhiteSpace(design.GlamourerDesign)
                ? design.GlamourerDesign
                : ExtractGlamourerDesignFromMacro(design.Macro ?? "");

            editedAutomation = design.Automation ?? "";
            editedCustomizeProfile = design.CustomizePlusProfile ?? "";
            editedGearset = design.AssignedGearset;
            editedDesignPreviewPath = design.PreviewImagePath ?? "";
            isAdvancedModeDesign = design.IsAdvancedMode;
            isAdvancedModeWindowOpen = design.IsAdvancedMode;
            advancedDesignMacroText = design.AdvancedMacro ?? "";
            
            // Check if this is a Secret Mode (Conflict Resolution) design
            if ((design.SecretModState != null && design.SecretModState.Any()) ||
                (design.ModOptionSettings != null && design.ModOptionSettings.Any()) ||
                (design.SecretModPinOverrides != null && design.SecretModPinOverrides.Any()))
            {
                isSecretDesignMode = true;
                // Load the existing mod state into temporary storage for editing
                if (design.SecretModState != null)
                {
                    temporaryDesignSecretModState = new Dictionary<string, bool>(design.SecretModState);
                }
                if (design.SecretModPinOverrides != null)
                {
                    temporaryDesignSecretModPinOverrides = new HashSet<string>(design.SecretModPinOverrides);
                }
            }
        }

        private void CloseDesignEditor()
        {
            isEditDesignWindowOpen = false;
            plugin.WindowState.IsEditDesignWindowOpen = false;
            isAdvancedModeWindowOpen = false;
            isNewDesign = false;
            isSecretDesignMode = false;
            
            ResetEditFields();
        }

        private void ResetEditFields()
        {
            editedDesignName = "";
            editedDesignMacro = "";
            editedGlamourerDesign = "";
            editedAutomation = "";
            editedCustomizeProfile = "";
            editedDesignPreviewPath = "";
            advancedDesignMacroText = "";
            originalDesignName = "";
            temporaryDesignSecretModState = null;
            temporaryDesignSecretModPinOverrides = null;
        }

        private void SaveDesign(Character character)
        {
            if (string.IsNullOrWhiteSpace(editedDesignName) || string.IsNullOrWhiteSpace(editedGlamourerDesign))
                return;

            var existingDesign = !isNewDesign
                ? character.Data.Designs.FirstOrDefault(d => d.Name == originalDesignName)
                : null;

            if (existingDesign != null)
            {
                // Update existing design
                existingDesign.Name = editedDesignName;
                bool wasPreviouslyAdvanced = existingDesign.IsAdvancedMode;
                bool keepAdvanced = wasPreviouslyAdvanced && !isAdvancedModeDesign;

                // For advanced mode with empty macro, generate from form fields
                string advancedMacroToUse = advancedDesignMacroText;
                if ((isAdvancedModeDesign || keepAdvanced) && string.IsNullOrWhiteSpace(advancedMacroToUse))
                {
                    advancedMacroToUse = GenerateDesignMacro(character);
                }

                existingDesign.Macro = keepAdvanced
                    ? advancedMacroToUse
                    : (isAdvancedModeDesign ? advancedMacroToUse : GenerateDesignMacro(character));

                existingDesign.AdvancedMacro = isAdvancedModeDesign || keepAdvanced
                    ? advancedMacroToUse
                    : "";

                existingDesign.IsAdvancedMode = isAdvancedModeDesign || keepAdvanced;
                existingDesign.Automation = editedAutomation;
                existingDesign.GlamourerDesign = editedGlamourerDesign;
                existingDesign.CustomizePlusProfile = editedCustomizeProfile;
                existingDesign.AssignedGearset = editedGearset;
                existingDesign.PreviewImagePath = editedDesignPreviewPath;

                // Apply any Secret Mode state that was configured during editing
                if (temporaryDesignSecretModState != null)
                {
                    existingDesign.SecretModState = temporaryDesignSecretModState;
                }
                if (temporaryDesignSecretModPinOverrides != null)
                {
                    existingDesign.SecretModPinOverrides = temporaryDesignSecretModPinOverrides;
                }
            }
            else
            {
                // Add new design - generate macro from fields if advanced mode has empty macro
                string macroForNewDesign = isAdvancedModeDesign
                    ? (string.IsNullOrWhiteSpace(advancedDesignMacroText) ? GenerateDesignMacro(character) : advancedDesignMacroText)
                    : GenerateDesignMacro(character);

                var newDesign = new CharacterDesign(
                    editedDesignName,
                    macroForNewDesign,
                    isAdvancedModeDesign,
                    isAdvancedModeDesign ? macroForNewDesign : "",
                    editedGlamourerDesign,
                    editedAutomation,
                    editedCustomizeProfile,
                    editedDesignPreviewPath
                )
                {
                    DateAdded = DateTime.UtcNow,
                    AssignedGearset = editedGearset
                };

                // Apply any Secret Mode state that was configured during editing
                if (temporaryDesignSecretModState != null)
                {
                    newDesign.SecretModState = temporaryDesignSecretModState;
                }
                if (temporaryDesignSecretModPinOverrides != null)
                {
                    newDesign.SecretModPinOverrides = temporaryDesignSecretModPinOverrides;
                }

                character.Data.Designs.Add(newDesign);
            }

            plugin.SaveConfiguration();
        }

        private void DeleteFolder(Character character, DesignFolder folder)
        {
            foreach (var d in character.Data.Designs.Where(d => d.FolderId == folder.Id))
                d.FolderId = null;

            foreach (var sub in character.Data.DesignFolders.Where(f => f.ParentFolderId == folder.Id))
                sub.ParentFolderId = null;

            character.Data.DesignFolders.RemoveAll(f => f.Id == folder.Id);

            plugin.SaveConfiguration();
        }

        private DesignSortType GetDesignSortFromConfig()
        {
            return plugin.Configuration.CurrentDesignSortIndex switch
            {
                0 => DesignSortType.Favorites,
                1 => DesignSortType.Alphabetical,
                2 => DesignSortType.Recent,
                3 => DesignSortType.Oldest,
                4 => DesignSortType.Manual,
                _ => DesignSortType.Alphabetical // Default fallback
            };
        }
        
        private void SetDesignSort(int sortIndex)
        {
            plugin.Configuration.CurrentDesignSortIndex = sortIndex;
            plugin.Configuration.Save();
        }

        private void SortDesigns(Character character)
        {
            var sortType = currentDesignSort;
            if (sortType == DesignSortType.Manual)
                return;

            // Sort all designs - both root level and within folders
            SortDesignList(character.Data.Designs, sortType);
        }
        
        private void SortDesignList(List<CharacterDesign> designs, DesignSortType sortType)
        {
            if (sortType == DesignSortType.Favorites)
            {
                designs.Sort((a, b) =>
                {
                    int favCompare = b.IsFavorite.CompareTo(a.IsFavorite);
                    if (favCompare != 0) return favCompare;
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else if (sortType == DesignSortType.Alphabetical)
            {
                designs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            else if (sortType == DesignSortType.Recent)
            {
                designs.Sort((a, b) => b.DateAdded.CompareTo(a.DateAdded));
            }
            else if (sortType == DesignSortType.Oldest)
            {
                designs.Sort((a, b) => a.DateAdded.CompareTo(b.DateAdded));
            }
        }

        private Vector4 GetFolderColor(Character character, DesignFolder folder)
        {
            Vector3 baseColor;

            if (folder.CustomColor.HasValue)
            {
                baseColor = folder.CustomColor.Value;
            }
            else
            {
                baseColor = GetAutoGeneratedColor(character, folder);
            }

            return new Vector4(baseColor.X, baseColor.Y, baseColor.Z, 0.6f);
        }

        private Vector3 GetAutoGeneratedColor(Character character, DesignFolder folder)
        {
            return character.Data.NameplateColor;
        }

        private List<(string name, bool isFolder, object item, DateTime dateAdded, int manual)> BuildRenderItems(Character character)
        {
            var renderItems = new List<(string name, bool isFolder, object item, DateTime dateAdded, int manual)>();

            // Apply search filtering if active
            var designsToShow = character.Data.Designs.AsEnumerable();
            var foldersToShow = character.Data.DesignFolders.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                designsToShow = designsToShow.Where(d => MatchesSearchQuery(d));
                foldersToShow = foldersToShow.Where(f => FolderContainsMatchingDesigns(character, f));
            }

            foreach (var f in foldersToShow.Where(f => f.ParentFolderId == null))
            {
                renderItems.Add((f.Name, true, f as object, DateTime.MinValue, f.SortOrder));
            }

            foreach (var d in designsToShow.Where(d => d.FolderId == null))
            {
                renderItems.Add((d.Name, false, d as object, d.DateAdded, d.SortOrder));
            }

            switch (currentDesignSort)
            {
                case DesignSortType.Favorites:
                    renderItems = renderItems
                        .OrderByDescending(x => x.isFolder ? false : ((CharacterDesign)x.item).IsFavorite)
                        .ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    break;
                case DesignSortType.Alphabetical:
                    renderItems = renderItems
                        .OrderBy(x => x.name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    break;
                case DesignSortType.Recent:
                    renderItems = renderItems
                        .OrderByDescending(x => x.dateAdded)
                        .ToList();
                    break;
                case DesignSortType.Oldest:
                    renderItems = renderItems
                        .OrderBy(x => x.dateAdded)
                        .ToList();
                    break;
                case DesignSortType.Manual:
                    renderItems = renderItems
                        .OrderBy(x => x.manual)
                        .ToList();
                    break;
            }

            return renderItems;
        }

        private string GenerateDesignMacro(Character character)
        {
            if (string.IsNullOrWhiteSpace(editedGlamourerDesign))
                return "";

            string macro = $"/glamour apply {editedGlamourerDesign} | self";

            // Conditionally include automation line
            if (plugin.Configuration.EnableAutomations)
            {
                string automationToUse = !string.IsNullOrWhiteSpace(editedAutomation)
                    ? editedAutomation
                    : (!string.IsNullOrWhiteSpace(character.Data.CharacterAutomation)
                        ? character.Data.CharacterAutomation
                        : "None");

                macro += $"\n/glamour automation enable {automationToUse}";
            }

            // Always disable Customize+ first
            macro += "\n/customize profile disable <me>";

            // Determine Customize+ profile
            string customizeProfileToUse = !string.IsNullOrWhiteSpace(editedCustomizeProfile)
                ? editedCustomizeProfile
                : !string.IsNullOrWhiteSpace(character.Data.CustomizeProfile)
                    ? character.Data.CustomizeProfile
                    : string.Empty;

            // Enable only if needed
            if (!string.IsNullOrWhiteSpace(customizeProfileToUse))
                macro += $"\n/customize profile enable <me>, {customizeProfileToUse}";

            // Redraw line
            macro += "\n/penumbra redraw self";

            return macro;
        }

        private string ExtractGlamourerDesignFromMacro(string macro)
        {
            string[] lines = macro.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("/glamour apply ", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Replace("/glamour apply ", "").Replace(" | self", "").Trim();
                }
            }
            return "";
        }

        private static string TruncateWithEllipsis(string text, float maxWidth)
        {
            while (ImGui.CalcTextSize(text + "...").X > maxWidth && text.Length > 0)
                text = text[..^1];
            return text + "...";
        }
        
        // Search helper methods
        private bool MatchesSearchQuery(CharacterDesign design)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return true;
                
            var query = searchQuery.ToLowerInvariant();
            
            // Search in design name
            if (design.Name.ToLowerInvariant().Contains(query))
                return true;
                
            // Search in glamourer design name
            if (!string.IsNullOrWhiteSpace(design.GlamourerDesign) && 
                design.GlamourerDesign.ToLowerInvariant().Contains(query))
                return true;
                
            // Search in automation
            if (!string.IsNullOrWhiteSpace(design.Automation) && 
                design.Automation.ToLowerInvariant().Contains(query))
                return true;
                
            // Search in tags
            if (design.Tag?.ToLowerInvariant().Contains(query) == true)
                return true;
                
            return false;
        }
        
        private bool FolderContainsMatchingDesigns(Character character, DesignFolder folder)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return true;
                
            // Check if folder name matches
            if (folder.Name.ToLowerInvariant().Contains(searchQuery.ToLowerInvariant()))
                return true;
                
            // Check if any design in this folder matches
            if (character.Data.Designs.Any(d => d.FolderId == folder.Id && MatchesSearchQuery(d)))
                return true;
                
            // Check if any subfolder contains matching designs
            var subfolders = character.Data.DesignFolders.Where(f => f.ParentFolderId == folder.Id);
            foreach (var subfolder in subfolders)
            {
                if (FolderContainsMatchingDesigns(character, subfolder))
                    return true;
            }
                
            return false;
        }

        private void CheckClipboardForImage()
        {
            try
            {
                // Clipboard operations need to be on STA thread
                var thread = new Thread(() =>
                {
                    try
                    {
                        // Check if clipboard contains image data
                        snapshotHasClipboardImage = System.Windows.Forms.Clipboard.ContainsImage();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Warning($"Failed to check clipboard for image: {ex.Message}");
                        snapshotHasClipboardImage = false;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"Failed to check clipboard for image: {ex.Message}");
                snapshotHasClipboardImage = false;
            }
        }

        private async Task<string> SaveClipboardImageForDesign(Guid designId)
        {
            try
            {
                string imagePath = "";
                
                // Clipboard operations need to be on STA thread
                var thread = new Thread(() =>
                {
                    try
                    {
                        if (!System.Windows.Forms.Clipboard.ContainsImage())
                            return;

                        var image = System.Windows.Forms.Clipboard.GetImage();
                        if (image == null)
                            return;

                        // Create designs directory if it doesn't exist
                        var designsDir = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "Designs");
                        Directory.CreateDirectory(designsDir);

                        // Save image with design ID as filename
                        imagePath = Path.Combine(designsDir, $"{designId}.png");
                        
                        using (var bitmap = new System.Drawing.Bitmap(image))
                        {
                            bitmap.Save(imagePath, ImageFormat.Png);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Error($"Failed to save clipboard image: {ex}");
                        imagePath = "";
                    }
                });
                
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                return imagePath;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to save clipboard image: {ex}");
                return string.Empty;
            }
        }

        private bool IsDesignCurrentlyActive(Character character, CharacterDesign design)
        {
            // Only show active design for the currently active SCS character
            var currentActiveCharacter = GetCurrentActiveCharacter();
            if (currentActiveCharacter == null || currentActiveCharacter.Data.Name != character.Data.Name)
                return false;

            if (plugin?.Configuration?.LastUsedDesignByCharacter == null)
                return false;

            if (!plugin.Configuration.LastUsedDesignByCharacter.TryGetValue(character.Data.Name, out var lastUsedDesignName))
                return false;

            return design.Name.Equals(lastUsedDesignName, StringComparison.OrdinalIgnoreCase);
        }
        
        
        // TODO this may be redundant
        private Character? GetCurrentActiveCharacter()
        {
            // Use the same logic as the plugin uses to determine current character
            Character? currentCharacter = null;

            // Try player-specific mapping first
            if (Plugin.ObjectTable.LocalPlayer is { } player && player.HomeWorld.IsValid)
            {
                string localName = player.Name.TextValue;
                string worldName = player.HomeWorld.Value.Name.ToString();
                string fullKey = $"{localName}@{worldName}";
                
                if (plugin.Configuration.LastUsedCharacterByPlayer.TryGetValue(fullKey, out var lastUsedCharacterName))
                {
                    // lastUsedCharacterName is in format "CharacterName@WorldName", extract just the character name
                    var characterName = lastUsedCharacterName.Contains("@") ? lastUsedCharacterName.Split('@')[0] : lastUsedCharacterName;
                    currentCharacter = plugin.Characters.FirstOrDefault(c => c.Data.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Fallback to global last used
            if (currentCharacter == null && !string.IsNullOrEmpty(plugin.Configuration.LastUsedCharacterKey))
            {
                currentCharacter = plugin.Characters.FirstOrDefault(c => c.Data.Name.Equals(plugin.Configuration.LastUsedCharacterKey, StringComparison.OrdinalIgnoreCase));
            }

            return currentCharacter;
        }
    }
}
