

using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace SimpleCharacterSelectPlugin.Windows.Utils;

public static class CommonElements
{
    
    public static void ColoredText(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextWrapped(text);
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

    public static void PushScaledStyles(float scale)
    {
        var bg = new Vector4(0.08f, 0.08f, 0.1f, 0.98f);
        var childBg = new Vector4(0.1f, 0.1f, 0.12f, 0.95f);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, bg);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, childBg);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.95f, 0.95f, 0.95f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.16f, 0.16f, 0.2f, 0.9f));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.22f, 0.22f, 0.28f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.28f, 0.28f, 0.35f, 1.0f));

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8 * scale, 5 * scale));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6 * scale, 3 * scale));
    }
    
    public static void PopScaledStyles()
    {
        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(6);
    }
    
    public static bool DrawInputField(string id, string label, float inputWidth, float scale, IReadOnlyList<string> options, ref string selected, string? tooltip)
    {
        ImGui.Text(label);

        // Tooltip
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text("\uf05a");
        ImGui.PopFont();

        if (tooltip != null && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(300 * scale);
            ImGui.TextUnformatted(tooltip);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        ImGui.SetCursorPosX(10 * scale);

        return AutocompleteCombo.Draw($"##{id}", ref selected, options, inputWidth, "------");
    }
    

}