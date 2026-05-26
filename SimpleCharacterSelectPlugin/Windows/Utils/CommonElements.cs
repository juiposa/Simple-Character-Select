

using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace SimpleCharacterSelectPlugin.Windows.Utils;

public static class CommonElements
{
    public static void ColoredButton(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text(text);
        ImGui.PopStyleColor();
    }

    public static void DrawTooltip(string tooltip, float scale, Action? afterTooltip = null)
    {
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
}