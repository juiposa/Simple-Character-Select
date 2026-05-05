using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.ImGuiSeStringRenderer;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;
using SimpleCharacterSelectPlugin.Windows.Styles;
using SimpleCharacterSelectPlugin.Windows.Utils;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;
using SeStringBuilder = Lumina.Text.SeStringBuilder;
using DalamudSeStringBuilder = Dalamud.Game.Text.SeStringHandling.SeStringBuilder;

namespace SimpleCharacterSelectPlugin.Windows.Components
{
    public class CharacterForm : IDisposable
    {
        private Plugin plugin;
        private UIStyles uiStyles;

        // Form state
        public bool IsEditWindowOpen { get; private set; } = false;
        private Character currentCharacter;
        private int selectedCharacterIndex = -1;
        private bool isSecretMode = false;
        private bool isAdvancedModeCharacter = false;
        private string? pendingImagePath = null;
        private bool noErrors = true;

        // Temp fields for live updates
        // TODO refactor
        private string tempHonorificTitle = "";
        private string tempHonorificPrefix = "Prefix";
        private string tempHonorificSuffix = "Suffix";
        private Vector3 tempHonorificColor = new Vector3(1.0f, 1.0f, 1.0f);
        private Vector3 tempHonorificGlow = new Vector3(1.0f, 1.0f, 1.0f);
        private Vector3 tempHonorificColor3 = new Vector3(0.5f, 0.5f, 1.0f);  // Default light blue for contrast
        private int? tempHonorificGradientSet = null;
        private string? tempHonorificAnimationStyle = null;
        private string tempMoodlePreset = "";

        // Gradient preset names and data from Honorific (exact base64 encoded color arrays)
        private static readonly string[] GradientPresetNames = new[]
        {
            "Pride Rainbow", "Transgender", "Lesbian", "Bisexual",
            "Black & White", "Black & Red", "Black & Blue", "Black & Yellow",
            "Black & Green", "Black & Pink", "Black & Cyan", "Cherry Blossom",
            "Golden", "Pastel Rainbow", "Dark Rainbow", "Non-binary"
        };

