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

namespace SimpleCharacterSelectPlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin plugin;
        private CharacterGrid characterGrid;
        private CharacterForm characterForm;
        private DesignPanel designPanel;
        private SettingsPanel settingsPanel;
        private ReorderWindow reorderWindow;
        private UIStyles uiStyles;
        private float giftBoxShakeTimer = 0f;
        private const float GIFT_BOX_SHAKE_DURATION = 0.3f;

        // Custom theme background image path (texture fetched fresh each frame)
        private string? _lastLoggedBackgroundPath;
        public bool IsDesignPanelOpen => designPanel?.IsOpen ?? false;
        public bool IsEditCharacterWindowOpen => characterForm?.IsEditWindowOpen ?? false;
        public bool IsReorderWindowOpen => reorderWindow?.IsOpen ?? false;
        
        public DesignPanel? GetDesignPanel() => designPanel;

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

            this.characterGrid = new CharacterGrid(plugin, uiStyles);
            this.characterForm = new CharacterForm(plugin, uiStyles);
            this.designPanel = new DesignPanel(plugin, uiStyles);
            this.settingsPanel = new SettingsPanel(plugin, uiStyles, this);
            this.reorderWindow = new ReorderWindow(plugin, uiStyles);

            // Pre-warm the file cache on a background thread to prevent UI freezing
            // when opening the window for the first time (especially for network paths)
            characterGrid.PreWarmCacheAsync();
        }

        public override void PreDraw()
        {
            uiStyles.PushCustomWindowBgIfNeeded();
        }

        public override void PostDraw()
        {
            uiStyles.PopCustomWindowBgIfNeeded();
        }

        public void InvalidateLayout()
        {
            characterGrid?.InvalidateCache();
        }

        public void Dispose()
        {
            characterGrid?.Dispose();
            characterForm?.Dispose();
            designPanel?.Dispose();
            settingsPanel?.Dispose();
            reorderWindow?.Dispose();
        }

        /// <summary>Draws custom background image in current child window.</summary>
        private void DrawCustomBackgroundInChild()
        {
            var config = plugin.Configuration.CustomTheme;
            if (string.IsNullOrEmpty(config.BackgroundImagePath))
                return;

            if (!File.Exists(config.BackgroundImagePath))
                return;

            var texture = Plugin.TextureProvider
                .GetFromFile(config.BackgroundImagePath)
                .GetWrapOrDefault();

            var childPos = ImGui.GetWindowPos();
            var childSize = ImGui.GetWindowSize();
            var drawList = ImGui.GetWindowDrawList();

            if (texture == null)
                return;

            if (_lastLoggedBackgroundPath != config.BackgroundImagePath)
            {
                Plugin.Log.Info($"[CustomBG] Loaded! Size: {texture.Width}x{texture.Height}");
                _lastLoggedBackgroundPath = config.BackgroundImagePath;
            }

            // Calculate base image size (cover, maintain aspect ratio)
            var imageAspect = (float)texture.Width / texture.Height;
            var windowAspect = childSize.X / childSize.Y;

            Vector2 baseImageSize;

            if (imageAspect > windowAspect)
            {
                baseImageSize.Y = childSize.Y;
                baseImageSize.X = childSize.Y * imageAspect;
            }
            else
            {
                baseImageSize.X = childSize.X;
                baseImageSize.Y = childSize.X / imageAspect;
            }

            // Zoom
            var zoom = Math.Clamp(config.BackgroundImageZoom, 0.5f, 3.0f);
            var imageSize = baseImageSize * zoom;

            var centeredOffset = (childSize - imageSize) / 2;

            // User offset
            var userOffsetX = config.BackgroundImageOffsetX * (imageSize.X - childSize.X) * 0.5f;
            var userOffsetY = config.BackgroundImageOffsetY * (imageSize.Y - childSize.Y) * 0.5f;
            var finalOffset = centeredOffset + new Vector2(userOffsetX, userOffsetY);

            var tintColor = new Vector4(1, 1, 1, config.BackgroundImageOpacity);

            drawList.PushClipRect(childPos, childPos + childSize, true);

            drawList.AddImage(
                texture.Handle,
                childPos + finalOffset,
                childPos + finalOffset + imageSize,
                Vector2.Zero,
                Vector2.One,
                ImGui.ColorConvertFloat4ToU32(tintColor)
            );

            drawList.PopClipRect();
        }

        /// <summary>Draws hearts.jpg background in current child window for Valentine's theme.</summary>
        private void DrawValentinesBackgroundInChild()
        {
            var heartsPath = Path.Combine(plugin.PluginDirectory, "Assets", "hearts.jpg");
            if (!File.Exists(heartsPath))
                return;

            var texture = Plugin.TextureProvider
                .GetFromFile(heartsPath)
                .GetWrapOrDefault();

            if (texture == null)
                return;

            var childPos = ImGui.GetWindowPos();
            var childSize = ImGui.GetWindowSize();
            var drawList = ImGui.GetWindowDrawList();

            // Cover the child window, maintaining aspect ratio
            var imageAspect = (float)texture.Width / texture.Height;
            var windowAspect = childSize.X / childSize.Y;

            Vector2 imageSize;
            if (imageAspect > windowAspect)
            {
                imageSize.Y = childSize.Y;
                imageSize.X = childSize.Y * imageAspect;
            }
            else
            {
                imageSize.X = childSize.X;
                imageSize.Y = childSize.X / imageAspect;
            }

            var offset = (childSize - imageSize) / 2;
            var tintColor = new Vector4(1, 1, 1, 0.5f);

            drawList.PushClipRect(childPos, childPos + childSize, true);
            drawList.AddImage(
                texture.Handle,
                childPos + offset,
                childPos + offset + imageSize,
                Vector2.Zero,
                Vector2.One,
                ImGui.ColorConvertFloat4ToU32(tintColor)
            );
            drawList.PopClipRect();
        }

        public override void Draw()
        {
            plugin.WindowState.MainWindowPos = ImGui.GetWindowPos();
            plugin.WindowState.MainWindowSize = ImGui.GetWindowSize();

            float deltaTime = ImGui.GetIO().DeltaTime;

            if (giftBoxShakeTimer > 0f)
            {
                giftBoxShakeTimer -= deltaTime;
                if (giftBoxShakeTimer < 0f) giftBoxShakeTimer = 0f;
            }

            uiStyles.PushMainWindowStyle();

            try
            {
                DrawHeader();
                DrawMainContent(deltaTime);
                DrawBottomBar();

                settingsPanel.Draw();
                reorderWindow.Draw();
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
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
            ImGui.Text($"({totalCharacters} total)");
            ImGui.PopStyleColor();

            // Idle pose indicator
            if (Plugin.ObjectTable.LocalPlayer != null)
            {
                unsafe
                {
                    var charPtr = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)Plugin.ObjectTable.LocalPlayer.Address;
                    var currentIdle = charPtr->EmoteController.CPoseState;
                    
                    var scale = ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier;
                    
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                    ImGui.Text($"Current Idle: {currentIdle}");
                    ImGui.PopStyleColor();
                    
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Current idle pose: {currentIdle}");
                    }
                }
            }

            ImGui.SameLine();

            var totalScale = ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier;

            float buttonWidth = 12 * totalScale;
            float iconButtonSize = ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2;
            float buttonHeight = iconButtonSize;
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float spacing = ImGui.GetStyle().ItemSpacing.X;

            // Position for Revert button + Discord button
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + availableWidth - buttonWidth - iconButtonSize);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (5 * totalScale) );

            // Revert button
            if (uiStyles.IconButton("\uf0e2", "Revert All SCS Changes\n\nReverts:\n• Glamourer → Game state\n• Honorific → Cleared\n• Moodles → All removed\n• Customize+ → Disabled\n• Penumbra → Your Character collection\n• SCS → No active character", new Vector2(iconButtonSize, iconButtonSize)))
            {
                //plugin.RevertAllChanges();
            }

            ImGui.SameLine();

            ImGui.Separator();
        }


        public void UpdateSortType()
        {
            characterGrid.SetSortType((Plugin.SortType)plugin.Configuration.CurrentSortIndex);
        }

        private void DrawMainContent(float deltaTime)
        {
            var totalScale = ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier;

            if (plugin.WindowState.IsAddCharacterWindowOpen || characterForm.IsEditWindowOpen)
            {
                characterForm.Draw();
            }

            float characterGridWidth = 0;
            if (designPanel.IsOpen)
            {
                float scaledPanelWidth = designPanel.PanelWidth * totalScale;
                characterGridWidth = -(scaledPanelWidth + 10);
            }

            // Main content area
            float bottomBarHeight = ImGui.GetFrameHeight() + (10 * totalScale);
            ImGui.BeginChild("CharacterGrid", new Vector2(characterGridWidth, -bottomBarHeight), true);

            characterGrid.Draw();
            ImGui.EndChild();

            if (designPanel.IsOpen)
            {
                ImGui.SameLine();
                float characterGridHeight = ImGui.GetItemRectSize().Y;
                float scaledPanelWidth = designPanel.PanelWidth * totalScale;

                ImGui.BeginChild("DesignPanel", new Vector2(scaledPanelWidth, characterGridHeight), true);
                designPanel.Draw();
                ImGui.EndChild();
            }
        }

        private void DrawBottomBar()
        {
            var totalScale = ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier;
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
                reorderWindow.Open();

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
            
            ImGui.SameLine();

            string randomIcon;
            Vector4? iconColor = null;
            randomIcon = "\uf522"; // Dice

            string randomTooltip = plugin.Configuration.RandomSelectionFavoritesOnly
                ? "Randomly selects from favourited characters and designs only"
                : "Randomly selects from all characters and designs";

            if (uiStyles.IconButtonWithColor(randomIcon, randomTooltip, null, 1.0f, iconColor))
            {
                Vector2 effectPos = ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2;
                plugin.SelectRandomCharacterAndDesign();
            }

        }

        public void OpenEditCharacterWindow(int index) => characterForm.OpenEditCharacterWindow(index);
        public void OpenAddCharacterWindow()
        {
            characterForm.InitCreateCharacterWindow();
            plugin.WindowState.IsAddCharacterWindowOpen = true;
        }
        public void OpenDesignPanel(int characterIndex) => designPanel.Open(characterIndex);
        public void CloseDesignPanel() => designPanel.Close();
        public void SortCharacters() => characterGrid.SortCharacters();

        /// <summary>Opens the settings panel and navigates to a specific section.</summary>
        public void SwitchToSettingsSection(string sectionName)
        {
            plugin.WindowState.IsSettingsOpen = true;
            settingsPanel.ExpandSection(sectionName);
        }
    }
}
