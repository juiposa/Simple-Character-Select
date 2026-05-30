
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Lumina.Data.Parsing.Layer;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;
using SimpleCharacterSelectPlugin.Windows.Styles;
using SimpleCharacterSelectPlugin.Windows.Utils;

namespace SimpleCharacterSelectPlugin.Windows.Components
{
    public class GearsetPanel : IDisposable
    {
        private Plugin plugin;
        private UIStyles uiStyles;
        private bool isAssignmentFormOpen = false;
        private bool isNewAssignment = false;

        private GearsetAssignment currentGearsetAssignment;
        
        public bool IsOpen { get; private set; } = false;

        // Resizable panel
        public float PanelWidth { get; private set; } = 300f; // Default width

        public GearsetPanel(Plugin plugin, UIStyles uiStyles)
        {
            this.plugin = plugin;
            this.uiStyles = uiStyles;
            currentGearsetAssignment = new GearsetAssignment();
        }
        
        public void Draw()
        {
            if (!IsOpen) return;
            
            // Calculate responsive sizing
            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier);

            // Scale the panel dimensions
            float scaledPanelWidth = PanelWidth * GetSafeScale(totalScale);
            
            float remainingHeight = ImGui.GetContentRegionAvail().Y;
            
            // Minimum height
            remainingHeight = Math.Max(remainingHeight, 100f * totalScale);
            
            CommonElements.PushScaledStyles(totalScale);

            DrawHeader(totalScale);
            
            if (isAssignmentFormOpen)
            {
                DrawAssignmentForm(totalScale);
            }

            DrawAssignmentList(totalScale);
            
            CommonElements.PopScaledStyles();
        }

