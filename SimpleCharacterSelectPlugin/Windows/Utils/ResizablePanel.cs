using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SimpleCharacterSelectPlugin.Windows.Utils;

public class ResizablePanel : IDisposable
{
    public bool IsResizing = false;
    public float MinPanelWidth;
    public float MaxPanelWidth;
    public float PanelWidth;
    
    internal void DrawResizeHandle(Plugin plugin, float totalScale, float scaledPanelWidth, float scaledMinWidth, float scaledMaxWidth, float scaledHandleWidth)
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
        if (hovered || IsResizing)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

            if (hovered && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Left)))
            {
                ImGui.SetItemAllowOverlap();

                // Create an invisible button over the resize area to capture input
                ImGui.SetCursorScreenPos(handleMin);
                ImGui.InvisibleButton("##resize_handle", new Vector2(scaledHandleWidth, windowSize.Y));

                if (ImGui.IsItemActive() || IsResizing)
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        IsResizing = true;
                    }
                }
            }
        }

        // Handle resizing
        if (IsResizing)
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
                Plugin.Configuration.DesignPanelWidth = PanelWidth;
                // Force main window to recalculate layout
                if (plugin.MainWindow != null)
                {
                    plugin.MainWindow.InvalidateLayout();
                }
            }
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                IsResizing = false;
                // Save configuration
                Plugin.Configuration.Save();
            }
        }

        // Draw visual resize handle
        var drawList = ImGui.GetWindowDrawList();
        uint handleColor = hovered || IsResizing
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
        if (hovered || IsResizing)
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

    public void Dispose()
    {
    }
}