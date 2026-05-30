using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using SimpleCharacterSelectPlugin.Windows.Components;
using SimpleCharacterSelectPlugin.Windows.Styles;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Windows.Utils;

namespace SimpleCharacterSelectPlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin plugin;
        public CharacterGrid CharacterGrid;
        public CharacterForm CharacterForm;
        public DesignPanel DesignPanel;
        public GearsetPanel GearsetPanel;
        public SettingsPanel SettingsPanel;
        public ReorderWindow ReorderWindow;
        private UIStyles uiStyles;

        // Custom theme background image path (texture fetched fresh each frame)
        private string? _lastLoggedBackgroundPath;
        public DesignPanel? GetDesignPanel() => DesignPanel;

        public MainWindow(Plugin plugin)
            : base("Simple Character Select", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDocking)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(850, 700),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            this.plugin = plugin;
            this.uiStyles = new UIStyles(plugin);

            this.CharacterGrid = new CharacterGrid(plugin, uiStyles);
            this.CharacterForm = new CharacterForm(plugin, uiStyles);
            this.DesignPanel = new DesignPanel(plugin, uiStyles);
            this.GearsetPanel = new GearsetPanel(plugin, uiStyles);
            this.SettingsPanel = new SettingsPanel(plugin, uiStyles, this);
            this.ReorderWindow = new ReorderWindow(plugin, uiStyles);
        }

        public override void PostDraw()
        {
            uiStyles.PopCustomWindowBgIfNeeded();
        }

        public void InvalidateLayout()
        {
            CharacterGrid?.InvalidateCache();
        }

        public void Dispose()
        {
            CharacterGrid?.Dispose();
            CharacterForm?.Dispose();
            DesignPanel?.Dispose();
            SettingsPanel?.Dispose();
            ReorderWindow?.Dispose();
        }

        public override void Draw()
        {
            plugin.WindowState.MainWindowPos = ImGui.GetWindowPos();
            plugin.WindowState.MainWindowSize = ImGui.GetWindowSize();

            float deltaTime = ImGui.GetIO().DeltaTime;

            uiStyles.PushMainWindowStyle();

            try
            {
                DrawHeader();
                DrawMainContent(deltaTime);
                DrawBottomBar();

                SettingsPanel.Draw();
                ReorderWindow.Draw();
            }

            finally
            {
                uiStyles.PopMainWindowStyle();
            }
        }

        private void DrawHeader()
        {
            int totalCharacters = plugin.Characters.Count;
            string headerText = $"Choose your character";
            ImGui.Text(headerText);

            ImGui.SameLine();
            CommonElements.ColoredText($"({totalCharacters} total)", Colors.Grey1);

            // Current character data
            if (Plugin.ObjectTable.LocalPlayer != null)
            {
                {
                    ImGui.SameLine();
                    CommonElements.ColoredText($"Current gearset: {GearsetManager.GetCurrentGearset().DisplayName()}", Colors.Grey2);
                }
            }

            ImGui.SameLine();

            var totalScale = ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier;

            float buttonWidth = 12 * totalScale;
            float iconButtonSize = ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2;
            float buttonHeight = iconButtonSize;
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float spacing = ImGui.GetStyle().ItemSpacing.X;

            // Position for Revert button + Discord button
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + availableWidth - buttonWidth - iconButtonSize);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (5 * totalScale) );

            // Revert button
            if (uiStyles.IconButton("\uf0e2", "Revert All SCS Changes", new Vector2(iconButtonSize, iconButtonSize)))
            {
                plugin.ActivePlayer.Pc.ActiveCharacter = null;
                DesignManager.RevertAllChanges();
            }

            ImGui.SameLine();

            ImGui.Separator();
        }


        public void UpdateSortType()
        {
            CharacterGrid.SetSortType((Plugin.SortType)Plugin.Configuration.CurrentSortIndex);
        }

        private void DrawMainContent(float deltaTime)
        {
            var totalScale = ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier;

            if (plugin.WindowState.IsAddCharacterWindowOpen || CharacterForm.IsEditWindowOpen)
            {
                CharacterForm.Draw();
            }

            float characterGridWidth = 0;
            float scaledPanelWidth = 0;
            if (DesignPanel.IsOpen || GearsetPanel.IsOpen)
            {
                scaledPanelWidth = DesignPanel.PanelWidth * totalScale;
                characterGridWidth = -(scaledPanelWidth + 10);
            }

            // Main content area
            float bottomBarHeight = ImGui.GetFrameHeight() + (10 * totalScale);
            ImGui.BeginChild("CharacterGrid", new Vector2(characterGridWidth, -bottomBarHeight), true);

            CharacterGrid.Draw();
            ImGui.EndChild();

            if (DesignPanel.IsOpen || GearsetPanel.IsOpen)
            {
                ImGui.SameLine();
            
                float characterGridHeight = ImGui.GetItemRectSize().Y;
                ImGui.BeginChild("SidePanel", new Vector2(scaledPanelWidth, characterGridHeight), true);
            
                if (DesignPanel.IsOpen)
                {
                    DesignPanel.Draw();
                }

                if (GearsetPanel.IsOpen)
                {
                    GearsetPanel.Draw();
                }
            
                ImGui.EndChild();
            }
        }

        private void DrawBottomBar()
        {
            var totalScale = ImGuiHelpers.GlobalScale * Plugin.Configuration.UIScaleMultiplier;
            float bottomPadding = 10 * totalScale;
            ImGui.SetCursorPos(new Vector2(10 * totalScale, ImGui.GetWindowHeight() - ImGui.GetFrameHeight() - bottomPadding));

            if (uiStyles.IconButton("\uf013", "Settings"))
            {
                plugin.WindowState.IsSettingsOpen = !plugin.WindowState.IsSettingsOpen;
            }
            plugin.WindowState.SettingsButtonPos = ImGui.GetItemRectMin();
            plugin.WindowState.SettingsButtonSize = ImGui.GetItemRectSize();

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                ImGui.BeginTooltip();
                ImGui.Text("Open Settings Menu.");
                ImGui.Text("You can find options for adjusting your Character Grid.");
                ImGui.Text("As well as the Opt-In for Glamourer Automations.");
                ImGui.EndTooltip();
            }

            ImGui.SameLine();

            if (ImGui.Button("Reorder Characters"))
                ReorderWindow.Open();

            ImGui.SameLine();

            if (ImGui.Button("Quick Switch"))
                plugin.QuickSwitchWindow.IsOpen = !plugin.QuickSwitchWindow.IsOpen;
            plugin.WindowState.QuickSwitchButtonPos = ImGui.GetItemRectMin();
            plugin.WindowState.QuickSwitchButtonSize = ImGui.GetItemRectSize();

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                ImGui.BeginTooltip();
                ImGui.Text("Opens a more compact UI to swap between Characters & Designs.");
                ImGui.EndTooltip();
            }
        }

        public void OpenEditCharacterWindow(int index) => CharacterForm.OpenEditCharacterWindow(index);
        public void OpenAddCharacterWindow()
        {
            CharacterForm.InitCreateCharacterWindow();
            plugin.WindowState.IsAddCharacterWindowOpen = true;
        }
        public void OpenDesignPanel(int characterIndex) => DesignPanel.Open(characterIndex);
        public void SortCharacters() => CharacterGrid.SortCharacters();
    }
}