        public void DrawHeader(float scale)
        {
            float buttonSize = 25f * scale;
            float spacing = 2f * scale;

            
            ImGui.BeginGroup();

            // Add button
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.27f, 1.07f, 0.27f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.35f, 0.35f, 1f));

            if (ImGui.Button("Add##AddGearsetAssignment"))
            {
                var io = ImGui.GetIO();
                bool ctrlHeld = io.KeyCtrl;
                bool shiftHeld = io.KeyShift;
                isAssignmentFormOpen = true;
                isNewAssignment = true;
            }
            
            ImGui.PopStyleColor(4);
            
            // Close button
            ImGui.SameLine(0, spacing);

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.27f, 0.27f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.2f, 0.2f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.3f, 0.3f, 1f));

            if (ImGui.Button("Close##CloseGearsetPanel"))
            {
                CloseGearsetPanel();
            }

            ImGui.PopStyleColor(4);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Close Gearset Assignment");
            }
            
            ImGui.TextWrapped("Assign a design to a gearset. Whenever you switch to that gearset, SCS will automatically switch to the design you specify.");
            
            ImGui.EndGroup();

        }
        
        private float GetSafeScale(float baseScale)
        {
            return Math.Clamp(baseScale, 0.3f, 5.0f);
        }

        private void DrawAssignmentForm(float scale)
        {
            float formHeight = 320f * scale;
            ImGui.BeginChild("EditDesignForm", new Vector2(0, formHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize);

            ImGui.Text(isNewAssignment ? "Add Gearset Assignment" : "Edit Gearset Assignment");

            float inputWidth = Math.Max(150f * scale, ImGui.GetContentRegionAvail().X - (50f * scale));

            ImGui.Separator();

            if (!isNewAssignment)
                ImGui.BeginDisabled();
            var gearsets = GearsetManager.GetPlayerGearsets();
            var gearsetOptions = gearsets.Select(gs => gs.DisplayName()).ToArray();
            var gIndex = currentGearsetAssignment.GearsetIndex;
            var gearset = gIndex >= 0 ? gearsetOptions[(int)currentGearsetAssignment.GearsetIndex] : "";
            if (CommonElements.DrawInputField("GearsetField", "Gearset", inputWidth, scale, gearsetOptions, ref gearset, "Select a gearset to assign a design to"))
            {
                if (gearset != "")
                {
                    currentGearsetAssignment.GearsetIndex = gearsetOptions.IndexOf(gearset);
                    currentGearsetAssignment.GearsetDisplay =
                        gearsets[currentGearsetAssignment.GearsetIndex].DisplayName();
                    Plugin.Log.Debug($"CHOSEN GEARSET {currentGearsetAssignment.GearsetIndex}");
                }
                else
                {
                    currentGearsetAssignment.GearsetIndex = -1;
                }

            }
            if (!isNewAssignment)
                ImGui.EndDisabled();

            if (isNewAssignment && plugin.GearsetAssignments.ContainsKey(currentGearsetAssignment.GearsetIndex))
            {
                CommonElements.ColoredText("Assignment for gearset already exists, edit from list below.", Colors.Red);
            }

            var characters = plugin.Characters;
            var characterOptions = characters.Select(ch => ch.Data.Name).ToList();
            var characterName = currentGearsetAssignment.CharacterName;
            Character? selectedCharacter = null;
            if (characterName != "")
            {
                selectedCharacter = characters.Find(ch => ch.Data.Name == characterName);
            }
            if (CommonElements.DrawInputField("CharacterField", "Character", inputWidth, scale, characterOptions, ref characterName, "Select a character to choose a design from"))
            {
                currentGearsetAssignment.CharacterName = characterName;
                if (characterName != "")
                {
                    currentGearsetAssignment.CharacterName = characterName;
                    Plugin.Log.Debug($"CHOSEN CHARACTER {currentGearsetAssignment.CharacterName}");
                }

            }
            
            if( selectedCharacter == null )
                ImGui.BeginDisabled();
            
            List<string> designOptions = selectedCharacter != null ? selectedCharacter.Data.Designs.Select(de => de.Name).ToList() : new List<string>();
            var dIndex = currentGearsetAssignment.DesignId;

            var designName = "";
            if (dIndex != null)
            {
                designName = selectedCharacter?.GetDesignByIdOrDefault(dIndex).Name;
            }
            if (CommonElements.DrawInputField("DesignField", "Design", inputWidth, scale, designOptions, ref designName, "Select design to switch to when switching to chosen gearset"))
            {
                if (designName != "")
                {
                    currentGearsetAssignment.DesignId = selectedCharacter?.Data.Designs.Find(de => de.Name == designName)?.Id;
                    currentGearsetAssignment.DesignName = designName;
                    Plugin.Log.Debug($"CHOSEN CHARACTER {currentGearsetAssignment.CharacterName}");
                }
                else
                {
                    currentGearsetAssignment.DesignId = null;
                }
            }
            
            if( selectedCharacter == null )
                ImGui.EndDisabled();


            ImGui.Separator();

            DrawFormActionButtons( scale);

            ImGui.EndChild();
        }
        
        private void DrawAssignmentList(float scale)
        {
            float remainingHeight = ImGui.GetContentRegionAvail().Y;

            // Minimum height
            remainingHeight = Math.Max(remainingHeight, 100f * scale);

            ImGui.BeginChild("AssignmentListBackground", new Vector2(0, remainingHeight), true, ImGuiWindowFlags.AlwaysVerticalScrollbar);

            foreach (var gsa in plugin.GearsetAssignments)
            {
                DrawAssignmentRow(gsa.Key, gsa.Value, scale);
            }

            ImGui.EndChild();
        }
        
        private void DrawAssignmentRow(int index, GearsetAssignment assignment, float scale)
        {
            ImGui.PushID(assignment.GearsetDisplay);

            var rowMin = ImGui.GetCursorScreenPos();
            float rowW = ImGui.GetContentRegionAvail().X;
            float rowH = 32f * scale;
            ImGui.Dummy(new Vector2(rowW, rowH));
            var rowMax = rowMin + new Vector2(rowW, rowH);

            // Draw design row content with compact styling, america's next top model has nothing on me now!
            DrawAssignmentRowContent(index, assignment, rowMin, rowMax, rowH, rowW, scale);

            ImGui.PopID();
            ImGui.SetCursorScreenPos(new Vector2(rowMin.X, rowMin.Y + rowH));
        }
        
          private void DrawAssignmentRowContent(int index, GearsetAssignment assignment, Vector2 rowMin, Vector2 rowMax, float rowH, float rowW, float scale)
        {
            float pad = 8f * scale;
            float spacing = 4f * scale;
            float btnSize = 24f * scale;
            float x = rowMin.X + (2f * scale);

            float rightZone = 2 * btnSize + 2 * spacing + pad;
            float availW = rowW - rightZone - pad;

            ImGui.SetCursorScreenPos(new Vector2(x, rowMin.Y + (rowH - ImGui.GetTextLineHeight()) / 2));

            var name = assignment.DisplayName();
            if (ImGui.CalcTextSize(name).X > availW)
                name = LayoutHelper.TruncateWithEllipsis(name, availW);

            // Assignment name
            ImGui.Text(name);
            
            // Position buttons
            float startX = rowMin.X + rowW - (3 * btnSize + 2 * spacing + pad);
            float buttonY = rowMin.Y + (rowH - btnSize) / 2;
            
            // Edit button
            ImGui.SetCursorScreenPos(new Vector2(startX + btnSize + spacing, buttonY));
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Blue);
            if (ImGui.Button("\uf044", new Vector2(btnSize, btnSize)))
            {
                currentGearsetAssignment = assignment;
                isNewAssignment = false;
                isAssignmentFormOpen = true;
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Edit");

            // Delete button
            ImGui.SetCursorScreenPos(new Vector2(startX + 2 * (btnSize + spacing), buttonY));
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
            var io = ImGui.GetIO();
            if (ImGui.Button("\uf2ed", new Vector2(btnSize, btnSize)) && io.KeyCtrl && io.KeyShift)
            {
                plugin.GearsetAssignments.Remove(index);
                plugin.SaveConfiguration();
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold Ctrl+Shift to delete");
        }
        
        // TODO refactor the side panel more
        private void DrawFormActionButtons(float scale)
        {
            float buttonWidth = 85 * scale;
            float buttonHeight = 20 * scale;
            float buttonSpacing = 8 * scale;
            float totalButtonWidth = (buttonWidth * 2 + buttonSpacing);
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float buttonPosX = (availableWidth > totalButtonWidth) ? (availableWidth - totalButtonWidth) / 2f : 0;

            ImGui.SetCursorPosX(buttonPosX);

            // Center text in buttons
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4 * scale, 4 * scale));
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));

            // Save button styling
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.3f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.6f, 0.4f, 1.0f));

            var canSave = currentGearsetAssignment.IsValid();
            if (!canSave)
                ImGui.BeginDisabled();

            if (ImGui.Button("Save", new Vector2(buttonWidth, 0)))
            {
                SaveAssignment();
                isAssignmentFormOpen = false;
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
                currentGearsetAssignment = new GearsetAssignment();
                isAssignmentFormOpen = false;
            }

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(2);
        }

        private void SaveAssignment()
        {
            plugin.GearsetAssignments[currentGearsetAssignment.GearsetIndex] = currentGearsetAssignment;
            plugin.SaveConfiguration();
            currentGearsetAssignment = new GearsetAssignment();
        }
        
        public void OpenGearsetAssignment()
        {
            Plugin.Log.Debug($"Opening create gearset assignment window");
            currentGearsetAssignment = new GearsetAssignment();
            plugin.MainWindow.DesignPanel.Close();
            isNewAssignment = true;
            IsOpen = true;
        }
        public void CloseGearsetPanel()
        {
            IsOpen = false;
            currentGearsetAssignment = new GearsetAssignment();
            isAssignmentFormOpen = false;
            plugin.WindowState.IsEditDesignWindowOpen = false;
        }

        public void Dispose()
        {
        }


    }
}