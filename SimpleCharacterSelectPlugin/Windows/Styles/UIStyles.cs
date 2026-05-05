using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace SimpleCharacterSelectPlugin.Windows.Styles
{
    public class UIStyles
    {
        private Plugin plugin;
        private int styleStackCount = 0;
        private int colorStackCount = 0;
        private bool pushedPreDrawWindowBg = false;

        public UIStyles(Plugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Called in PreDraw. Only pushes WindowBg for Custom theme to allow window frame customization.
        /// Default/Seasonal themes are completely unaffected.
        /// </summary>
        public void PushCustomWindowBgIfNeeded()
        {
            pushedPreDrawWindowBg = false;

            var customTheme = plugin.Configuration.CustomTheme;
            if (customTheme.ColorOverrides.TryGetValue("color.windowBg", out var packed) && packed.HasValue)
            {
                var color = CustomThemeDefinitions.UnpackColor(packed.Value);
                ImGui.PushStyleColor(ImGuiCol.WindowBg, color);
                pushedPreDrawWindowBg = true;
            }
        }

        /// <summary>
        /// Called in PostDraw. Pops WindowBg if it was pushed in PreDraw.
        /// </summary>
        public void PopCustomWindowBgIfNeeded()
        {
            if (pushedPreDrawWindowBg)
            {
                ImGui.PopStyleColor(1);
                pushedPreDrawWindowBg = false;
            }
        }

        public void PushMainWindowStyle()
        {
            float scale = ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier;
            colorStackCount += ThemeHelper.PushDefaultThemeColors();
            styleStackCount += ThemeHelper.PushThemeStyleVars();
        }

        public void PopMainWindowStyle()
        {
            ImGui.PopStyleVar(styleStackCount);
            ImGui.PopStyleColor(colorStackCount);
            styleStackCount = 0;
            colorStackCount = 0;
        }

        public void DrawGlowingBorder(Vector2 min, Vector2 max, Vector3 color, float intensity = 1.0f, bool isHovered = false, float scale = 1.0f)
        {
            var drawList = ImGui.GetWindowDrawList();
            float finalScale = ImGuiHelpers.GlobalScale * scale;

            // Convert colour to ImGui format
            var glowColor = new Vector4(color.X, color.Y, color.Z, intensity);
            uint glowColorU32 = ImGui.GetColorU32(glowColor);

            // Draw multiple borders for glow effect - scale thickness and radius
            float thickness = (isHovered ? 2.0f : 1.5f) * finalScale;
            float cornerRadius = 12.0f * finalScale;

            // Outer glow
            for (int i = 0; i < 5; i++)
            {
                float alpha = (0.4f - i * 0.08f) * intensity;
                if (alpha <= 0) break;

                uint outerColor = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, alpha));
                float offset = (i + 1) * 1.5f * finalScale;

                drawList.AddRect(
                    min - new Vector2(offset, offset),
                    max + new Vector2(offset, offset),
                    outerColor,
                    cornerRadius + offset,
                    ImDrawFlags.RoundCornersAll,
                    1.0f * finalScale
                );
            }

            // Inner bright border
            if (isHovered)
            {
                uint brightColor = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, intensity * 0.8f));
                drawList.AddRect(
                    min + new Vector2(1 * finalScale, 1 * finalScale),
                    max - new Vector2(1 * finalScale, 1 * finalScale),
                    brightColor,
                    cornerRadius - (1 * finalScale),
                    ImDrawFlags.RoundCornersAll,
                    1.0f * finalScale
                );
            }

            // Main border
            drawList.AddRect(min, max, glowColorU32, cornerRadius, ImDrawFlags.RoundCornersAll, thickness);
        }

        public void PushDarkButtonStyle(float scale = 1.0f)
        {
            float finalScale = ImGuiHelpers.GlobalScale * scale;
            
            // Dark button styling
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.9f, 0.9f, 1.0f));

            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f * finalScale); // Scale button rounding

            colorStackCount += 4;
            styleStackCount += 2;
        }

        public void PopDarkButtonStyle()
        {
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(4);
            styleStackCount -= 2;
            colorStackCount -= 4;
        }

        public bool IconButton(string icon, string tooltip, Vector2? size = null, float scale = 1.0f)
        {
            return IconButtonWithColor(icon, tooltip, size, scale, null);
        }

        public bool IconButtonWithColor(string icon, string tooltip, Vector2? size = null, float scale = 1.0f, Vector4? iconColor = null)
        {
            float finalScale = ImGuiHelpers.GlobalScale * scale;

            // Calculate icon size
            ImGui.PushFont(UiBuilder.IconFont);
            var iconSize = ImGui.CalcTextSize(icon);
            ImGui.PopFont();

            // Determine button size
            Vector2 buttonSize;
            if (size.HasValue)
            {
                buttonSize = new Vector2(size.Value.X * finalScale, size.Value.Y * finalScale);
            }
            else
            {
                // Default: icon size + padding
                var padding = ImGui.GetStyle().FramePadding;
                buttonSize = new Vector2(iconSize.X + padding.X * 2, iconSize.Y + padding.Y * 2);
            }

            // Get button position before creating it
            var buttonPos = ImGui.GetCursorScreenPos();

            // Create invisible button for interaction
            var buttonId = $"##iconbtn_{icon}_{buttonPos.X}_{buttonPos.Y}";
            bool result = ImGui.InvisibleButton(buttonId, buttonSize);
            bool isHovered = ImGui.IsItemHovered();
            bool isActive = ImGui.IsItemActive();

            // Draw button background
            var drawList = ImGui.GetWindowDrawList();
            var buttonEnd = buttonPos + buttonSize;

            Vector4 bgColor;
            if (isActive)
                bgColor = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive];
            else if (isHovered)
                bgColor = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
            else
                bgColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];

            drawList.AddRectFilled(buttonPos, buttonEnd, ImGui.ColorConvertFloat4ToU32(bgColor), ImGui.GetStyle().FrameRounding);

            // Draw centered icon
            var iconPos = buttonPos + (buttonSize - iconSize) * 0.5f;
            var textColor = iconColor ?? ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            ImGui.PushFont(UiBuilder.IconFont);
            drawList.AddText(iconPos, ImGui.ColorConvertFloat4ToU32(textColor), icon);
            ImGui.PopFont();

            if (isHovered && !string.IsNullOrEmpty(tooltip))
            {
                ImGui.SetTooltip(tooltip);
            }

            return result;
        }

        public void DrawGradientBackground(Vector2 min, Vector2 max, Vector4 topColor, Vector4 bottomColor)
        {
            var drawList = ImGui.GetWindowDrawList();

            uint topColorU32 = ImGui.GetColorU32(topColor);
            uint bottomColorU32 = ImGui.GetColorU32(bottomColor);

            drawList.AddRectFilledMultiColor(
                min, max,
                topColorU32, topColorU32,
                bottomColorU32, bottomColorU32
            );
        }

        public void PushNameplateStyle(float scale = 1.0f)
        {
            // Nameplate styling with transparency
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0.85f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 0.0f); // Nameplates typically don't have rounding
            ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0.0f);

            colorStackCount++;
            styleStackCount += 2;
        }

        public void PopNameplateStyle()
        {
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(1);
            styleStackCount -= 2;
            colorStackCount--;
        }

        public void DrawPaginationDots(int currentPage, int totalPages, Vector2 position, float scale = 1.0f)
        {
            if (totalPages <= 1) return;

            var drawList = ImGui.GetWindowDrawList();
            float finalScale = ImGuiHelpers.GlobalScale * scale;
            float dotSize = 8.0f * finalScale; 
            float spacing = 16.0f * finalScale; 

            for (int i = 0; i < totalPages; i++)
            {
                Vector2 dotPos = position + new Vector2(i * spacing, 0);
                uint color = i == currentPage
                    ? ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f))
                    : ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 0.7f));

                drawList.AddCircleFilled(dotPos, dotSize / 2, color);

                // Glow effect for active dot
                if (i == currentPage)
                {
                    drawList.AddCircle(dotPos, dotSize / 2 + (2 * finalScale),
                        ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 0.5f)), 0, 1.0f * finalScale);
                }
            }
        }

        public void PushFormStyle()
        {
            float scale = ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier;

            // Form-specific styling
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.16f, 0.16f, 0.16f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.22f, 0.22f, 0.22f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.28f, 0.28f, 0.28f, 0.9f));

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f * ImGuiHelpers.GlobalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6 * scale, 4 * scale));

            colorStackCount += 3;
            styleStackCount += 2;
        }

        public void PopFormStyle()
        {
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(3);
            styleStackCount -= 2;
            colorStackCount -= 3;
        }
    }

    public static class SeStringExtensions
    {
        public static SeStringBuilder AddColored(this SeStringBuilder builder, string text, ushort colorId, bool bold = false)
        {
            builder.AddUiForeground(colorId);
            if (bold) builder.Add(RawPayload.LinkTerminator);
            builder.AddText(text);
            if (bold) builder.Add(RawPayload.LinkTerminator);
            builder.AddUiForegroundOff();
            return builder;
        }

        public static SeStringBuilder AddRed(this SeStringBuilder builder, string text, bool bold = false)
            => builder.AddColored(text, 14, bold); // Red color

        public static SeStringBuilder AddBlue(this SeStringBuilder builder, string text, bool bold = false)
            => builder.AddColored(text, 37, bold); // Blue color

        public static SeStringBuilder AddYellow(this SeStringBuilder builder, string text, bool bold = false)
            => builder.AddColored(text, 31, bold); // Yellow color

        public static SeStringBuilder AddGreen(this SeStringBuilder builder, string text, bool bold = false)
            => builder.AddColored(text, 43, bold); // Green color

        public static SeStringBuilder AddPurple(this SeStringBuilder builder, string text, bool bold = false)
            => builder.AddColored(text, 541, bold); // Purple color

        public static SeStringBuilder AddOrange(this SeStringBuilder builder, string text, bool bold = false)
            => builder.AddColored(text, 500, bold); // Orange color

        public static SeStringBuilder AddWhite(this SeStringBuilder builder, string text, bool bold = false)
            => builder.AddColored(text, 1, bold); // White color
    }
    
    public static class ThemeHelper
    {
        public static int PushThemeColors(Configuration config)
        {
            return PushDefaultColors();
        }
        
        public static void PopThemeColors(int count)
        {
            if (count > 0)
            {
                ImGui.PopStyleColor(count);
            }
        }
        
        public static int PushDefaultThemeColors()
        {
            return PushDefaultColors();
        }
        
        public static int PushThemeStyleVars(float scale = 1.0f)
        {
            float finalScale = ImGuiHelpers.GlobalScale * scale;

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8 * finalScale, 4 * finalScale));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8 * finalScale, 6 * finalScale));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10.0f * finalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8.0f * finalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f * finalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 8.0f * finalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 6.0f * finalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1.0f * finalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0.5f * finalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.5f * finalScale);

            return 10;
        }
        
        public static void PopThemeStyleVars(int count)
        {
            if (count > 0)
            {
                ImGui.PopStyleVar(count);
            }
        }

        private static int PushDefaultColors()
        {
            // Default matte black styling
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.06f, 0.06f, 0.06f, 0.98f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.08f, 0.08f, 0.08f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.06f, 0.06f, 0.06f, 0.98f));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.12f, 0.12f, 0.12f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.18f, 0.18f, 0.18f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.22f, 0.22f, 0.22f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.04f, 0.04f, 0.04f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.06f, 0.06f, 0.06f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new Vector4(0.06f, 0.06f, 0.06f, 0.98f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.04f, 0.04f, 0.04f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.25f, 0.25f, 0.25f, 0.6f));
            ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, new Vector4(0.35f, 0.35f, 0.35f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.SeparatorActive, new Vector4(0.45f, 0.45f, 0.45f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.92f, 0.92f, 0.92f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.5f, 0.5f, 0.5f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.16f, 0.16f, 0.16f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.22f, 0.22f, 0.22f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.28f, 0.28f, 0.28f, 0.9f));

            return 21;
        }
    }
}
