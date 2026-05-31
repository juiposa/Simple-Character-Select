using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using System.Collections.Generic;
using SimpleCharacterSelectPlugin.Windows.Styles;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Windows
{
    public class QuickSwitchWindow : Window
    {
        private readonly Plugin plugin;
        private int selectedCharacterIndex = -1;
        private int selectedDesignIndex = -1;
        private Guid selectedDesignId;
        private bool hasInitializedSelection = false;
        private string lastTrackedDesignName = "";

        public QuickSwitchWindow(Plugin plugin)
            : base("Quick Character Switch", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize)
        {
            this.plugin = plugin;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(360, 75),
                MaximumSize = new Vector2(360, 75)
            };
        }

        public override void Draw()
        {
            float scale = ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier;

            // Apply escape key and focus settings based on config
            RespectCloseHotkey = !Plugin.Configuration.QuickSwitchIgnoreEscape;

            int themeColorCount = ThemeHelper.PushThemeColors(Plugin.Configuration);
            int themeStyleVarCount = ThemeHelper.PushThemeStyleVars(Plugin.Configuration.UIScaleMultiplier);

            try
            {
                if (!hasInitializedSelection && plugin.Characters.Count > 0)
                {
                    Init();
                    hasInitializedSelection = true;
                }

            // Base flags for compact/non-compact modes
            var baseFlags = Plugin.Configuration.QuickSwitchCompact
                ? ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground
                : ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

            // Add NoFocusOnAppearing if configured to ignore escape
            if (Plugin.Configuration.QuickSwitchIgnoreEscape)
                baseFlags |= ImGuiWindowFlags.NoFocusOnAppearing;

            this.Flags = baseFlags;

            if (Plugin.Configuration.QuickSwitchCompact)
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new System.Numerics.Vector2(360 * scale, 28 * scale),
                    MaximumSize = new System.Numerics.Vector2(360 * scale, 28 * scale),
                };

                // Get button opacity - use custom value if Custom theme, otherwise 1.0 (opaque)
                float buttonOpacity = 1.0f;

                // Push button colours for compact mode (NoBackground means semi-transparent buttons are see-through)
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.16f, 0.16f, 0.16f, buttonOpacity));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.22f, 0.22f, 0.22f, buttonOpacity));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.28f, 0.28f, 0.28f, buttonOpacity));
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.12f, 0.12f, 0.12f, buttonOpacity));
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.18f, 0.18f, 0.18f, buttonOpacity));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.22f, 0.22f, 0.22f, buttonOpacity));
            }
            else
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new System.Numerics.Vector2(360 * scale, 55 * scale),
                    MaximumSize = new System.Numerics.Vector2(360 * scale, 58 * scale),
                };
            }

            float dropdownWidth = 135 * scale;
            float spacing = 6 * scale;

            ImGui.SetNextItemWidth(dropdownWidth);
            int tempCharacterIndex = selectedCharacterIndex;

            if (ImGui.BeginCombo("##CharacterDropdown", GetSelectedCharacterName(), ImGuiComboFlags.HeightRegular))
            {
                for (int i = 0; i < plugin.Characters.Count; i++)
                {
                    var character = plugin.Characters[i];
                    bool isSelected = (tempCharacterIndex == i);

                    if (ImGui.Selectable(character.Data.Name, isSelected))
                    {
                        tempCharacterIndex = i;

                        if (character.Data.Designs.Count > 0)
                        {
                            var sortedDesigns = GetSortedDesigns(character);
                            if (sortedDesigns.Count > 0)
                            {
                                selectedDesignIndex = GetOriginalIndex(character, sortedDesigns[0]);
                            }
                        }
                        else
                        {
                            selectedDesignIndex = -1;
                        }
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            selectedCharacterIndex = tempCharacterIndex;

            ImGui.SameLine(0, spacing);

            if (selectedCharacterIndex >= 0 && selectedCharacterIndex < plugin.Characters.Count)
            {
                var selectedCharacter = plugin.Characters[selectedCharacterIndex];

                int tempDesignIndex = selectedDesignIndex;
                Guid tempDesignId = selectedDesignId;

                ImGui.SetNextItemWidth(dropdownWidth);
                if (ImGui.BeginCombo("##DesignDropdown", GetSelectedDesignName(selectedCharacter), ImGuiComboFlags.HeightRegular))
                {

                    var orderedDesigns = GetSortedDesigns(selectedCharacter)
                        .Select((d, index) => new { Design = d, OriginalIndex = GetOriginalIndex(selectedCharacter, d) })
                        .ToList();

                    for (int j = 0; j < orderedDesigns.Count; j++)
                    {
                        var entry = orderedDesigns[j];
                        bool isSelected = (tempDesignIndex == entry.OriginalIndex);

                        if (ImGui.Selectable(entry.Design.Name, isSelected))
                        {
                            tempDesignId = entry.Design.Id;
                            tempDesignIndex = entry.OriginalIndex;
                            lastTrackedDesignName = entry.Design.Name;
                        }

                        // Preview tooltip
                        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(entry.Design.PreviewImagePath) && File.Exists(entry.Design.PreviewImagePath))
                        {
                            try
                            {
                                var texture = Plugin.TextureProvider.GetFromFile(entry.Design.PreviewImagePath).GetWrapOrDefault();
                                if (texture != null)
                                {
                                    float maxSize = 300f * scale;
                                    var (displayWidth, displayHeight) = CalculateImageDimensions(texture, maxSize);

                                    var mousePos = ImGui.GetMousePos();
                                    var dropdownRect = ImGui.GetItemRectMax();
                                    var viewportSize = ImGui.GetMainViewport().Size;

                                    var tooltipPos = new Vector2(dropdownRect.X + 10, mousePos.Y - displayHeight / 2);

                                    if (tooltipPos.X + displayWidth > viewportSize.X)
                                        tooltipPos.X = ImGui.GetItemRectMin().X - displayWidth - 10;

                                    if (tooltipPos.Y < 0)
                                        tooltipPos.Y = 0;
                                    else if (tooltipPos.Y + displayHeight > viewportSize.Y)
                                        tooltipPos.Y = viewportSize.Y - displayHeight;

                                    ImGui.SetNextWindowPos(tooltipPos);
                                    ImGui.BeginTooltip();
                                    ImGui.Image(texture.Handle, new Vector2(displayWidth, displayHeight));
                                    ImGui.EndTooltip();
                                }
                            }
                            catch { }
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }

                selectedDesignIndex = tempDesignIndex;
                selectedDesignId = tempDesignId;
            }
            else
            {
                ImGui.BeginDisabled();
                ImGui.SetNextItemWidth(dropdownWidth);
                ImGui.Combo("##DesignDropdown", ref selectedDesignIndex, Array.Empty<string>(), 0);
                ImGui.EndDisabled();
            }

            ImGui.SameLine(0, spacing);

            if (selectedCharacterIndex >= 0)
            {
                if (ImGui.Button("Apply", new Vector2(50 * scale, ImGui.GetFrameHeight())))
                {
                    ApplySelection();
                }
            }
            else
            {
                ImGui.BeginDisabled();
                ImGui.Button("Apply", new Vector2(50 * scale, ImGui.GetFrameHeight()));
                ImGui.EndDisabled();
            }

                if (selectedCharacterIndex >= 0)
                {
                    Vector4 charColor = GetNameplateColor(plugin.Characters[selectedCharacterIndex]);
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, charColor);
                    ImGui.BeginChild("ColorBar", new Vector2(ImGui.GetContentRegionAvail().X, 3), false);
                    ImGui.EndChild();
                    ImGui.PopStyleColor();
                }
            }
            finally
            {
                // Pop compact mode opaque colours if we pushed them
                if (Plugin.Configuration.QuickSwitchCompact)
                {
                    ImGui.PopStyleColor(6);
                }
                ThemeHelper.PopThemeStyleVars(themeStyleVarCount);
                ThemeHelper.PopThemeColors(themeColorCount);
            }
        }
        
        private void Init()
        {
            if (plugin.StartupComplete && plugin.ActivePlayer.Pc.ActiveCharacter != null)
            {
                var character = plugin.ActivePlayer.Pc.ActiveCharacter;
                selectedCharacterIndex = plugin.Characters.IndexOf(character);

                selectedDesignIndex = plugin.ActivePlayer.Pc.ActiveDesign;
                selectedDesignId = plugin.ActivePlayer.Pc.ActiveDesignId;
                lastTrackedDesignName = character.Data.Designs[selectedDesignIndex].Name;
            }
        }

        public void RefreshSelection()
        {
            hasInitializedSelection = false;
        }

        public void UpdateSelectionFromCharacter(Character character)
        {
            if (character == null) return;

            var index = plugin.Characters.IndexOf(character);
            if (index >= 0)
            {
                selectedCharacterIndex = index;
                selectedDesignIndex = character.Data.DefaultDesignIndex;
            }
        }

        private List<CharacterDesign> GetSortedDesigns(Character character)
        {
            var sortIndex = Plugin.Configuration.CurrentDesignSortIndex;
            var designs = character.Data.Designs.ToList();
            
            // 0=Favorites, 1=Alphabetical, 2=Recent, 3=Oldest, 4=Manual
            if (sortIndex == 4) // Manual
                return designs;

            if (sortIndex == 0) // Favorites
            {
                designs.Sort((a, b) =>
                {
                    int favCompare = b.IsFavorite.CompareTo(a.IsFavorite);
                    if (favCompare != 0) return favCompare;
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else if (sortIndex == 1) // Alphabetical
            {
                designs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            else if (sortIndex == 2) // Recent
            {
                designs.Sort((a, b) => b.DateAdded.CompareTo(a.DateAdded));
            }
            else if (sortIndex == 3) // Oldest
            {
                designs.Sort((a, b) => a.DateAdded.CompareTo(b.DateAdded));
            }
            
            return designs;
        }
        
        private int GetOriginalIndex(Character character, CharacterDesign design)
        {
            return character.Data.Designs.FindIndex(d => d.Id == design.Id);
        }

        private Vector4 GetNameplateColor(Character character)
        {
            return new Vector4(character.Data.NameplateColor.X, character.Data.NameplateColor.Y, character.Data.NameplateColor.Z, 1.0f);
        }

        private string GetSelectedCharacterName()
        {
            return (selectedCharacterIndex >= 0 && selectedCharacterIndex < plugin.Characters.Count)
                ? plugin.Characters[selectedCharacterIndex].Data.Name
                : "Select Character";
        }

        private string GetSelectedDesignName(Character character)
        {
            return (selectedDesignIndex >= 0 && selectedDesignIndex < character.Data.Designs.Count)
                ? character.Data.Designs[selectedDesignIndex].Name
                : "Select Design";
        }

        private void ApplySelection()
        {
            if (selectedCharacterIndex < 0 || selectedCharacterIndex >= plugin.Characters.Count)
                return;

            var character = plugin.Characters[selectedCharacterIndex];
            DesignManager.ApplyProfile(plugin.ActivePlayer.Pc, character, selectedDesignId);
        }

        private (float width, float height) CalculateImageDimensions(Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap texture, float maxSize)
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
    }
}