        private static readonly string[] GradientPresetData = new[]
        {
            "5AMD6RsC7TMC8ksB92MB/HsA/5EA/6IA/7IA/8MA/9QA/+UA5+MEutAKjr0RYaoYNZYeCIMlAHlFAG9rAGaRAF23AFTdAkv9FkXnKj/RPjm8UjOmZi2QcymCcymCcymCcymCcymCcymCZi2QUjOmPjm8Kj/RFkXnAkv9AFTdAF23AGaRAG9rAHlFCIMlNZYeYaoYjr0RutAK5+ME/+UA/9QA/8MA/7IA/6IA/5EA/HsA92MB8ksB7TMC6RsC5AMD", // Pride Rainbow
            "W876b8nygsXplsDhqbvYvbfQ0LLI5K2/9aq59rXC+MDL+cvU+tbd/OHm/ezv/vf4//z9/fH0/Obr+9zi+tHZ+MbQ97vH9rC+7qu72q/Ex7TMs7nUn77djMLleMftZcz2Zcz2eMftjMLln77ds7nUx7TM2q/E7qu99rC+97vH+MbQ+tHZ+9zi/Obr/fH0//z9/vf4/ezv/OHm+tbd+cvU+MDL9rXC9aq55K2/0LLIvbfQqbvYlsDhgsXpb8nyW876", // Transgender
            "1S0A2lQT33ol46E46MdL7e5d8Opg9Nhe98Zc+rVZ/aNX/6Rm/7eG/8qm/93H//Hn/fj79Nnp67vY4p3G2n+10WKkzGCgxl2cwVuZvFmVtleRskqJrzqBrCp4qBpvpQpmpQpmqBpvrCp4rzqBskqJtleRvFmVwVuZxl2czGCg0WKk2oC1457H67zY9Nrp/fj7//Hn/93H/8qm/7eG/6Rm/aNX+rVZ98dc9Nhe8Opg7exd6MZK46A43nkl2lMT1S0A", // Lesbian
            "1gJwzgx1xxZ6vyB/uCmDsDOIqT2NoUeSm0+Wm0+Wm0+Wm0+WlU6XgUuZbUibWUWeRkKgMj+iHjylCjmnCjmnHjylMj+iRkKgWUWebUibgUuZlU6Xm0+Wm0+Wm0+Wm0+WoUeSqT2NsDOIuCmDvyB/xxZ6zgx11gJw", // Bisexual
            "////9/f37+/v5+fn39/f19fXzs7OxsbGvr6+tra2rq6upqamnp6elpaWjo6OhoaGfX19dXV1bW1tZWVlXV1dVVVVTU1NRUVFPT09NTU1LS0tJCQkHBwcFBQUDAwMBAQEBAQEDAwMFBQUHBwcJCQkLS0tNTU1PT09RUVFTU1NVVVVXV1dZWVlbW1tdXV1fX19hoaGjo6OlpaWnp6epqamrq6utra2vr6+xsbGzs7O19fX39/f5+fn7+/v9/f3////", // Black & White
            "/wAA9QAA6wAA4QAA1wAAzAAAwgAAuAAArgAApAAAmgAAkAAAhgAAewAAcQAAZwAAXQAAUwAASQAAPwAANQAAKwAAIAAAFgAADAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAADAAAFgAAIAAAKgAANQAAPwAASQAAUwAAXQAAZwAAcQAAewAAhgAAkAAAmgAApAAArgAAuAAAwgAAzAAA1wAA4QAA6wAA9QAA/wAA", // Black & Red
            "AAD/AAD1AADrAADhAADXAADMAADCAAC4AACuAACkAACaAACQAACGAAB7AABxAABnAABdAABTAABJAAA/AAA1AAArAAAgAAAWAAAMAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAMAAAWAAAgAAAqAAA1AAA/AABJAABTAABdAABnAABxAAB7AACGAACQAACaAACkAACuAAC4AADCAADMAADXAADhAADrAAD1AAD/", // Black & Blue
            "//8A9fUA6+sA4eEA19cAzMwAwsIAuLgArq4ApKQAmpoAkJAAhoYAe3sAcXEAZ2cAXV0AU1MASUkAPz8ANTUAKysAICAAFhYADAwAAgIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgIADAwAFhYAICAAKioANTUAPz8ASUkAU1MAXV0AZ2cAcXEAe3sAhoYAkJAAmpoApKQArq4AuLgAwsIAzMwA19cA4eEA6+sA9fUA//8A", // Black & Yellow
            "AP8AAPUAAOsAAOEAANcAAMwAAMIAALgAAK4AAKQAAJoAAJAAAIYAAHsAAHEAAGcAAF0AAFMAAEkAAD8AADUAACsAACAAABYAAAwAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAAAwAABYAACAAACoAADUAAD8AAEkAAFMAAF0AAGcAAHEAAHsAAIYAAJAAAJoAAKQAAK4AALgAAMIAAMwAANcAAOEAAOsAAPUAAP8A", // Black & Green
            "/wD/9QD16wDr4QDh1wDXzADMwgDCuAC4rgCupACkmgCakACQhgCGewB7cQBxZwBnXQBdUwBTSQBJPwA/NQA1KwArIAAgFgAWDAAMAgACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgACDAAMFgAWIAAgKgAqNQA1PwA/SQBJUwBTXQBdZwBncQBxewB7hgCGkACQmgCapACkrgCuuAC4wgDCzADM1wDX4QDh6wDr9QD1/wD/", // Black & Pink
            "AP//APX1AOvrAOHhANfXAMzMAMLCALi4AK6uAKSkAJqaAJCQAIaGAHt7AHFxAGdnAF1dAFNTAElJAD8/ADU1ACsrACAgABYWAAwMAAICAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAICAAwMABYWACAgACoqADU1AD8/AElJAFNTAF1dAGdnAHFxAHt7AIaGAJCQAJqaAKSkAK6uALi4AMLCAMzMANfXAOHhAOvrAPX1AP//", // Black & Cyan
            "7s7/7Mj36sPv573n5bje47LW4azO3qbG3KC+2pq32pW13pG84YzD5YjK6ITR7H/Y8Hvg83bm93Lt+m30/Wn5/Wf3+Wbt9GXj72Ta6mPQ5mLH4mK+3mG02mCq1l+h0l6Yzl2Oyl2GymCEzmaI0WyM1HOP13mT2n6X34Sb4oqf5ZCi6Jam65up76Gt8qex9a60+bS4/Lq8/r/A/cHF+8LK+sPQ+cXV98bb9sfg9Mjm88rs8svy8Mz378387s7/7s7/", // Cherry Blossom
            "/5IA/5QE/5YI/5kL/5sP/50T/58X/6Eb/6Mf/6Yj/6gn/6or/6wv/68z/7E2/7M6/7Y+/7hC/7pG/71J/79N/8FR/8NV/8VZ/8dd/8ph/8xl/85p/9Jz/9mJ/+Cl/+a1/+uu/+2c/++L/+2D/+p+/+Z5/+N0/+Bw/9xr/9lm/9Vh/9Jc/89X/8tS/8hN/8VI/8FE/74//7s6/7c1/7Qx/7As/60n/6oi/6Yd/6MY/58T/5wO/5kK/5UF/5IA/5IA", // Golden
            "/7y8/8K8/8i8/868/9S8/9q8/+G8/+e8/+28//O8//m8/v68+f+88/+87f+86P+84f+82/+81f+8z/+8yf+8w/+8vf+8vP/BvP/HvP/NvP/TvP/avP/gvP/mvP/svP/yvP/4vP//vPn/vPP/vOz/vOX/vN//vNj/vNL/vMz/vMX/vL//v7z/xrz/zLz/0rz/2rz/4Lz/5rz/7bz/87z/+rz//7z+/7z4/7zx/7zr/7zk/7ze/7zX/7zR/7zK/7y8", // Pastel Rainbow
            "MgAAMgUAMgkAMg4AMhIAMhcAMhsAMiAAMiUAMioAMi4AMTIALTIAKDIAJDIAHzIAGjIAFTIAETIADDIABzIAAzIAADICADIGADILADIQADIUADIZADIeADIiADInADIrADIwAC8yACsyACYyACEyABwyABgyABMyAA0yAAkyAAQyAQEyBQAyCgAyDwAyEwAyGQAyHgAyIgAyJwAyLAAyMQAyMgAvMgAqMgAlMgAgMgAbMgAWMgASMgANMgAAMgAA", // Dark Rainbow
            "//Qz//VK//Zg//h3//mO//qk//u7//3S//7o////9O366dr13sjv07Xqx6PlvJDgsX7apmvVm1nQik+5eUWiZzuLVjF0RShcNB5FIhQuEQoXAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEQoXIhQuNB5FRShcVjF0ZzuLeUWiik+5m1nQpmvVsX7avJDgx6Pl07Xq3sjv6dr19O36//////7o//3S//u7//qk//mO//h3//Zg//VK//Qz"  // Non-binary
        };

        // Decoded gradient color arrays (lazy initialized)
        private static byte[][,]? _decodedGradients = null;
        private static byte[][,] DecodedGradients
        {
            get
            {
                if (_decodedGradients == null)
                {
                    _decodedGradients = new byte[GradientPresetData.Length][,];
                    for (int i = 0; i < GradientPresetData.Length; i++)
                    {
                        var arr = Convert.FromBase64String(GradientPresetData[i]);
                        var arr2 = new byte[arr.Length / 3, 3];
                        for (var j = 0; j < arr.Length; j += 3)
                        {
                            arr2[j / 3, 0] = arr[j];
                            arr2[j / 3, 1] = arr[j + 1];
                            arr2[j / 3, 2] = arr[j + 2];
                        }
                        _decodedGradients[i] = arr2;
                    }
                }
                return _decodedGradients;
            }
        }

        // Animation timer for preview
        private static readonly System.Diagnostics.Stopwatch AnimationTimer = System.Diagnostics.Stopwatch.StartNew();
        private string advancedCharacterMacroText = "";

        public CharacterForm(Plugin plugin, UIStyles uiStyles)
        {
            this.plugin = plugin;
            this.uiStyles = uiStyles;
        }

        public void CloseAddCharacterWindow()
        {
            plugin.WindowState.IsAddCharacterWindowOpen = false;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier);

            uiStyles.PushFormStyle();

            try
            {
                float baseLines = 26f;

                float maxContentHeight = ImGui.GetTextLineHeightWithSpacing() * baseLines;
                float availableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * 2.5f;
                float scrollHeight = Math.Min(maxContentHeight, availableHeight);

                ImGui.BeginChild("CharacterFormScrollable", new Vector2(0, scrollHeight), true, ImGuiWindowFlags.AlwaysVerticalScrollbar);
                DrawCharacterFormContent(totalScale);
                ImGui.EndChild();
            }
            finally
            {
                uiStyles.PopFormStyle();
            }
        }

        private void DrawCharacterFormContent(float scale)
        {
            float labelWidth = 130 * scale;
            float inputWidth = 250 * scale;
            float inputOffset = 10 * scale;
            
            string tempName = currentCharacter.Data.Name;
            string tempPenumbra = currentCharacter.Data.PenumbraCollection;
            string tempGlamourer = currentCharacter.Data.GlamourerDesign;
            Vector3 tempColor = currentCharacter.Data.NameplateColor;
            string tempTag = currentCharacter.Data.Tag;
            Honorific tempHonorific = currentCharacter.Data.Honorific;

            // Character Name
            DrawFormField("Character Name*", labelWidth, inputWidth, inputOffset, () =>
            {
                ImGui.InputText("##CharacterName", ref tempName, 50);
                plugin.WindowState.CharacterNameFieldPos = ImGui.GetItemRectMin();
                plugin.WindowState.CharacterNameFieldSize = ImGui.GetItemRectSize();

                // Validate name on change
                SCSError? err = CharacterManager.ValidateName(tempName, currentCharacter, plugin.Characters);
                if (err != null)
                {
                    noErrors = false;
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2.0f);
                }
                
                if (err != null)
                {
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar();
                    
                    // Show error message
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f));
                    ImGui.TextWrapped(err.Message);
                    ImGui.PopStyleColor();
                    return;
                }
                currentCharacter.Data.Name = tempName;
                noErrors = true;
            }, "Enter your OC's name or nickname for profile here.", scale, null);

            // Character Alias field - only show when Name Sync is enabled
            if (plugin.Configuration.EnableNameReplacement || plugin.Configuration.EnableSharedNameReplacement)
            {
                string tempAlias = currentCharacter.Data.Alias;
                DrawFormField("Character Alias", labelWidth, inputWidth, inputOffset, () =>
                {
                    ImGui.InputTextWithHint("##CharacterAlias", "Leave empty to use Character Name", ref tempAlias, 100);

                    currentCharacter.Data.Alias = tempAlias;
                }, "Optional alias used for Name Sync.\nIf set, this name is displayed instead of Character Name.\nLeave empty to use the Character Name above.", scale);
            }

            ImGui.Separator();

            // Character Tags
            DrawFormField("Character Tags", labelWidth, inputWidth, inputOffset, () =>
            {
                ImGui.InputTextWithHint("##Tags", "e.g. Casual, Battle, Beach", ref tempTag, 100);

                currentCharacter.Data.Tag = tempTag;
            }, "You can assign multiple tags by separating them with commas.\nExamples: Casual, Favourites, Seasonal", scale);

            ImGui.Separator();

            // Nameplate Colour
            DrawFormField("Nameplate Color", labelWidth, inputWidth, inputOffset, () =>
            {
                ImGui.ColorEdit3("##NameplateColor", ref tempColor);

                currentCharacter.Data.NameplateColor = tempColor;
            }, "Affects your character's nameplate under their profile picture in Simple Character Select.", scale);

            ImGui.Separator();

            // Penumbra Collection
            DrawFormField("Penumbra Collection*", labelWidth, inputWidth, inputOffset, () =>
            {
                var penumbraOptions = plugin.IntegrationListProvider?.GetPenumbraCollections() ?? Array.Empty<string>();
                var currentPenumbra = plugin.IntegrationListProvider?.GetCurrentPenumbraCollection();

                if (AutocompleteCombo.Draw("##PenumbraCollection", ref tempPenumbra, penumbraOptions, inputWidth, "Select collection...", currentActive: currentPenumbra))
                {
                    plugin.WindowState.PenumbraFieldPos = ImGui.GetItemRectMin();
                    plugin.WindowState.PenumbraFieldSize = ImGui.GetItemRectSize();

                    currentCharacter.Data.PenumbraCollection = tempPenumbra;
                }
                else
                {
                    // Still track position even when not changed
                    plugin.WindowState.PenumbraFieldPos = ImGui.GetItemRectMin();
                    plugin.WindowState.PenumbraFieldSize = ImGui.GetItemRectSize();
                }
            }, "Select the Penumbra collection for this character. Right-click to clear.", scale);

            ImGui.Separator();

            // Glamourer Design
            DrawFormField("Glamourer Design*", labelWidth, inputWidth, inputOffset, () =>
            {
                var glamourerOptions = plugin.IntegrationListProvider?.GetGlamourerDesigns() ?? Array.Empty<string>();

                if (AutocompleteCombo.Draw("##GlamourerDesign", ref tempGlamourer, glamourerOptions, inputWidth, "Select design..."))
                {
                    plugin.WindowState.GlamourerFieldPos = ImGui.GetItemRectMin();
                    plugin.WindowState.GlamourerFieldSize = ImGui.GetItemRectSize();

                    currentCharacter.Data.GlamourerDesign = tempGlamourer;
                }
                else
                {
                    // Still track position even when not changed
                    plugin.WindowState.GlamourerFieldPos = ImGui.GetItemRectMin();
                    plugin.WindowState.GlamourerFieldSize = ImGui.GetItemRectSize();
                }
            }, "Select the Glamourer design for this character. Right-click to clear.\nYou can add additional designs later.", scale);

            ImGui.Separator();

            // Automation (if enabled)
            if (plugin.Configuration.EnableAutomations)
            {
                DrawAutomationField(labelWidth, inputWidth, inputOffset, scale);
                ImGui.Separator();
            }

            // Customize+ Profile
            DrawCustomizeField(labelWidth, inputWidth, inputOffset, scale);
            ImGui.Separator();

            // Honorific Section
            DrawHonorificSection(labelWidth, inputWidth, inputOffset, scale);
            ImGui.Separator();

            // Moodle Preset
            DrawMoodleField(labelWidth, inputWidth, inputOffset, scale);
            ImGui.Separator();

            // Idle Pose
            DrawIdlePoseField(labelWidth, inputWidth, inputOffset, scale);
            ImGui.Separator();

            // Assigned Gearset (only if enabled)
            if (plugin.Configuration.EnableGearsetAssignments)
            {
                DrawGearsetField(labelWidth, inputWidth, inputOffset, scale);
                ImGui.Separator();
            }

            // Image Selection
            DrawImageSelection(scale);
            ImGui.Separator();

            // Buttons!
            DrawActionButtons(scale);
        }

        private void DrawFormField(string label, float labelWidth, float inputWidth, float inputOffset,
                                 System.Action drawInput, string tooltip, float scale, System.Action? afterTooltip = null)
        {
            ImGui.SetCursorPosX(10 * scale);
            ImGui.Text(label);
            ImGui.SameLine(labelWidth);
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);

            drawInput();

            // Tooltip
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted(tooltip);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            // Optional content after tooltip
            afterTooltip?.Invoke();
        }
        
        private void DrawAutomationField(float labelWidth, float inputWidth, float inputOffset, float scale)
        {
            string tempCharacterAutomation = currentCharacter.Data.CharacterAutomation;

            DrawFormField("Glam. Automation", labelWidth, inputWidth, inputOffset, () =>
            {
                // Glamourer doesn't expose an IPC to get automation names, so use plain text input
                ImGui.SetNextItemWidth(inputWidth);
                if (ImGui.InputText("##Glam.Automation", ref tempCharacterAutomation, 100))
                {
                    currentCharacter.Data.CharacterAutomation = tempCharacterAutomation;
                }
            }, "Enter the name of a Glamourer Automation for this character.\nMust match the automation name EXACTLY as shown in Glamourer.\nDesign-level automations override this if both are set.", scale);
        }

        private void DrawCustomizeField(float labelWidth, float inputWidth, float inputOffset, float scale)
        {
            string tempCustomize = currentCharacter.Data.CustomizeProfile;

            DrawFormField("Customize+ Profile", labelWidth, inputWidth, inputOffset, () =>
            {
                var customizeOptions = plugin.IntegrationListProvider?.GetCustomizePlusProfiles() ?? Array.Empty<string>();
                var currentCustomize = plugin.IntegrationListProvider?.GetCurrentCustomizePlusProfile();

                if (AutocompleteCombo.Draw("##CustomizeProfile", ref tempCustomize, customizeOptions, inputWidth, "Select profile...", currentActive: currentCustomize))
                {
                    currentCharacter.Data.CustomizeProfile = tempCustomize;
                }
            }, "Select the Customize+ profile for this character. Right-click to clear.", scale);
        }

        private void DrawHonorificSection(float labelWidth, float inputWidth, float inputOffset, float scale)
        {
            ImGui.SetCursorPosX(10 * scale);
            ImGui.Text("Honorific Title");
            ImGui.SameLine();
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);

            bool changed = false;

            // Title input
            changed |= ImGui.InputText("##HonorificTitle", ref tempHonorificTitle, 50);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(80 * scale);
            if (ImGui.BeginCombo("##HonorificPlacement", tempHonorificPrefix))
            {
                foreach (var opt in new[] { "Prefix", "Suffix" })
                {
                    if (ImGui.Selectable(opt, tempHonorificPrefix == opt))
                    {
                        tempHonorificPrefix = opt;
                        tempHonorificSuffix = opt;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            // Text colour picker
            ImGui.SameLine();
            ImGui.SetNextItemWidth(40 * scale);
            changed |= ImGui.ColorEdit3("##HonorificColor", ref tempHonorificColor, ImGuiColorEditFlags.NoInputs);

            // Glow picker with gradient options (Honorific-style)
            ImGui.SameLine();
            changed |= DrawGlowPicker(scale);

            // Tooltip
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("This will set a forced title when you switch to this character.\nThe dropdown selects if the title appears above (prefix) or below (suffix) your name in-game.\nClick the glow color box to access gradient presets.\nUse the Honorific plug-in's 'Clear' button if you need to remove it.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            // Live preview to the right of tooltip
            if (!string.IsNullOrWhiteSpace(tempHonorificTitle))
            {
                ImGui.SameLine(0, 4 * scale);
                DrawHonorificPreview(scale);
            }

            if (changed)
            {
                UpdateHonorificData(currentCharacter);
            }
        }

        /// <summary>
        /// Draws a glow color picker with gradient options (Honorific-style)
        /// </summary>
        private bool DrawGlowPicker(float scale)
        {
            bool modified = false;
            long animOffset = AnimationTimer.ElapsedMilliseconds;

            // When a gradient is selected, show animated color; otherwise show solid glow
            Vector3 displayColor;
            if (tempHonorificGradientSet.HasValue)
            {
                if (tempHonorificGradientSet.Value == -1)
                {
                    // Two-colour gradient: alternate between the two colours
                    displayColor = GetTwoColourPreviewColor(tempHonorificGlow, tempHonorificColor3, animOffset);
                }
                else
                {
                    displayColor = GetGradientPreviewColor(tempHonorificGradientSet.Value, animOffset);
                }
            }
            else
            {
                displayColor = tempHonorificGlow;
            }

            // Use ColorButton to match the text color picker size exactly
            if (ImGui.ColorButton("##GlowPickerBtn", new Vector4(displayColor, 1f), ImGuiColorEditFlags.NoTooltip))
            {
                ImGui.OpenPopup("##GlowPickerPopup");
            }

            // Tooltip
            if (ImGui.IsItemHovered())
            {
                if (tempHonorificGradientSet.HasValue)
                {
                    if (tempHonorificGradientSet.Value == -1)
                        ImGui.SetTooltip($"Two Colour Gradient ({tempHonorificAnimationStyle ?? "Wave"})");
                    else
                        ImGui.SetTooltip($"{GradientPresetNames[tempHonorificGradientSet.Value]} ({tempHonorificAnimationStyle ?? "Wave"})");
                }
                else
                    ImGui.SetTooltip("Glow (click for gradients)");
            }

            // The popup with gradient options
            if (ImGui.BeginPopup("##GlowPickerPopup"))
            {
                float popupWidth = 220 * scale;

                // Default Glow option with color picker
                ImGui.Text("Solid Glow:");
                ImGui.SameLine();
                if (ImGui.ColorEdit3("##GlowColorPicker", ref tempHonorificGlow, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel))
                {
                    tempHonorificGradientSet = null;
                    tempHonorificAnimationStyle = null;
                    modified = true;
                }
                ImGui.SameLine();
                if (ImGui.SmallButton("Use##UseGlow"))
                {
                    tempHonorificGradientSet = null;
                    tempHonorificAnimationStyle = null;
                    modified = true;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.Separator();

                // Gate animated gradients behind supporter acknowledgment
                if (plugin.Configuration.HasAcknowledgedHonorificSupport)
                {
                    // Nested combo for gradient selection (like Honorific)
                    string gradientLabel = tempHonorificGradientSet.HasValue
                        ? (tempHonorificGradientSet.Value == -1 ? "Two Colour Gradient" : GradientPresetNames[tempHonorificGradientSet.Value])
                        : "Select Gradient...";

                    ImGui.SetNextItemWidth(popupWidth);
                    if (ImGui.BeginCombo("##GradientSelect", gradientLabel, ImGuiComboFlags.HeightLargest))
                    {
                        // Tab bar for animation styles
                        if (ImGui.BeginTabBar("##GradAnimTabs"))
                        {
                            foreach (var animStyle in new[] { "Wave", "Pulse", "Static" })
                            {
                                if (ImGui.BeginTabItem(animStyle))
                                {
                                    // Child region for scrolling
                                    float childHeight = Math.Min(180 * scale, (GradientPresetNames.Length + 1) * ImGui.GetTextLineHeightWithSpacing());
                                    if (ImGui.BeginChild($"##Presets{animStyle}", new Vector2(popupWidth - 16 * scale, childHeight)))
                                    {
                                        var drawList = ImGui.GetWindowDrawList();

                                        // Two Colour Gradient option at top
                                        bool isTwoColourSelected = tempHonorificGradientSet == -1 && tempHonorificAnimationStyle == animStyle;
                                        if (ImGui.Selectable("Two Colour Gradient", isTwoColourSelected, ImGuiSelectableFlags.DontClosePopups))
                                        {
                                            tempHonorificGradientSet = -1;
                                            tempHonorificAnimationStyle = animStyle;
                                            modified = true;
                                            ImGui.CloseCurrentPopup();  // Close inner combo only
                                        }

                                        // Preset gradients
                                        for (int i = 0; i < GradientPresetNames.Length; i++)
                                        {
                                            bool isSelected = tempHonorificGradientSet == i && tempHonorificAnimationStyle == animStyle;

                                            var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing());
                                            var cursorPos = ImGui.GetCursorScreenPos();

                                            if (ImGui.Selectable($"##Preset{animStyle}{i}", isSelected, ImGuiSelectableFlags.DontClosePopups, selectableSize))
                                            {
                                                tempHonorificGradientSet = i;
                                                tempHonorificAnimationStyle = animStyle;
                                                modified = true;
                                                ImGui.CloseCurrentPopup();
                                            }

                                            // Draw the preset name with animated gradient effect
                                            var textPos = cursorPos + ImGui.GetStyle().FramePadding;
                                            DrawGradientTextForPicker(drawList, textPos, GradientPresetNames[i], i, animStyle);
                                        }
                                    }
                                    ImGui.EndChild();
                                    ImGui.EndTabItem();
                                }
                            }
                            ImGui.EndTabBar();
                        }
                        ImGui.EndCombo();
                    }

                    // Show animated preview of selected gradient (below the combo, still in popup)
                    if (tempHonorificGradientSet.HasValue)
                    {
                        var previewText = tempHonorificGradientSet.Value == -1
                            ? "Two Colour Gradient"
                            : GradientPresetNames[tempHonorificGradientSet.Value];

                        var previewPos = ImGui.GetCursorScreenPos();
                        var drawList = ImGui.GetWindowDrawList();

                        // Reserve space and draw preview
                        ImGui.Dummy(new Vector2(popupWidth, ImGui.GetTextLineHeightWithSpacing()));
                        DrawGradientTextForPicker(drawList, previewPos, previewText,
                            tempHonorificGradientSet.Value, tempHonorificAnimationStyle ?? "Wave");
                    }

                    // Two colour pickers (shown below combo when two-colour is selected)
                    if (tempHonorificGradientSet == -1)
                    {
                        if (ImGui.ColorEdit3("##TwoColour1", ref tempHonorificGlow, ImGuiColorEditFlags.NoInputs))
                        {
                            modified = true;
                        }
                        ImGui.SameLine();
                        if (ImGui.ColorEdit3("Colours##TwoColour2", ref tempHonorificColor3, ImGuiColorEditFlags.NoInputs))
                        {
                            modified = true;
                        }
                    }
                }
                else
                {
                    // Show message when supporter acknowledgment not enabled
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.65f, 1.0f));
                    ImGui.TextWrapped("Enable in Settings > Visual Settings to use animated gradients.");
                    ImGui.PopStyleColor();
                }

                ImGui.EndPopup();
            }

            return modified;
        }

        /// <summary>
        /// Gets a preview colour for the two-colour gradient (alternates between the two)
        /// </summary>
        private Vector3 GetTwoColourPreviewColor(Vector3 color1, Vector3 color2, long animOffset)
        {
            // Simple wave between the two colours
            float t = (float)(Math.Sin(animOffset / 500.0) * 0.5 + 0.5);
            return Vector3.Lerp(color1, color2, t);
        }

        /// <summary>
        /// Draws text with animated gradient for the picker preview
        /// </summary>
        private void DrawGradientTextForPicker(ImDrawListPtr drawList, Vector2 pos, string text, int gradientSet, string animStyle)
        {
            long animOffset = AnimationTimer.ElapsedMilliseconds;

            float charX = pos.X;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                string charStr = c.ToString();

                // For two-colour gradient, pass the current colours
                Vector3 charColor = GetGradientColor(gradientSet, i, animOffset, 5, animStyle, text.Length,
                    gradientSet == -1 ? tempHonorificGlow : null,
                    gradientSet == -1 ? tempHonorificColor3 : null);
                uint colorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(charColor, 1f));

                drawList.AddText(new Vector2(charX, pos.Y), colorU32, charStr);
                charX += ImGui.CalcTextSize(charStr).X;
            }
        }

        private void DrawMoodleField(float labelWidth, float inputWidth, float inputOffset, float scale)
        {
            DrawFormField("Moodle Preset", labelWidth, inputWidth, inputOffset, () =>
            {
                var moodleOptions = plugin.IntegrationListProvider?.GetMoodlesPresets() ?? Array.Empty<string>();

                if (AutocompleteCombo.Draw("##MoodlePreset", ref tempMoodlePreset, moodleOptions, inputWidth, "Select preset..."))
                {
                    currentCharacter.Data.MoodlePreset = tempMoodlePreset;
                }
            }, "Select the Moodle preset for this character. Right-click to clear.", scale);
        }

        private void DrawIdlePoseField(float labelWidth, float inputWidth, float inputOffset, float scale)
        {
            ImGui.SetCursorPosX(10 * scale);
            ImGui.Text("Idle Pose");
            ImGui.SameLine();
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);

            string[] poseOptions = { "None", "0", "1", "2", "3", "4", "5", "6" };
            byte storedIndex = IsEditWindowOpen
                ? plugin.Characters[selectedCharacterIndex].Data.IdlePoseIndex
                : plugin.NewCharacterIdlePoseIndex;

            int dropdownIndex = storedIndex == 7 ? 0 : storedIndex + 1;

            if (ImGui.BeginCombo("##IdlePose", poseOptions[dropdownIndex]))
            {
                for (int i = 0; i < poseOptions.Length; i++)
                {
                    bool selected = i == dropdownIndex;
                    if (ImGui.Selectable(poseOptions[i], selected))
                    {
                        byte newIndex = (byte)(i == 0 ? 7 : i - 1);
                        byte currentIndex = IsEditWindowOpen
                            ? plugin.Characters[selectedCharacterIndex].Data.IdlePoseIndex
                            : plugin.NewCharacterIdlePoseIndex;

                        if (currentIndex != newIndex)
                        {
                            currentCharacter.Data.IdlePoseIndex = newIndex;
                        }
                        
                    }
                    if (selected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // Tooltip
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextUnformatted("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("Sets your character's idle pose (0–6).\nChoose 'None' if you don't want Simple Character Select to change your idle.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }
        
        // TODO
        private void DrawGearsetField(float labelWidth, float inputWidth, float inputOffset, float scale)
        {
            ImGui.SetCursorPosX(10 * scale);
            ImGui.Text("Assigned Gearset");
            ImGui.SameLine();
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);

            // Get available gearsets
            //var gearsets = plugin.GetPlayerGearsets();

            // Get current value
            int? currentGearset = currentCharacter.Data.AssignedGearset;

            // Build display text for current selection
            string currentDisplay = "None";
            if (currentGearset.HasValue)
            {
                // var matchingGearset = gearsets.FirstOrDefault(g => g.Number == currentGearset.Value);
                // if (matchingGearset.Number > 0)
                // {
                //     //currentDisplay = plugin.GetGearsetDisplayName(matchingGearset.Number, matchingGearset.JobId, matchingGearset.Name);
                // }
                // else
                // {
                //     currentDisplay = $"Gearset {currentGearset.Value}";
                // }
            }

            if (ImGui.BeginCombo("##AssignedGearset", currentDisplay))
            {
                // "None" option
                if (ImGui.Selectable("None", !currentGearset.HasValue))
                {
                    currentCharacter.Data.AssignedGearset = null;
                }
                if (!currentGearset.HasValue)
                    ImGui.SetItemDefaultFocus();

                // Gearset options
                // foreach (var gearset in gearsets)
                // {
                //     string displayName = plugin.GetGearsetDisplayName(gearset.Number, gearset.JobId, gearset.Name);
                //     bool isSelected = currentGearset.HasValue && currentGearset.Value == gearset.Number;
                //
                //     if (ImGui.Selectable(displayName, isSelected))
                //     {
                //         if (IsEditWindowOpen)
                //             editedCharacterGearset = gearset.Number;
                //         else
                //             plugin.NewCharacterGearset = gearset.Number;
                //     }
                //     if (isSelected)
                //         ImGui.SetItemDefaultFocus();
                // }

                ImGui.EndCombo();
            }

            // Tooltip
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextUnformatted("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300 * scale);
                ImGui.TextUnformatted("Automatically switch to this gearset when applying this character.\nChoose 'None' to not change gearsets.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        private void DrawImageSelection(float scale)
        {
            if (ImGui.Button("Choose Image", new Vector2(0, 25 * scale)))
            {
                plugin.OpenFilePicker(
                    "Select Character Image",
                    "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|PNG files (*.png)|*.png",
                    (selectedPath) =>
                    {
                        lock (this)
                        {
                            pendingImagePath = selectedPath;
                        }
                    }
                );
            }

            // Apply pending image
            if (pendingImagePath != null)
            {
                lock (this)
                {
                    currentCharacter.Data.ImagePath = pendingImagePath;

                    pendingImagePath = null;
                }
            }

            // Show image preview
            DrawImagePreview(scale);
        }

        private void DrawImagePreview(float scale)
        {
            string pluginDirectory = plugin.PluginDirectory;
            string defaultImagePath = Path.Combine(pluginDirectory, "Assets", "Default.png");

            string? imagePath = currentCharacter.Data.ImagePath;
            string finalImagePath = !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath)
                ? imagePath
                : defaultImagePath;

            if (!string.IsNullOrEmpty(finalImagePath) && File.Exists(finalImagePath))
            {
                var texture = Plugin.TextureProvider.GetFromFile(finalImagePath).GetWrapOrDefault();
                if (texture != null)
                {
                    float originalWidth = texture.Width;
                    float originalHeight = texture.Height;
                    float maxSize = 100f * scale;

                    float aspectRatio = originalWidth / originalHeight;
                    float displayWidth, displayHeight;

                    if (aspectRatio > 1)
                    {
                        displayWidth = maxSize;
                        displayHeight = maxSize / aspectRatio;
                    }
                    else
                    {
                        displayHeight = maxSize;
                        displayWidth = maxSize * aspectRatio;
                    }

                    var cursorPos = ImGui.GetCursorScreenPos();
                    var imageEnd = cursorPos + new Vector2(displayWidth, displayHeight);

                    uiStyles.DrawGlowingBorder(
                        cursorPos - new Vector2(2 * scale, 2 * scale),
                        imageEnd + new Vector2(2 * scale, 2 * scale),
                        new Vector3(0.5f, 0.5f, 0.5f),
                        0.3f,
                        false,
                        scale
                    );

                    ImGui.Image((ImTextureID)texture.Handle, new Vector2(displayWidth, displayHeight));
                }
                else
                {
                    ImGui.Text($"Failed to load image: {Path.GetFileName(finalImagePath)}");
                }
            }
            else
            {
                ImGui.Text("No Image Available");
            }
        }

        private float GetSafeScale(float baseScale)
        {
            return Math.Clamp(baseScale, 0.3f, 5.0f); // Prevent extreme scaling
        }

        private void DrawActionButtons(float scale)
        {
            string tempName = currentCharacter.Data.Name;
            string tempPenumbra = currentCharacter.Data.PenumbraCollection;
            string tempGlamourer = currentCharacter.Data.GlamourerDesign;

            bool canSaveCharacter = !string.IsNullOrWhiteSpace(tempName) &&
                                   !string.IsNullOrWhiteSpace(tempPenumbra) &&
                                   !string.IsNullOrWhiteSpace(tempGlamourer) &&
                                   noErrors;

            uiStyles.PushDarkButtonStyle(scale);

            if (!canSaveCharacter)
                ImGui.BeginDisabled();

            if (ImGui.Button(IsEditWindowOpen ? "Save Changes" : "Save Character", new Vector2(0, 30 * scale)))
            {
                CharacterManager.SaveCharacter(selectedCharacterIndex, currentCharacter, plugin.Configuration, plugin.Logger);
                CloseForm();
            }

            plugin.WindowState.SaveButtonPos = ImGui.GetItemRectMin();
            plugin.WindowState.SaveButtonSize = ImGui.GetItemRectSize();

            if (!canSaveCharacter)
                ImGui.EndDisabled();

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(0, 30 * scale)))
            {
                CloseForm();
            }

            uiStyles.PopDarkButtonStyle();
        }

        private void UpdateHonorificData(Character character)
        {   
            character.Data.Honorific.Title = tempHonorificTitle;
            character.Data.Honorific.Prefix = tempHonorificPrefix;
            character.Data.Honorific.Suffix = tempHonorificSuffix;
            character.Data.Honorific.Color = tempHonorificColor;
            character.Data.Honorific.Glow = tempHonorificGlow;
            character.Data.Honorific.Color3 = tempHonorificGradientSet == -1 ? tempHonorificColor3 : null;;
            character.Data.Honorific.GradientSet = tempHonorificGradientSet;
            character.Data.Honorific.AnimationStyle = tempHonorificAnimationStyle;
        }

        /// <summary>
        /// Draws an animated preview of the Honorific title with the current settings in a dark container
        /// </summary>
        private void DrawHonorificPreview(float scale)
        {
            if (string.IsNullOrWhiteSpace(tempHonorificTitle))
                return;

            var textSize = ImGui.CalcTextSize(tempHonorificTitle);
            var padding = new Vector2(8 * scale, 4 * scale);
            var boxSize = textSize + padding * 2;

            // Draw dark background box
            var drawList = ImGui.GetWindowDrawList();
            var boxStart = ImGui.GetCursorScreenPos();
            var boxEnd = boxStart + boxSize;

            // Dark background with slight border
            drawList.AddRectFilled(boxStart, boxEnd, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1f)), 4f);
            drawList.AddRect(boxStart, boxEnd, ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)), 4f);

            // Text position inside the box
            var textPos = boxStart + padding;

            // Build SeString with proper color and glow
            SeString seString;
            if (tempHonorificGradientSet.HasValue)
            {
                // For gradients, build per-character SeString with animated colors
                // For two-colour gradient (-1), pass both colours
                seString = BuildGradientSeString(tempHonorificTitle, tempHonorificGradientSet.Value,
                    tempHonorificAnimationStyle ?? "Wave", tempHonorificColor,
                    tempHonorificGradientSet.Value == -1 ? tempHonorificGlow : null,
                    tempHonorificGradientSet.Value == -1 ? tempHonorificColor3 : null);
            }
            else
            {
                // Build SeString with solid color and glow
                seString = BuildColoredSeString(tempHonorificTitle, tempHonorificColor, tempHonorificGlow);
            }

            // Render using Dalamud's SeString renderer for smooth text
            ImGuiHelpers.SeStringWrapped(seString.Encode(), new SeStringDrawParams
            {
                Color = 0xFFFFFFFF,
                WrapWidth = float.MaxValue,
                TargetDrawList = drawList,
                Font = UiBuilder.DefaultFont,
                FontSize = UiBuilder.DefaultFontSizePx,
                ScreenOffset = textPos
            });

            // Reserve space for the box
            ImGui.Dummy(boxSize);
        }

        /// <summary>
        /// Builds an SeString with solid color and glow effect
        /// </summary>
        private SeString BuildColoredSeString(string text, Vector3 color, Vector3 glow)
        {
            var builder = new SeStringBuilder();

            // Add text color
            builder.PushColorRgba(new Vector4(color, 1f));

            // Add edge/glow color
            builder.PushEdgeColorRgba(new Vector4(glow, 1f));

            builder.Append(text);

            builder.PopEdgeColor();
            builder.PopColor();

            return SeString.Parse(builder.GetViewAsSpan());
        }

        /// <summary>
        /// Builds an SeString with animated gradient glow effect
        /// </summary>
        private SeString BuildGradientSeString(string text, int gradientSet, string animStyle, Vector3 textColor,
            Vector3? twoColourFirst = null, Vector3? twoColourSecond = null)
        {
            var builder = new SeStringBuilder();
            long animOffset = AnimationTimer.ElapsedMilliseconds;

            // Add base text color
            builder.PushColorRgba(new Vector4(textColor, 1f));

            for (int i = 0; i < text.Length; i++)
            {
                // Calculate gradient color for this character
                Vector3 glowColor = GetGradientColor(gradientSet, i, animOffset, 5, animStyle, text.Length, twoColourFirst, twoColourSecond);

                // Push edge color for this character
                builder.PushEdgeColorRgba(new Vector4(glowColor, 1f));
                builder.Append(text[i].ToString());
                builder.PopEdgeColor();
            }

            builder.PopColor();

            return SeString.Parse(builder.GetViewAsSpan());
        }

        /// <summary>
        /// Gets a color from the gradient using Honorific's exact algorithm
        /// </summary>
        private Vector3 GetGradientColor(int gradientSet, int charIndex, long rawMilliseconds, int throttle, string animStyle,
            int textLength = 16, Vector3? twoColourFirst = null, Vector3? twoColourSecond = null)
        {
            // Handle two-colour gradient (gradientSet == -1)
            if (gradientSet == -1 && twoColourFirst.HasValue && twoColourSecond.HasValue)
            {
                return GetTwoColourGradientColor(twoColourFirst.Value, twoColourSecond.Value,
                    charIndex, rawMilliseconds, throttle, animStyle, textLength);
            }

            if (gradientSet < 0 || gradientSet >= DecodedGradients.Length)
                return new Vector3(1f, 1f, 1f);

            var colors = DecodedGradients[gradientSet];
            var colorCount = colors.GetLength(0);

            // Honorific's exact timing: divide by 15 first, then by throttle
            var animationOffset = rawMilliseconds / 15;

            int index;
            if (animStyle == "Pulse")
            {
                // Pulse: whole text uses same color (charIndex multiplier = 0)
                index = (int)((animationOffset / throttle) % colorCount);
            }
            else if (animStyle == "Static")
            {
                // Static: spread gradient across text length, no animation
                index = (int)Math.Round(charIndex / (float)Math.Max(1, textLength) * colorCount) % colorCount;
            }
            else // Wave
            {
                // Wave: position based on character index + time (charIndex multiplier = 1)
                index = (int)((animationOffset / throttle + charIndex) % colorCount);
            }

            return new Vector3(
                colors[index, 0] / 255f,
                colors[index, 1] / 255f,
                colors[index, 2] / 255f
            );
        }

        /// <summary>
        /// Gets a color for two-colour gradient animation (matching Honorific's GradientSystem.GetDualColourStyle)
        /// </summary>
        private Vector3 GetTwoColourGradientColor(Vector3 color1, Vector3 color2, int charIndex,
            long rawMilliseconds, int throttle, string animStyle, int textLength)
        {
            // Honorific generates a gradient: color1 -> fade -> color2 -> fade -> color1
            // We simulate this with 64 steps like Honorific does
            const int GradientSteps = 64;

            var animationOffset = rawMilliseconds / 15;

            int index;
            if (animStyle == "Pulse")
            {
                // Pulse: whole text uses same color
                index = (int)((animationOffset / throttle) % GradientSteps);
            }
            else if (animStyle == "Static")
            {
                // Static: spread gradient across text, no animation
                index = (int)Math.Round(charIndex / (float)Math.Max(1, textLength) * GradientSteps) % GradientSteps;
            }
            else // Wave
            {
                // Wave: position based on character index + time
                index = (int)((animationOffset / throttle + charIndex) % GradientSteps);
            }

            // Calculate interpolation: 0->32 goes color1->color2, 32->64 goes color2->color1
            float t;
            if (index < GradientSteps / 2)
            {
                t = index / (float)(GradientSteps / 2);  // 0 to 1
            }
            else
            {
                t = 1f - ((index - GradientSteps / 2) / (float)(GradientSteps / 2));  // 1 to 0
            }

            return Vector3.Lerp(color1, color2, t);
        }

        /// <summary>
        /// Gets a representative color from a gradient preset (for button preview)
        /// </summary>
        private Vector3 GetGradientPreviewColor(int preset, long rawMilliseconds)
        {
            if (preset < 0 || preset >= DecodedGradients.Length)
                return new Vector3(1f, 1f, 1f);

            var colors = DecodedGradients[preset];
            var colorCount = colors.GetLength(0);
            // Match Honorific timing: /15 then /5 (throttle)
            var index = (int)((rawMilliseconds / 15 / 5) % colorCount);

            return new Vector3(
                colors[index, 0] / 255f,
                colors[index, 1] / 255f,
                colors[index, 2] / 255f
            );
        }
        private string PatchMacroLine(string existing, string prefix, string replacement)
        {
            var lines = existing.Split('\n').ToList();
            var idx = lines.FindIndex(l => l.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (idx >= 0)
            {
                lines[idx] = replacement;
            }
            else
            {
                int insertPosition = GetProperInsertPosition(lines, prefix);
                lines.Insert(insertPosition, replacement);
            }

            return string.Join("\n", lines);
        }

        private int GetProperInsertPosition(List<string> lines, string prefix)
        {
            var order = new[]
            {
                "/penumbra collection",
                "/penumbra bulktag disable",
                "/penumbra bulktag enable",
                "/glamour apply no clothes",
                "/glamour apply",
                "/glamour automation enable",
                "/customize profile disable",
                "/customize profile enable",
                "/honorific force clear",
                "/honorific force set",
                "/moodle remove",
                "/moodle apply",
                "/sidle",
                "/penumbra redraw"
            };

            int targetOrder = Array.FindIndex(order, o => prefix.StartsWith(o, StringComparison.OrdinalIgnoreCase));
            if (targetOrder == -1) return lines.Count;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].TrimStart();
                int lineOrder = Array.FindIndex(order, o => line.StartsWith(o, StringComparison.OrdinalIgnoreCase));

                if (lineOrder > targetOrder || lineOrder == -1)
                {
                    return i;
                }
            }

            return lines.Count;
        }
        
        private void CloseForm()
        {
            IsEditWindowOpen = false;
            CloseAddCharacterWindow();
            
            isSecretMode = false;
            isAdvancedModeCharacter = false;
            ResetFields();
        }

        public void ResetFields()
        {
            currentCharacter.ResetEdits();
        }

        public void InitCreateCharacterWindow()
        {
            currentCharacter = new Character();
            selectedCharacterIndex = -1;
            IsEditWindowOpen = false;
        }

        public void OpenEditCharacterWindow(int index)
        {
            if (index < 0 || index >= plugin.Characters.Count)
                return;

            selectedCharacterIndex = index;
            var character = plugin.Characters[index];
            currentCharacter = character;
            
            OpenCharacterWindow(character);
            
            IsEditWindowOpen = true;
        }

        public void OpenCharacterWindow(Character character)
        {
            string pluginDirectory = plugin.PluginDirectory;
            string defaultImagePath = Path.Combine(pluginDirectory, "Assets", "Default.png");
            
            // Copy to temp fields
            tempHonorificTitle = character.Data.Honorific.Title;
            tempHonorificPrefix = character.Data.Honorific.Prefix;
            tempHonorificSuffix = character.Data.Honorific.Suffix;
            tempHonorificColor = character.Data.Honorific.Color;
            tempHonorificGlow = character.Data.Honorific.Glow;
            tempHonorificColor3 = character.Data.Honorific.Color3 ?? new Vector3(0.5f, 0.5f, 1.0f);
            tempHonorificGradientSet = character.Data.Honorific.GradientSet;
            tempHonorificAnimationStyle = character.Data.Honorific.AnimationStyle;
            tempMoodlePreset = character.Data.MoodlePreset;
        }
    }
}
